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

    [SerializeField]
    private float turnBrakingDistance = 10f; // Дистанция, на которой начинаем тормозить перед поворотом

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

    private VehicleMovement vehicleMovement;
    private float currentSpeedMultiplier = 1f;
    internal Coroutine resetCoroutine;
    private float steeringPowerValue;
    private int currentTurnIndex = 0;

    private void Start()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
    }

    private void FixedUpdate()
    {
        bool isBraking = ApplyTurnBraking();

        if (!isBraking)
            vehicleMovement.ApplyAcceleration(accelerationFactor * currentSpeedMultiplier);

        vehicleMovement.ApplySteering(steeringPowerValue);

        // Если скорость очень мала, обнуляем движение
        if (vehicleMovement.rb.velocity.sqrMagnitude < 0.25f && currentSpeedMultiplier <= 0f)
        {
            vehicleMovement.rb.velocity = Vector3.zero;
            vehicleMovement.rb.angularVelocity = Vector3.zero;
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (vehicleMovement.CanMove &&
            PathHolder.Instance.pathCreator != null &&
            GameController.Instance.isGameStarted)
        {
            if (DetectObstacle(out Vector3 avoidanceDirection))
            {
                AvoidObstacle(avoidanceDirection);
            }
            else
            {
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

        if (!Physics.Raycast(transform.position + Vector3.up * 1.0f,
                Vector3.down, out RaycastHit hit, 2f, groundLayer) ||
            hit.collider.tag != "Ground")
        {
            vehicleMovement.ResetCarPosition();
        }

        resetCoroutine = null;
    }

    private void FollowCachedPath()
    {
        List<TurnInfo> cachedTurns = PathHolder.Instance.cachedTurns;
        Vector3 targetPoint;

        if (currentTurnIndex < cachedTurns.Count)
        {
            // Выбираем следующий поворот из кэша
            float turnDistance = cachedTurns[currentTurnIndex].distance;
            targetPoint = PathHolder.Instance.GetPointAtDistance(turnDistance);

            // Если машина достаточно близко к точке поворота, переходим к следующему
            if (Vector3.Distance(transform.position, targetPoint) < 20f)
            {
                currentTurnIndex++;
            }
        }
        else
        {
            // Если поворотов больше нет, цель – конец пути
            targetPoint = PathHolder.Instance.GetPointAtDistance(PathHolder.Instance.TotalPathLength);
        }

        // Сохраняем текущую высоту машины
        targetPoint.y = transform.position.y;

        Vector3 targetDirection = (targetPoint - transform.position).normalized;
        float targetAngle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);

        // Прямое рулевое воздействие (можно добавить сглаживание, если требуется)
        steeringPowerValue = targetAngle * steeringSensitivity * vehicleMovement.steeringPower;
    }

    /// <summary>
    /// Применяет торможение перед поворотом.
    /// Если расстояние до поворота меньше turnBrakingDistance и скорость выше допустимой,
    /// применяется торможение и возвращается true.
    /// </summary>
    private bool ApplyTurnBraking()
    {
        List<TurnInfo> cachedTurns = PathHolder.Instance.cachedTurns;
        bool brakingApplied = false;

        if (currentTurnIndex < cachedTurns.Count)
        {
            float turnDistance = cachedTurns[currentTurnIndex].distance;
            Vector3 turnPoint = PathHolder.Instance.GetPointAtDistance(turnDistance);
            float distanceToTurn = Vector3.Distance(transform.position, turnPoint);
            if (distanceToTurn <= turnBrakingDistance)
            {
                // Вычисляем максимально допустимую скорость для входа в поворот
                float allowedTurnSpeed = GetMaxSpeedForTurn(cachedTurns[currentTurnIndex].angle);
                if (vehicleMovement.rb.velocity.magnitude > allowedTurnSpeed)
                {
                    vehicleMovement.ApplyBraking(brakingPower);
                    brakingApplied = true;
                }
            }
        }

        if (!brakingApplied)
        {
            vehicleMovement.ApplyBraking(0f);
        }

        return brakingApplied;
    }

    /// <summary>
    /// Вычисляет максимально допустимую скорость для входа в поворот в зависимости от его угла.
    /// При угле 0 автомобиль может ехать со скоростью maxTurnSpeed,
    /// а при максимальном угле – замедляется до minTurnSpeed.
    /// </summary>
    private float GetMaxSpeedForTurn(float turnAngle)
    {
        float angleRatio = Mathf.Clamp01(turnAngle / maxTurnAngle);
        // Метод возвращает скорость, не превышать которую можно при данном угле поворота
        return Mathf.Lerp(maxTurnSpeed, minTurnSpeed, angleRatio);
    }

    /// <summary>
    /// Обнаруживает препятствия с использованием нескольких лучей (raycast).
    /// </summary>
    private bool DetectObstacle(out Vector3 avoidanceDirection)
    {
        avoidanceDirection = Vector3.zero;
        Vector3 origin = transform.position;
        Vector3 rayOrigin = origin + transform.forward * 1.0f;

        float angleStep = detectionAngle / (rayCount - 1);
        float startAngle = -detectionAngle / 2f;

        RaycastHit[] hits = new RaycastHit[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 rayDirection = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
            int hitCount =
                Physics.RaycastNonAlloc(origin, rayDirection, hits, obstacleDetectionDistance, obstacleLayerMask);
            for (int j = 0; j < hitCount; j++)
            {
                if (hits[j].collider.CompareTag("Obstacle") || hits[j].collider.CompareTag("PlayerMesh"))
                {
                    // Определяем, с какой стороны находится препятствие
                    Vector3 localHitPoint = transform.InverseTransformPoint(hits[j].point);
                    avoidanceDirection = (localHitPoint.x < 0f) ? transform.right : -transform.right;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Применяет обход препятствия посредством добавления крутящего момента.
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
        // Рисуем лучи для обнаружения препятствий
        Gizmos.color = Color.red;
        float angleStep = detectionAngle / (rayCount - 1);
        float startAngle = -detectionAngle / 2f;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * obstacleDetectionDistance);
        }

        // Рисуем луч для проверки земли под машиной
        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        Vector3 rayEnd = rayStart + Vector3.down * 2f;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(rayStart, rayEnd);
    }
#endif
}