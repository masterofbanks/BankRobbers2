using UnityEngine;

public class WheelPhysics : MonoBehaviour
{
    [Header("Tire Details")]
    [SerializeField] private float WheelRadius;
    [SerializeField] private float BrakeForce;
    public bool TakeSettingsFromCarManager = true;
    public bool TireInContactWithGround;// { get; private set;}
    private RaycastHit hit;

    [Header("Grip Details")]
    [SerializeField] private float GripFactor = 0.5f;
    [SerializeField] private float TireMass = 0.085f;
    [SerializeField] private bool CanTurn = true;
    [SerializeField] private bool CanBrake = true;

    [Header("Suspension Details")]
    [SerializeField] private float SuspensionRestDistance = 0.5f;
    [SerializeField] private float SpringStrength = 100f;
    [SerializeField] private float SpringDamper = 10f;

    [Header("Power Details")]
    [SerializeField] private bool HasPower = true;

    public virtual void TestWhetherWheelHitTheGround(LayerMask groundLayer)
    {
        TireInContactWithGround = Physics.Raycast(transform.position, -1 * transform.up, out hit, WheelRadius, groundLayer);
    }

    /// <summary>
    /// Update the current wheel settings to settings sent in, most likely, from the Car Manager
    /// </summary>
    /// <param name="wheelRadius"></param>
    public void InitializeWheelDetails(float wheelRadius, float brakeForce)
    {
        WheelRadius = wheelRadius;
        BrakeForce = brakeForce;
        
    }

    /// <summary>
    /// Update the current suspension settings to settings sent in, most likely, from the Car Manager
    /// </summary>
    /// <param name="springStrength"></param>
    /// <param name="springDamper"></param>
    /// <param name="springRestLength"></param>
    public void InitalizeSuspensionDetails(float springStrength, float springDamper, float springRestLength)
    {
        SpringStrength = springStrength;
        SpringDamper = springDamper;
        SuspensionRestDistance = springRestLength;
    }

    /// <summary>
    /// Cast a ray down from this tire's position, and check if we have hit the ground within the wheel radius. If we have, apply an upward force at this location to propell the car's rigidbody upwards at this position. Code taken from https://www.youtube.com/watch?v=CdPYlj5uZeI (Toyful Games)
    /// </summary>
    /// <param name="carBody"></param>
    /// <param name="groundLayer"></param>
    public void SimulateSuspensionPhysics(Rigidbody carBody)
    {
        if (TireInContactWithGround)
        {
            Vector3 springDir = transform.up;
            Vector3 tireWorldVel = carBody.GetPointVelocity(transform.position);
            float offset = SuspensionRestDistance - hit.distance;
            float vel = Vector3.Dot(springDir, tireWorldVel);
            float force = (offset * SpringStrength) - (vel * SpringDamper);
            carBody.AddForceAtPosition(springDir * force, transform.position);
            Debug.DrawRay(transform.position, springDir * force, Color.green);
        }
    }

    /// <summary>
    /// Apply a force to the tire in the forward or backward direction. Use the goForward bool to set the direction; true for forward and false for backward
    /// </summary>
    /// <param name="carBody"></param>
    /// <param name="groundLayer"></param>
    /// <param name="amountOfPower"></param>
    /// <param name="direction"></param>
    public void DriveWheel(Rigidbody carBody, float amountOfPower, bool goForward)
    {
        if (!HasPower)
        {
            return;
        }

        
        if (TireInContactWithGround)
        {
            Vector3 directionOfForce = transform.forward;
            if (!goForward)
            {
                directionOfForce = -transform.forward;
            }
            carBody.AddForceAtPosition(directionOfForce * amountOfPower, transform.position);
            Debug.DrawRay(transform.position, directionOfForce * amountOfPower, Color.red);
        }
    }

    /// <summary>
    /// Provide a constant force onto a wheel in the opposite diretion of the wheel's forward direction
    /// </summary>
    /// <param name="carBody"></param>
    /// <param name="groundLayer"></param>
    public void BrakeWheel(Rigidbody carBody)
    {
        
        if (TireInContactWithGround && CanBrake)
        {
            Vector3 brakeDirection = -transform.forward;
            carBody.AddForceAtPosition(BrakeForce * brakeDirection, transform.position);
            Debug.DrawRay(transform.position, BrakeForce * brakeDirection, Color.red);
        }
    }

    /// <summary>
    /// Applies corrective measures to reduce or prevent tire slipping during vehicle operation. Used to turn the car when this object's transform is rotated on the y axis
    /// </summary>
    public void PreventTireSlipping(Rigidbody carBody)
    {
        if (TireInContactWithGround)
        {
            Vector3 steeringDir = transform.right;
            Vector3 tireWorldVel = carBody.GetPointVelocity(transform.position);

            float steeringSpeed = Vector3.Dot(steeringDir, tireWorldVel);
            float desiredVelChange = -steeringSpeed * GripFactor;

            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            Debug.DrawRay(transform.position, steeringDir * TireMass * desiredAccel, Color.yellow);
            carBody.AddForceAtPosition(steeringDir * TireMass * desiredAccel, transform.position);
        }
    }

    public void TurnWheel(float steeringAngle)
    {
        if (CanTurn)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, steeringAngle, transform.localEulerAngles.z);
        }
    }
}
