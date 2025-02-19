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

    [Header("Reverse Settings")] [SerializeField]
    private float stuckVelocityThreshold = 0.1f; // Если скорость меньше – считаем, что бот не движется

    [SerializeField]
    private float stuckTimeThreshold = 2f; // Время, которое бот должен «не двигаться», чтобы считаться застрявшим

    [SerializeField] private float reverseDuration = 2f; // Время реверса (движение назад)
    [SerializeField] private float reverseAccelerationFactor = 1f; // Сила ускорения при движении назад

    private VehicleMovement vehicleMovement;
    private float currentSpeedMultiplier = 1f;
    internal Coroutine resetCoroutine;
    private float steeringPowerValue;
    private int currentTurnIndex = 0;

    // Переменные для определения застревания
    private float stuckTimer = 0f;

    private enum BotState
    {
        Normal,
        Reversing
    }

    private BotState currentState = BotState.Normal;

    private void Start()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
    }

    private void FixedUpdate()
    {
        if (currentState == BotState.Normal)
        {
            bool isBraking = ApplyTurnBraking();
            if (!isBraking)
                vehicleMovement.ApplyAcceleration(accelerationFactor * currentSpeedMultiplier);

            vehicleMovement.ApplySteering(steeringPowerValue);

            // Если скорость очень мала – обнуляем движение (в нормальном режиме)
            if (vehicleMovement.rb.linearVelocity.sqrMagnitude < 0.25f && currentSpeedMultiplier <= 0f)
            {
                vehicleMovement.rb.linearVelocity = Vector3.zero;
                vehicleMovement.rb.angularVelocity = Vector3.zero;
            }

            FollowPath();

            if (vehicleMovement.rb.linearVelocity.magnitude < stuckVelocityThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckTimeThreshold)
                {
                    StartCoroutine(ReverseRoutine());
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else if (currentState == BotState.Reversing)
        {
            vehicleMovement.ApplyAcceleration(-reverseAccelerationFactor);
            vehicleMovement.ApplySteering(0f);
        }

        if (resetCoroutine == null)
            resetCoroutine = StartCoroutine(ResetCarPosition());
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


    internal IEnumerator ResetCarPosition()
    {
        yield return new WaitForSeconds(carResetTime);
        if (!vehicleMovement.groundDetection.IsGrounded)
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
            // Берём следующий поворот из кэша
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

        // Вычисляем рулевой отклонение
        steeringPowerValue = targetAngle * steeringSensitivity * vehicleMovement.steeringPower;
    }

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
                // Вычисляем максимально допустимую скорость для поворота
                float allowedTurnSpeed = GetMaxSpeedForTurn(cachedTurns[currentTurnIndex].angle);
                if (vehicleMovement.rb.linearVelocity.magnitude > allowedTurnSpeed)
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
    /// Вычисляет максимально допустимую скорость для входа в поворот.
    /// </summary>
    private float GetMaxSpeedForTurn(float turnAngle)
    {
        float angleRatio = Mathf.Clamp01(turnAngle / maxTurnAngle);
        return Mathf.Lerp(maxTurnSpeed, minTurnSpeed, angleRatio);
    }

    /// <summary>
    /// Обнаруживает препятствия с использованием нескольких лучей (raycast).
    /// </summary>
    private bool DetectObstacle(out Vector3 avoidanceDirection)
    {
        avoidanceDirection = Vector3.zero;
        Vector3 origin = transform.position;

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
    /// Применяет обход препятствия посредством крутящего момента.
    /// </summary>
    private void AvoidObstacle(Vector3 avoidanceDirection)
    {
        float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
        float steeringValue = angleToAvoidance * avoidanceStrength * vehicleMovement.steeringPower;
        vehicleMovement.rb.AddRelativeTorque(0f, steeringValue, 0f, ForceMode.Acceleration);
    }

    /// <summary>
    /// Корутин, который переводит бота в режим реверса на заданное время, а затем возвращает в нормальный режим.
    /// </summary>
    private IEnumerator ReverseRoutine()
    {
        currentState = BotState.Reversing;
        float timer = 0f;
        while (timer < reverseDuration)
        {
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        currentState = BotState.Normal;
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
    }
#endif
}