using PathCreation;
using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    [SerializeField] internal Rigidbody rb;
    [SerializeField] internal PathCreator pathCreator;
    [SerializeField] internal VehicleGroundDetection groundDetection = new VehicleGroundDetection();
    [SerializeField] private float gravity = 20;
    [SerializeField] private float fallGravity = 50;
    [SerializeField] private float maxSpeed = 50;
    [SerializeField] private float acceleration = 20;

    [Header("Wheels")] [SerializeField] private float wheelsRotationSpeed = 100f;
    [SerializeField] private float wheelsTurnPercentage = 1f;
    [SerializeField] private float wheelsTurnSpeed = 20f;
    [SerializeField] private Transform FLwheel;
    [SerializeField] private Transform FRwheel;
    [SerializeField] private Transform BLwheel;
    [SerializeField] private Transform BRwheel;
    [SerializeField] private Transform FLwheelPivot;
    [SerializeField] private Transform FRwheelPivot;
    [SerializeField] private Transform BLwheelPivot;
    [SerializeField] private Transform BRwheelPivot;

    [Header("Movement Settings")] [Range(0, 3)] [SerializeField]
    internal float steeringPower = 1.5f;

    [Range(0, 1)] [SerializeField] private float grip = 1;

    internal bool CanMove { get; set; } = true;
    private float ForwardSpeed => Vector3.Dot(rb.velocity, transform.forward);
    internal float NormalizedForwardSpeed => (Mathf.Abs(ForwardSpeed) > 0.1f ? ForwardSpeed / maxSpeed : 0.0f);

    private Vehicle vehicle;
    private float currentTurnAngle = 0f;
    private FloatingJoystick floatingJoystick;
    private Transform _cachedTransform;
    private GameController _gameController;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        rb = GetComponent<Rigidbody>();
        groundDetection.Initialize(vehicle);
        floatingJoystick = FunctionalButtons.Instance.floatingJoystick;
        pathCreator = FindObjectOfType<PathCreator>();

        _cachedTransform = transform;
        _gameController = GameController.Instance;
    }

    private void FixedUpdate()
    {
        if (!vehicle.isLocalPlayer) return;
        bool isGrounded = groundDetection.IsGrounded;

        PerformGroundCheck();
        ApplyGravity(isGrounded);
        ApplyLateralFriction(isGrounded);

        float steeringInput = floatingJoystick.Horizontal;
        float accelerationInput = floatingJoystick.Vertical;

        ApplySteering(steeringInput);
        ApplyAcceleration(accelerationInput);
        TurnWheels(steeringInput);
        SpinWheels();
    }

    public void ResetCarPosition()
    {
        if (pathCreator != null)
        {
            float closestDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
            transform.position = pathCreator.path.GetPointAtDistance(closestDistance);
            transform.rotation = Quaternion.LookRotation(pathCreator.path.GetDirection(closestDistance));
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        CanMove = true;
        vehicle.healthController.Heal(100);
    }

    public void ApplyBraking(float brakePower)
    {
        Vector3 brakeForce = -rb.velocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    public void TurnWheels(float turnAngle)
    {
        float targetTurnAngle = turnAngle * wheelsTurnPercentage;
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, Time.deltaTime * wheelsTurnSpeed);

        FLwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        FRwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        BLwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
        BRwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
    }

    private void PerformGroundCheck() => groundDetection.CheckGround();

    private void ApplyGravity(bool isGrounded)
    {
        float factor = isGrounded ? gravity : fallGravity;
        rb.AddForce(-factor * Vector3.up, ForceMode.Acceleration);
    }

    private void ApplyLateralFriction(bool isGrounded)
    {
        if (isGrounded)
        {
            // Кэширование transform.right
            Vector3 right = _cachedTransform.right;
            float lateralSpeed = Vector3.Dot(rb.velocity, right);
            Vector3 lateralFriction = -right * ((lateralSpeed / Time.fixedDeltaTime) * grip);
            rb.AddForce(lateralFriction, ForceMode.Acceleration);
        }
    }

    public void ApplySteering(float steeringInput)
    {
        bool isGameStarted = _gameController.isGameStarted;
        bool isGrounded = groundDetection.IsGrounded;

        if (!isGrounded || !CanMove || !isGameStarted)
            return;
        float forwardSpeed = Vector3.Dot(rb.velocity, _cachedTransform.forward);
        float speedFactor = forwardSpeed * 0.075f;
        float clampedSteering = Mathf.Clamp(steeringInput * speedFactor, -steeringPower, steeringPower);
        float rotationTorque = clampedSteering - rb.angularVelocity.y;
        rb.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);
    }

    public void ApplyAcceleration(float accelerationInput)
    {
        bool isGameStarted = _gameController.isGameStarted;
        bool isGrounded = groundDetection.IsGrounded;

        if (!isGrounded || !CanMove || !isGameStarted)
            return;

        float forceMagnitude = accelerationInput * acceleration;
        Vector3 forward = _cachedTransform.forward;
        rb.AddForce(forward * forceMagnitude, ForceMode.Acceleration);

        float currentSpeed = rb.velocity.magnitude;
        if (currentSpeed > maxSpeed)
        {
            float decelerationFactor = forceMagnitude * (currentSpeed - maxSpeed) / maxSpeed;
            rb.AddForce(-forward * decelerationFactor, ForceMode.Acceleration);
        }
    }


    private void SpinWheels()
    {
        float rotationSpeed = ForwardSpeed * wheelsRotationSpeed * Time.deltaTime;
        FLwheel.Rotate(Vector3.right, rotationSpeed);
        FRwheel.Rotate(Vector3.right, rotationSpeed);
        BLwheel.Rotate(Vector3.right, rotationSpeed);
        BRwheel.Rotate(Vector3.right, rotationSpeed);
    }

    private void OnDrawGizmosSelected() => groundDetection.OnDrawGizmosSelected(GetComponent<Vehicle>());
}