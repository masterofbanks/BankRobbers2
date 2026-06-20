using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarManager : MonoBehaviour
{
    [Header("General Car Information")]
    [SerializeField] private float CurrentForwardSpeed;
    
    [Header("Wheel Information")]
    [SerializeField] private WheelPhysics[] Wheels;
    [SerializeField] [Range(0.1f,10)] private float WheelRadius;
    [SerializeField] private LayerMask GroundLayer;
    [SerializeField] [Range(0f, 45f)] private float MaxTurningAngle;    
    public float SteeringDirection;

    [Header("Suspension Information")]
    [SerializeField] private bool ApplySettingsToAllCoils = true;
    [SerializeField] private float BrakeForce = 50f;
    [SerializeField] private float SpringStrength = 100f;
    [SerializeField] private float SpringDamper = 10f;
    [SerializeField] private float SpringRestLength = 1.0f;

    [Header("Throttle Information")]
    [SerializeField] private AnimationCurve PowerCurve;
    [SerializeField] private float MaxPower = 300f;
    [SerializeField] private float TopSpeed = 50f;
    [SerializeField] private float MaxReverseSpeed = 10f;
    [SerializeField] private float MinSpeedToReverse = 1.0f;
    public float ThrottleBrakeValue;// { get; set; }

    [Header("Jump Settings")]
    [SerializeField] private float JumpForce = 300f;
    [SerializeField] private float GravityOnTheWayUp = 2.0f;
    [SerializeField] private float GravityOnTheWayDown = 4.0f;
    [SerializeField] private float JumpBufferDistance = 4.0f;
    [SerializeField] private GameObject TargetBelow;
    private Vector3 _jumpDirection;
    private bool CanJump = false;
    private RaycastHit _jumpRayHit;

    [Header("Slam Down Settings")]
    [SerializeField] private float SlamDownForce = 500f;
    private GameObject _slamDownEffect;
    private bool _isSlamingDown = false;    

    [Header("In Air Rotation Values")]
    [SerializeField] private float YawRotationStrength = 10f;
    //set up an enum with for the car ground state with in air, fully on ground, and intermediate
    public enum CarGroundState
    {
        InAir,
        FullyOnGround,
        Intermediate
    }
    public CarGroundState _currentGroundState;


    //components
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        foreach(WheelPhysics wheel in Wheels)
        {
            wheel.InitializeWheelDetails(WheelRadius, BrakeForce);
            if (ApplySettingsToAllCoils)
            {
                wheel.InitalizeSuspensionDetails(SpringStrength, SpringDamper, SpringRestLength);
            }
        }
        TargetBelow.SetActive(false);
        SteeringDirection = 0f;
        _slamDownEffect = Resources.Load<GameObject>("SlamDownEffect");
    }
    void Start()
    {
        ThrottleBrakeValue = 0.0f;
    }


    private void FixedUpdate()
    {
        _rb.centerOfMass = Vector3.down * 0.5f; //lower center of mass to make car more stable
        foreach (WheelPhysics wheelSim in Wheels)
        {
            wheelSim.TestWhetherWheelHitTheGround(GroundLayer);
            wheelSim.SimulateSuspensionPhysics(_rb);
            wheelSim.TurnWheel(GetCurrentTurningConfiguration());
            wheelSim.PreventTireSlipping(_rb);
            ApplyDriveOrBrakeToWheel(wheelSim);
        }
        UpdateForwardSpeed();
        UpdateGroundState();
        ModulateGravity(); //change this to a currentGravMult that i moduldate depending on car settings
        //if youre in the air, add some addendemum here where you can use the directional keys to affect your car's rotation
        ApplyInAirRotation();
    }

    private void Update()
    {
        //update jump parameters
        bool jumpRayHit = Physics.Raycast(transform.position, -transform.up, out _jumpRayHit, Mathf.Infinity, GroundLayer);
        CanJump = _jumpRayHit.distance < JumpBufferDistance;
        Debug.DrawLine(transform.position, transform.position - transform.up * JumpBufferDistance, CanJump ? Color.blue : Color.red);
        TargetBelow.SetActive(_currentGroundState == CarGroundState.InAir && jumpRayHit);
        if (TargetBelow.activeInHierarchy)
        {
            TargetBelow.transform.position = transform.position - transform.up * (_jumpRayHit.distance * 0.75f);
        }
    }

    private void ModulateGravity()
    {
        if (_rb.linearVelocity.y > 0)
        {
            _rb.AddForce(Physics.gravity * GravityOnTheWayUp);
        }

        else
        {
            _rb.AddForce(Physics.gravity * GravityOnTheWayDown);
        }
    }

    /// <summary>
    /// Applies drive or brake force to the specified wheel based on the current throttle or brake input.
    /// </summary>
    /// <remarks>This method only applies force if the throttle or brake input is significant and the
    /// vehicle's speed is within the allowed range. The direction and magnitude of the force depend on the current
    /// input and power curve.</remarks>
    /// <param name="wheel">The wheel to which the drive or brake force will be applied.</param>
    private void ApplyDriveOrBrakeToWheel(WheelPhysics wheel)
    {
        if (ThrottleBrakeValue > 0)
        {
            float powerToTheWheels = CalculatePower(CurrentForwardSpeed, TopSpeed);
            wheel.DriveWheel(_rb, powerToTheWheels, ThrottleBrakeValue > 0.001f);
        }

        else if(ThrottleBrakeValue < 0)
        {
            if(CurrentForwardSpeed > MinSpeedToReverse)
            {
                wheel.BrakeWheel(_rb);
            }
            else
            {
                float powerToWheels = -1 * CalculatePower(Mathf.Abs(CurrentForwardSpeed), MaxReverseSpeed); //have to mult by -1 since ThrottleBrakeValue is negative
                wheel.DriveWheel(_rb, powerToWheels, false);
            }
        }
    }

    /// <summary>
    /// Find how far along the power curve based on the current speed of the car vs the top speed of the car to clauclate how much power we should supply to a wheel
    /// </summary>
    /// <param name="currentSpeed"></param>
    /// <param name="topSpeed"></param>
    /// <returns>The power needed at a wheel</returns>
    private float CalculatePower(float currentSpeed, float topSpeed)
    {
        if(FindSpeedAlpha(currentSpeed, topSpeed) > 1)
        {
            return 0f;
        }

        return MaxPower * PowerCurve.Evaluate(FindSpeedAlpha(currentSpeed, topSpeed)) * ThrottleBrakeValue;
    }

    private void OnValidate()
    {
        UpdateWheelSettings();
    }

    private void UpdateWheelSettings()
    {
        foreach(WheelPhysics wheelSim in Wheels)
        {
            if(wheelSim != null && wheelSim.TakeSettingsFromCarManager)
            {
                Vector3 currentScale = wheelSim.GetComponentInChildren<MeshRenderer>().transform.localScale;
                wheelSim.GetComponentInChildren<MeshRenderer>().transform.localScale = new Vector3(WheelRadius, currentScale.y, WheelRadius);
                wheelSim.InitializeWheelDetails(WheelRadius, BrakeForce);
                wheelSim.InitalizeSuspensionDetails(SpringStrength, SpringDamper, SpringRestLength);
            }
        }
    }

    /// <summary>
    /// Find the current fraction of the top speed the car is currently at
    /// </summary>
    /// <returns></returns>
    private float FindSpeedAlpha(float currentSpeed, float topSpeed)
    {
        return currentSpeed / topSpeed;
    }


    private void UpdateForwardSpeed()
    {
        CurrentForwardSpeed = Vector3.Dot(transform.forward, _rb.linearVelocity);
    }

    /// <summary>
    /// Check if each wheel is on the ground. If some are and some arent, then set the current ground state to an intermediate between the two extremes. 
    /// We also update whether we can jump in this method by raycasting down to the ground and checking if the distance between the ground and the car is less than a jump buffer distance.
    /// </summary>
    private void UpdateGroundState()
    {
        bool checkForPartialGround = true;
        WheelPhysics lastWheel = null;
        
        for(int i = 0; i < Wheels.Length - 1; i++)
        {
            if (i == 0)
            {
                checkForPartialGround = Wheels[i].TireInContactWithGround;
            }
            else
            {
                checkForPartialGround = checkForPartialGround ^ Wheels[i].TireInContactWithGround; //XOR to check if some but not all wheels are on the ground

            }

            lastWheel = Wheels[i];
        }

        if (checkForPartialGround)
        {
            _currentGroundState = CarGroundState.Intermediate;
        }

        else
        {
            if (lastWheel.TireInContactWithGround)
            {
                _currentGroundState = CarGroundState.FullyOnGround;
                if (_isSlamingDown)
                {
                    SlamInContactWithGround();
                }
            }
            else
            {
                _currentGroundState = CarGroundState.InAir;
            }
        }

        
    }

    private void ApplyInAirRotation()
    {
        if (_currentGroundState == CarGroundState.InAir)
        {
            float rollInput = SteeringDirection; 
            _rb.AddRelativeTorque(Vector3.up * rollInput * YawRotationStrength);
        }
    }

    public void Jump()
    {
        if (CanJump)
        {
            _jumpDirection = transform.up; 
            _rb.AddForce(_jumpDirection.normalized * JumpForce, ForceMode.Impulse);
        }

        else if(_currentGroundState == CarGroundState.InAir)
        {
            SlamDown();
        }

    }

    public void SlamDown()
    {
        _rb.AddForce(-transform.up * SlamDownForce, ForceMode.Impulse);
        _isSlamingDown = true;

    }

    private void SlamInContactWithGround()
    {
        _isSlamingDown = false;
        Instantiate(_slamDownEffect, transform.position, Quaternion.identity); 
    }

    private void OnDestroy()
    {
        Debug.Log($"Wheel Radius {WheelRadius} | Spring Strength: {SpringStrength} | Spring Damper: {SpringDamper} | Spring Rest Length: {SpringRestLength}");
    }

    public float GetCurrentTurningConfiguration()
    {
        return SteeringDirection * MaxTurningAngle;
    }

    
}
