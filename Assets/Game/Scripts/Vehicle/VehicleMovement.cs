using UnityEngine;
public class VehicleMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float fallGravity = 50f;
    [Range(0, 3)] [SerializeField] internal float steeringPower = 1.5f;
    [Range(0, 1)] [SerializeField] internal float grip = 1f;
    [SerializeField] internal VehicleGroundDetection groundDetection = new VehicleGroundDetection();

    internal Rigidbody _rb;
    private VehicleGroundDetection _groundDetection;
    private FloatingJoystick _joystick;
    private GameController _gameController;
    internal Vehicle _vehicle;
    private int lastClosestPathIndex;
    internal float NormalizedForwardSpeed => Mathf.Abs(ForwardSpeed) > 0.1f ? ForwardSpeed / maxSpeed : 0f;
    internal bool CanMove { get; set; } = true;
    internal float ForwardSpeed => Vector3.Dot(_rb.linearVelocity, transform.forward);
    public float CurrentSteeringInput { get; private set; }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _vehicle = GetComponent<Vehicle>();
        _groundDetection = groundDetection;
        _joystick = FindAnyObjectByType<FunctionalButtons>().floatingJoystick;
        _gameController = GameController.Instance;
        groundDetection.Initialize(_vehicle);
    }

    private void FixedUpdate()
    {
        if (!_vehicle.isLocalPlayer || !_gameController.isGameStarted) return;
        CurrentSteeringInput = _joystick.Horizontal;
        float accelerationInput = _joystick.Vertical;

        ApplyAcceleration(accelerationInput);
        ApplyLateralFriction(groundDetection.IsGrounded);
        ApplyGravity(groundDetection.IsGrounded);
        PerformGroundCheck();
    }

    private void PerformGroundCheck() => groundDetection.CheckGround();

    private void ApplyGravity(bool isGrounded)
    {
        _rb.AddForce(Vector3.down * (isGrounded ? gravity : fallGravity), ForceMode.Acceleration);
    }

    internal void ApplySteering(float steeringInput)
    {
        if (!_groundDetection.IsGrounded || !CanMove) return;

        // Получаем скорость в локальных координатах (ось Z – вперед)
        Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
        if (Mathf.Abs(localVelocity.z) < .5f)
        {
            // Если скорость слишком мала, обнуляем угловую скорость (опционально)
            _rb.angularVelocity = Vector3.zero;
            return;
        }

        float steeringForce = Mathf.Clamp(steeringInput, -steeringPower, steeringPower);
        float rotationTorque = steeringForce - _rb.angularVelocity.y;
        _rb.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);
    }


    internal void ApplyAcceleration(float accelerationInput)
    {
        if (!_groundDetection.IsGrounded || !CanMove) return;

        Vector3 forwardForce = transform.forward * (accelerationInput * acceleration);
        _rb.AddForce(forwardForce, ForceMode.Acceleration);

        if (_rb.linearVelocity.magnitude > maxSpeed)
            _rb.AddForce(-transform.forward * ((_rb.linearVelocity.magnitude - maxSpeed) * acceleration),
                ForceMode.Acceleration);
    }

    private void ApplyLateralFriction(bool isGrounded)
    {
        if (!isGrounded) return;
        Vector3 lateralFriction = -transform.right * (Vector3.Dot(_rb.linearVelocity, transform.right) * grip);
        _rb.AddForce(lateralFriction, ForceMode.Acceleration);
    }

    public void ApplyBraking(float brakePower)
    {
        Vector3 brakeForce = -_rb.linearVelocity.normalized * brakePower;
        _rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    public void ResetCarPosition()
    {
        var ezPath = EzPath.Instance;
        var pointTransform = ezPath.GetNextNearestPoint(transform);
        transform.position = pointTransform.pointTransform.position;
        transform.rotation = pointTransform.pointTransform.rotation;

        CanMove = true;
        _vehicle.healthController.Heal(100);
    }

    private void OnDrawGizmosSelected() => groundDetection.OnDrawGizmosSelected(GetComponent<Vehicle>());
}