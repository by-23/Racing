using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;

public class BotAI : MonoBehaviour
{
    [Header("Vehicle Settings")] [SerializeField]
    private float steeringSensitivity = 1f;

    [SerializeField] private float accelerationFactor = 1f;


    [Header("Turn Braking")] [SerializeField]
    private float brakingPower = 10f;

    [SerializeField] private float minTurnAngle = 5f;

    [Header("Turn Detection")] [SerializeField]
    private float turnScanDistance = 20f;

    [SerializeField] private float turnBrakingDistance = 10f;

    [Header("Turn Speed Settings")] [SerializeField]
    private float maxTurnSpeed = 60f;

    [SerializeField] private float minTurnSpeed = 20f;
    [SerializeField] private float maxTurnAngle = 90f;

    [Header("Obstacle Handling")] [SerializeField]
    private float obstacleDetectionDistance = 5f;

    [SerializeField] private float avoidanceStrength = 1f;
    [SerializeField] private int rayCount = 5;
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float carResetTime;

    [Header("Path Deviation")] [SerializeField]
    private float maxLateralDeviation = 2f; // Максимальное боковое отклонение

    [SerializeField] private float deviationUpdateInterval = 2f; // Интервал обновления отклонения

    private Vehicle vehicle;
    private VehicleMovement vehicleMovement;
    private Vector3 targetPosition;
    private Vector3 lastAvoidanceDirection;
    private float currentSpeedMultiplier = 1f;

    // Параметры для случайного отклонения от маршрута
    private float currentLateralDeviation = 0f;
    private float deviationTimer = 0f;

    internal Coroutine resetCoroutine;
    private float steeringPowerValue;

    private void Start()
    {
        vehicle = GetComponent<Vehicle>();
        vehicleMovement = GetComponent<VehicleMovement>();
    }

    /// <summary>
    /// Разбивает путь на точки с шагом pathSampleStep и вычисляет накопленные расстояния.
    /// </summary>
    private void FixedUpdate()
    {
        ApplyTurnBraking();

        vehicleMovement.ApplyAcceleration(accelerationFactor * currentSpeedMultiplier);
        vehicleMovement.ApplySteering(steeringPowerValue);

        // Используем sqrMagnitude для оптимизации (0.5^2 = 0.25)
        if (vehicleMovement.rb.velocity.sqrMagnitude < 0.25f && currentSpeedMultiplier <= 0f)
        {
            vehicleMovement.rb.velocity = Vector3.zero;
            vehicleMovement.rb.angularVelocity = Vector3.zero;
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (vehicleMovement.CanMove && PathHolder.Instance.pathCreator != null &&
            GameController.Instance.isGameStarted)
        {
            if (DetectObstacle(out Vector3 avoidanceDirection))
            {
                AvoidObstacle(avoidanceDirection);
                lastAvoidanceDirection = avoidanceDirection;
            }
            else
            {
                lastAvoidanceDirection = Vector3.zero;
                UpdateLateralDeviation();
                FollowCachedPath();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Explosive explosive) && resetCoroutine == null)
            resetCoroutine = StartCoroutine(ResetCarPosition());
    }

    internal IEnumerator ResetCarPosition()
    {
        yield return new WaitForSeconds(carResetTime);

        if (!Physics.Raycast(transform.position + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2f,
                groundLayer) ||
            hit.collider.tag != "Ground")
        {
            vehicleMovement.ResetCarPosition();
        }

        resetCoroutine = null;
    }

    /// <summary>
    /// Обновляет значение бокового отклонения через заданный интервал времени.
    /// </summary>
    private void UpdateLateralDeviation()
    {
        deviationTimer += Time.deltaTime;
        if (deviationTimer >= deviationUpdateInterval)
        {
            // Новое случайное отклонение в диапазоне [-maxLateralDeviation, maxLateralDeviation]
            currentLateralDeviation = Random.Range(-maxLateralDeviation, maxLateralDeviation);
            deviationTimer = 0f;
        }
    }

    /// <summary>
    /// Следование по маршруту с добавлением небольшого бокового смещения.
    /// Находим ближайшую точку к текущей позиции и вычисляем целевую точку по lookAheadDistance.
    /// Затем вычисляем вектор тангенса и смещаем целевую точку вбок.
    /// </summary>
    private void FollowCachedPath()
    {
        float currentDistance = PathHolder.Instance.FindClosestDistance(out vehicleMovement.lastClosestPathIndex,
            transform.position, vehicleMovement.lastClosestPathIndex);
        float targetDistance = Mathf.Min(currentDistance + PathHolder.Instance.lookAheadDistance,
            PathHolder.Instance.totalPathLength);
        targetPosition = PathHolder.Instance.GetPointAtDistance(targetDistance);

        // Вычисляем тангенс маршрута в точке targetPosition (используем небольшую дельту для интерполяции)
        float nextDistance = Mathf.Min(targetDistance + 0.1f, PathHolder.Instance.totalPathLength);
        Vector3 forwardOnPath = (PathHolder.Instance.GetPointAtDistance(nextDistance) - targetPosition).normalized;

        // Вычисляем вектор, перпендикулярный направлению движения (с учётом оси Y)
        Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardOnPath).normalized;

        // Добавляем к целевой точке смещение по боковой оси
        targetPosition += lateralDirection * currentLateralDeviation;

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        directionToTarget.y = 0f;

        float targetAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        steeringPowerValue = targetAngle * steeringSensitivity * vehicleMovement.steeringPower;
    }


    private void ApplyTurnBraking()
    {
        if (PathHolder.Instance.TryGetUpcomingTurnInfo(out float upcomingTurnAngle, out float distanceToTurn,
                turnScanDistance, turnBrakingDistance, minTurnAngle, transform.position,
                vehicleMovement.lastClosestPathIndex))
        {
            if (distanceToTurn <= turnBrakingDistance)
            {
                float minSpeedForTurn = GetMinSpeedForTurn(upcomingTurnAngle);
                if (vehicleMovement.rb.velocity.magnitude > minSpeedForTurn)
                {
                    vehicleMovement.ApplyBraking(brakingPower);
                }
            }
        }
        else
        {
            vehicleMovement.ApplyBraking(0f);
        }
    }

    private float GetMinSpeedForTurn(float turnAngle)
    {
        float angleRatio = Mathf.Clamp01(turnAngle / maxTurnAngle);
        return Mathf.Lerp(maxTurnSpeed, minTurnSpeed, angleRatio);
    }


    /// <summary>
    /// Обнаруживает препятствия с тегом "Obstacle" с использованием Physics.RaycastNonAlloc.
    /// </summary>
    private bool DetectObstacle(out Vector3 avoidanceDirection)
    {
        avoidanceDirection = Vector3.zero;
        Vector3 origin = transform.position;
        // Точка, немного вперёд от машины
        Vector3 rayOrigin = origin + transform.forward * 1.0f;

        float angleStep = detectionAngle / (rayCount - 1);
        float startAngle = -detectionAngle / 2f;

        // Используем фиксированный массив для хранения результатов
        RaycastHit[] hits = new RaycastHit[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 rayDirection = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
            int hitCount =
                Physics.RaycastNonAlloc(origin, rayDirection, hits, obstacleDetectionDistance, obstacleLayerMask);
            for (int j = 0; j < hitCount; j++)
            {
                if (hits[j].collider.CompareTag("Obstacle"))
                {
                    // Преобразуем hit.point в локальные координаты для определения стороны
                    Vector3 localHitPoint = transform.InverseTransformPoint(hits[j].point);
                    avoidanceDirection = (localHitPoint.x < 0f) ? transform.right : -transform.right;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Поворачивает транспортное средство для обхода обнаруженного препятствия.
    /// </summary>
    private void AvoidObstacle(Vector3 avoidanceDirection)
    {
        float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
        float steeringValue = angleToAvoidance * avoidanceStrength * vehicleMovement.steeringPower;
        vehicleMovement.rb.AddRelativeTorque(0f, steeringValue, 0f, ForceMode.Acceleration);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (PathHolder.Instance.pathCreator != null && PathHolder.Instance.cachedPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }

        Gizmos.color = Color.red;
        float angleStep = detectionAngle / (rayCount - 1);
        float startAngle = -detectionAngle / 2f;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * obstacleDetectionDistance);
        }

        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        Vector3 rayEnd = rayStart + Vector3.down * 2f;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(rayStart, rayEnd);
    }
#endif
}