using UnityEngine;

public class BotObstacleHandler
{
    private BotAI bot;

    // Локальные переменные для логики объезда
    private Vector3 detourPoint = Vector3.zero;
    private float lastObstacleCheckTime = 0f;
    private bool isObstacleStillPresent = false;
    private float lastDetourTime = 0f;

    public BotObstacleHandler(BotAI bot)
    {
        this.bot = bot;
    }

    public Vector3 DetourPoint
    {
        get { return detourPoint; }
    }

    public void HandleTriggerStay(Collider other, Transform botTransform)
    {
        if (!ShouldProcessObstacle(other))
            return;

        if (IsObstacleBlockingPath(other, botTransform))
        {
            UpdateDetourPoint(other, botTransform);
        }
    }

    private bool ShouldProcessObstacle(Collider other)
    {
        return GameController.Instance.isGameStarted &&
               other.CompareTag("Obstacle") &&
               bot.currentState != BotAI.BotState.Detouring &&
               Time.time > lastDetourTime + bot.detourCooldown;
    }

    private bool IsObstacleBlockingPath(Collider obstacle, Transform botTransform)
    {
        Vector3 toTarget = bot.currentNormalTarget - botTransform.position;
        RaycastHit hit;

        if (Physics.Raycast(botTransform.position, toTarget.normalized,
                out hit, Mathf.Min(toTarget.magnitude, bot.pathBlockCheckDistance), bot.obstacleLayerMask))
        {
            return hit.collider == obstacle;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, что между двумя точками нет препятствий.
    /// </summary>
    public bool CheckForObstaclesBetweenPoints(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        return Physics.Raycast(start, direction, distance, bot.obstacleLayerMask);
    }

    private void UpdateDetourPoint(Collider obstacle, Transform botTransform)
    {
        // Определяем позицию и размер препятствия
        Vector3 obstaclePos = obstacle.bounds.center;
        Vector3 obstacleSize = obstacle.bounds.size;

        Vector3 toObstacle = obstaclePos - botTransform.position;
        Vector3 avoidanceDir = CalculateAvoidanceDirection(toObstacle, botTransform);

        // Расчет базовой дистанции объезда с учётом расстояния до препятствия и его размера
        float baseAvoidanceDistance = GetAvoidanceDistance(toObstacle.magnitude, obstacleSize);
        Vector3 candidateDetourPoint = botTransform.position + avoidanceDir * baseAvoidanceDistance;
        candidateDetourPoint.y = botTransform.position.y;

        // Проверяем, что между ботом и кандидатом нет препятствий
        bool clearToDetour = !CheckForObstaclesBetweenPoints(botTransform.position, candidateDetourPoint);
        if (clearToDetour && IsDetourPointValid(candidateDetourPoint))
        {
            detourPoint = candidateDetourPoint;
            bot.currentState = BotAI.BotState.Detouring;
            lastDetourTime = Time.time;
            isObstacleStillPresent = true;
        }
    }

    private Vector3 CalculateAvoidanceDirection(Vector3 toObstacle, Transform botTransform)
    {
        Vector3 rightPerp = Vector3.Cross(toObstacle.normalized, Vector3.up);
        float sideChoice = Vector3.Dot(rightPerp, botTransform.right) > 0 ? 1 : -1;
        return (botTransform.right * sideChoice + botTransform.forward).normalized;
    }

    // Расчет дистанции объезда с учетом расстояния до препятствия и его размера.
    private float GetAvoidanceDistance(float obstacleDistance, Vector3 obstacleSize)
    {
        // Добавляем величину препятствия как дополнительный запас для безопасного объезда.
        float sizeFactor = obstacleSize.magnitude;
        return Mathf.Clamp(obstacleDistance * bot.sideOffsetMultiplier + sizeFactor, 5f, 15f);
    }

    private bool IsDetourPointValid(Vector3 point)
    {
        return !Physics.CheckSphere(point, 2f, bot.obstacleLayerMask);
    }

    public void UpdateDetourState(Transform botTransform, Vector3 normalTarget)
    {
        if (Time.time - lastObstacleCheckTime > bot.obstacleRecheckInterval)
        {
            // Проверка: есть ли препятствия между текущей позицией бота и основной целью маршрута
            isObstacleStillPresent = CheckForObstaclesBetweenPoints(botTransform.position, normalTarget);
            lastObstacleCheckTime = Time.time;
        }

        if (!isObstacleStillPresent)
        {
            AbandonCurrentDetour();
            return;
        }

        if (Vector3.Distance(botTransform.position, detourPoint) < bot.detourThreshold)
        {
            bot.SetState(BotAI.BotState.Normal);
            return;
        }

        Vector3 toDetour = detourPoint - botTransform.position;
        float angleToDetour = Vector3.Angle(botTransform.forward, toDetour.normalized);

        if (angleToDetour > bot.reverseAngleThreshold)
        {
            ProcessReverseDetour(toDetour, botTransform);
        }
        else
        {
            ProcessForwardDetour(toDetour, botTransform);
        }
    }

    private void ProcessReverseDetour(Vector3 toDetour, Transform botTransform)
    {
        float targetAngle = Vector3.SignedAngle(-botTransform.forward, toDetour.normalized, Vector3.up);
        bot.SetSteering(targetAngle * bot.steeringSensitivity * bot.reverseSteeringMultiplier);
        ApplyAcceleration(botTransform, negativeAcceleration: true);
    }

    private void ProcessForwardDetour(Vector3 toDetour, Transform botTransform)
    {
        float targetAngle = Vector3.SignedAngle(botTransform.forward, toDetour.normalized, Vector3.up);
        bot.SetSteering(targetAngle * bot.steeringSensitivity);

        if (!bot.HasTurnBraking(botTransform))
            ApplyAcceleration(botTransform, negativeAcceleration: false);
    }

    private void ApplyAcceleration(Transform botTransform, bool negativeAcceleration)
    {
        VehicleMovement vm = botTransform.GetComponent<VehicleMovement>();
        if (negativeAcceleration)
            vm.ApplyAcceleration(-bot.accelerationFactor);
        else
            vm.ApplyAcceleration(bot.accelerationFactor);
    }

    private void AbandonCurrentDetour()
    {
        bot.SetState(BotAI.BotState.Normal);
        lastDetourTime = Time.time;
        isObstacleStillPresent = false;
    }
}