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

        SteeringDirection = 0f;
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
            wheelSim.TurnWheel(SteeringDirection * MaxTurningAngle);
            wheelSim.PreventTireSlipping(_rb);
            ApplyDriveOrBrakeToWheel(wheelSim);
        }
        UpdateForwardSpeed();
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
            if(wheelSim != null)
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

    private void OnDestroy()
    {
        Debug.Log($"Wheel Radius {WheelRadius} | Spring Strength: {SpringStrength} | Spring Damper: {SpringDamper} | Spring Rest Length: {SpringRestLength}");
    }
}
