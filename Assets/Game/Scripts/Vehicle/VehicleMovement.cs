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
    [Header("Flip Settings")]
    [SerializeField] private float flipTorque = 15f;
    [SerializeField, Range(0, 1)] private float flipCheckAngle = 0.5f;

    internal Rigidbody _rb;
    private VehicleGroundDetection _groundDetection;
    private GameController _gameController;
    internal Vehicle _vehicle;
    private int _lastPassedPointIndex = 0;
    private int _currentTargetPointIndex = 1;

    public float ProgressDistance
    {
        get
        {
            var path = EzPath.Instance;
            if (path == null || path.pathPoints.Length < 2 || _currentTargetPointIndex >= path.pathPoints.Length)
                return _lastPassedPointIndex;

            Vector3 lastPointPos = path.pathPoints[_lastPassedPointIndex].pointTransform.position;
            Vector3 currentTargetPos = path.pathPoints[_currentTargetPointIndex].pointTransform.position;

            Vector3 segmentVector = currentTargetPos - lastPointPos;
            float segmentLength = segmentVector.magnitude;
            if (segmentLength < 0.001f)
                return _lastPassedPointIndex;

            Vector3 carVector = transform.position - lastPointPos;
            float progressOnSegment = Mathf.Clamp01(Vector3.Dot(carVector, segmentVector.normalized) / segmentLength);

            return _lastPassedPointIndex + progressOnSegment;
        }
    }

    internal float HorizontalInput { get; set; }
    internal float VerticalInput { get; set; }
    internal float NormalizedForwardSpeed => Mathf.Abs(ForwardSpeed) > 0.1f ? ForwardSpeed / maxSpeed : 0f;
    internal bool CanMove { get; set; } = true;
    internal float ForwardSpeed => Vector3.Dot(_rb.linearVelocity, transform.forward);
    public float CurrentSteeringInput { get; private set; }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _vehicle = GetComponent<Vehicle>();
        _groundDetection = groundDetection;
        _gameController = GameController.Instance;
        groundDetection.Initialize(_vehicle);
    }

    private void FixedUpdate()
    {
        if (!_vehicle.isLocalPlayer || !_gameController.isGameStarted) return;

        CheckIfPassedTargetPoint();
        ApplyFlipTorque(HorizontalInput, VerticalInput);
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

    private void ApplyFlipTorque(float horizontalInput, float verticalInput)
    {
        if (groundDetection.IsGrounded) return;

        float upDot = Vector3.Dot(transform.up, Vector3.up);

        if (Mathf.Abs(upDot) < flipCheckAngle && verticalInput > 0.1f && Mathf.Abs(horizontalInput) > 0.1f)
        {
            _rb.AddRelativeTorque(new Vector3(0, 0, horizontalInput * flipTorque), ForceMode.Acceleration);
        }
    }

    public void ApplyBraking(float brakePower)
    {
        Vector3 brakeForce = -_rb.linearVelocity.normalized * brakePower;
        _rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    public void ResetCarPosition()
    {
        var ezPath = EzPath.Instance;
        if (ezPath == null || ezPath.pathPoints.Length == 0) return;

        var nearestPoint = ezPath.GetNearestPoint(transform);
        int targetIndex = nearestPoint.index;

        // Если ближайшая точка находится дальше нашей текущей цели (срезали путь),
        // или если она находится позади уже пройденной точки,
        // то мы принудительно возвращаем игрока на последнюю пройденную точку.
        if (targetIndex > _currentTargetPointIndex || targetIndex < _lastPassedPointIndex)
        {
            targetIndex = _lastPassedPointIndex;
        }

        // Получаем данные о точке, к которой будем телепортироваться
        var teleportTargetPoint = ezPath.pathPoints[targetIndex];

        // Для этой точки всегда нужно пересчитывать угол, чтобы она смотрела на следующую.
        // Это гарантирует правильную ориентацию и после срезки, и после отката назад.
        if (targetIndex + 1 < ezPath.pathPoints.Length)
        {
            var nextPoint = ezPath.pathPoints[targetIndex + 1];
            Vector3 direction = (nextPoint.pointTransform.position - teleportTargetPoint.pointTransform.position).normalized;
            if (direction != Vector3.zero)
                teleportTargetPoint.angle = Quaternion.LookRotation(direction).eulerAngles.y;
        }

        // Выполняем телепортацию
        transform.position = teleportTargetPoint.pointTransform.position;
        transform.rotation = Quaternion.Euler(0, teleportTargetPoint.angle, 0);

        // Обновляем индексы прогресса
        _lastPassedPointIndex = targetIndex;
        _currentTargetPointIndex = _lastPassedPointIndex + 1;

        // Безопасная проверка на случай, если мы у последней точки пути
        if (_currentTargetPointIndex >= ezPath.pathPoints.Length)
        {
            _currentTargetPointIndex = ezPath.pathPoints.Length - 1;
        }

        CanMove = true;
        _vehicle.healthController.Heal(100);
    }

    private void CheckIfPassedTargetPoint()
    {
        var path = EzPath.Instance;
        // Проверяем, есть ли смысл в проверке
        if (path == null || path.pathPoints.Length < 2 || _currentTargetPointIndex >= path.pathPoints.Length)
            return;

        // Определяем текущий отрезок пути
        Vector3 lastPointPos = path.pathPoints[_lastPassedPointIndex].pointTransform.position;
        Vector3 currentTargetPos = path.pathPoints[_currentTargetPointIndex].pointTransform.position;

        Vector3 segmentVector = currentTargetPos - lastPointPos;
        if (segmentVector.sqrMagnitude < 0.001f) return; // Отрезок слишком мал

        // Проецируем вектор от начала отрезка до машины на сам отрезок
        Vector3 carVector = transform.position - lastPointPos;
        float projection = Vector3.Dot(carVector, segmentVector);

        // Если длина проекции больше квадрата длины отрезка, значит, машина прошла целевую точку
        if (projection > segmentVector.sqrMagnitude)
        {
            _lastPassedPointIndex = _currentTargetPointIndex;
            _currentTargetPointIndex++;
            
            // Сообщаем контроллеру, что произошло событие, которое может повлиять на рейтинг
            GameController.Instance.ReportCheckpointPassed();

            if (_currentTargetPointIndex >= path.pathPoints.Length)
            {
                GameController.Instance.VehicleFinished(_vehicle);
            }
        }
    }

    private void OnDrawGizmosSelected() => groundDetection.OnDrawGizmosSelected(GetComponent<Vehicle>());
}