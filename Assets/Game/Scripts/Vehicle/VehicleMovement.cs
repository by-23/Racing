using PathCreation;
using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    [SerializeField] internal Rigidbody rb;
    [SerializeField] internal PathCreator pathCreator;
    [SerializeField] internal VehicleGroundDetection groundDetection = new VehicleGroundDetection();
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float fallGravity = 50f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float acceleration = 20f;

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

    [Range(0, 1)] [SerializeField] private float grip = 1f;

    internal bool CanMove { get; set; } = true;
    private float ForwardSpeed => Vector3.Dot(rb.linearVelocity, transform.forward);
    internal float NormalizedForwardSpeed => Mathf.Abs(ForwardSpeed) > 0.1f ? ForwardSpeed / maxSpeed : 0f;

    private int lastClosestPathIndex = 0;
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
        // Если pathCreator не назначен в инспекторе, пытаемся получить его из PathHolder
        if (pathCreator == null)
            pathCreator = (PathHolder.Instance != null)
                ? PathHolder.Instance.pathCreator
                : FindAnyObjectByType<PathCreator>();

        _cachedTransform = transform;
        _gameController = GameController.Instance;
    }

    private void FixedUpdate()
    {
        if (!vehicle.isLocalPlayer)
            return;

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
        var pathHolder = PathHolder.Instance;
        if (pathHolder != null && pathHolder.pathCreator != null)
        {
            int bestSegmentIndex;
            float closestDistance = pathHolder.FindClosestDistance(out bestSegmentIndex, transform.position, 0);
            transform.position = pathHolder.GetPointAtDistance(closestDistance);

            // Вычисляем точку впереди по маршруту (lookahead)
            float lookaheadDistance = 0.1f; // можно настроить это значение
            float totalPathLength = pathHolder.TotalPathLength;
            float nextDistance = Mathf.Clamp(closestDistance + lookaheadDistance, 0f, totalPathLength);
            Vector3 lookAheadPoint = pathHolder.GetPointAtDistance(nextDistance);

            // Определяем направление от текущей позиции к следующей точке
            Vector3 direction = (lookAheadPoint - transform.position).normalized;
            if (direction == Vector3.zero)
            {
                // Если не удалось вычислить направление, используем касательную к пути
                direction = pathHolder.pathCreator.path.GetTangent((int)closestDistance);
                if (direction == Vector3.zero)
                    direction = transform.forward;
            }

            transform.rotation = Quaternion.LookRotation(direction);
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
        Vector3 brakeForce = -rb.linearVelocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    public void TurnWheels(float turnInput)
    {
        float targetTurnAngle = turnInput * wheelsTurnPercentage;
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, Time.deltaTime * wheelsTurnSpeed);

        // Передние колёса поворачиваются в одну сторону, задние — в противоположную
        FLwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        FRwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        BLwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
        BRwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
    }

    private void PerformGroundCheck() => groundDetection.CheckGround();

    private void ApplyGravity(bool isGrounded)
    {
        float appliedGravity = isGrounded ? gravity : fallGravity;
        rb.AddForce(Vector3.down * appliedGravity, ForceMode.Acceleration);
    }

    private void ApplyLateralFriction(bool isGrounded)
    {
        if (!isGrounded)
            return;
        Vector3 right = _cachedTransform.right;
        float lateralSpeed = Vector3.Dot(rb.linearVelocity, right);
        Vector3 lateralFriction = -right * ((lateralSpeed / Time.fixedDeltaTime) * grip);
        rb.AddForce(lateralFriction, ForceMode.Acceleration);
    }

    public void ApplySteering(float steeringInput)
    {
        if (!groundDetection.IsGrounded || !CanMove || !_gameController.isGameStarted)
            return;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, _cachedTransform.forward);
        float speedFactor = forwardSpeed * 0.075f;
        float clampedSteering = Mathf.Clamp(steeringInput * speedFactor, -steeringPower, steeringPower);
        float rotationTorque = clampedSteering - rb.angularVelocity.y;
        rb.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);
    }

    public void ApplyAcceleration(float accelerationInput)
    {
        if (!groundDetection.IsGrounded || !CanMove || !_gameController.isGameStarted)
            return;

        float forceMagnitude = accelerationInput * acceleration;
        Vector3 forward = _cachedTransform.forward;
        rb.AddForce(forward * forceMagnitude, ForceMode.Acceleration);

        // Если превышена максимальная скорость, применяем компенсацию
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed > maxSpeed)
        {
            float decelerationFactor = forceMagnitude * (currentSpeed - maxSpeed) / maxSpeed;
            rb.AddForce(-forward * decelerationFactor, ForceMode.Acceleration);
        }
    }

    private void SpinWheels()
    {
        float rotationAmount = ForwardSpeed * wheelsRotationSpeed * Time.deltaTime;
        FLwheel.Rotate(Vector3.right, rotationAmount);
        FRwheel.Rotate(Vector3.right, rotationAmount);
        BLwheel.Rotate(Vector3.right, rotationAmount);
        BRwheel.Rotate(Vector3.right, rotationAmount);
    }

    private void OnDrawGizmosSelected() => groundDetection.OnDrawGizmosSelected(GetComponent<Vehicle>());
}