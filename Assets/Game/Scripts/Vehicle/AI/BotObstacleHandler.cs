using UnityEngine;

public class BotObstacleHandler
{
    private BotAI bot;

    // Локальные переменные для логики объезда
    private Vector3 detourPoint = Vector3.zero;
    private float lastObstacleCheckTime = 0f;
    private bool isObstacleStillPresent = false;

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
        if (!(GameController.Instance.isGameStarted &&
              bot.currentState != BotAI.BotState.Detouring))
            return;

        if (IsObstacleBlockingPath(botTransform))
        {
            UpdateDetourPoint(other, botTransform);
        }
    }

    private bool IsObstacleBlockingPath(Transform botTransform)
    {
        Vector3 toTarget = bot.currentNormalTarget - botTransform.position;
        RaycastHit hit;

        // Отладочный луч от бота к цели
        Debug.DrawRay(botTransform.position,
            toTarget.normalized * Mathf.Min(toTarget.magnitude, bot.pathBlockCheckDistance),
            Color.cyan, 0.5f);

        if (Physics.Raycast(botTransform.position, toTarget.normalized,
                out hit, Mathf.Min(toTarget.magnitude, bot.pathBlockCheckDistance)))
        {
            Debug.Log($"[Detour] Raycast hit: {hit.collider.name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет наличие препятствий между двумя точками.
    /// </summary>
    public bool CheckForObstaclesBetweenPoints(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        Debug.DrawLine(start, end, Color.magenta, 0.5f);
        return Physics.Raycast(start, direction, distance, bot.obstacleLayerMask);
    }

    private void UpdateDetourPoint(Collider obstacle, Transform botTransform)
    {
        // Позиция и размер препятствия
        Vector3 obstaclePos = obstacle.bounds.center;
        Vector3 obstacleSize = obstacle.bounds.size;
        Vector3 toObstacle = obstaclePos - botTransform.position;

        // Получаем текущую скорость бота (предполагается, что VehicleMovement содержит свойство CurrentSpeed)
        VehicleMovement vm = botTransform.GetComponent<VehicleMovement>();
        float currentSpeed = vm != null ? vm.rb.linearVelocity.magnitude : 0f;

        // Определяем сторону объезда: 1 – вправо, -1 – влево
        float sideChoice = CalculateSideChoice(toObstacle, botTransform);

        // Латеральное смещение определяется на основе размера препятствия:
        // Берем проекцию полного размера препятствия на вектор, перпендикулярный направлению движения бота,
        // делим на 2 (чтобы получить "половину" ширины) и прибавляем запас.
        float projectedWidth = Mathf.Abs(Vector3.Dot(obstacle.bounds.size, botTransform.right.normalized));
        float lateralOffset = (projectedWidth / 2f) + bot.sideOffsetMultiplier;

        // Фронтальное смещение зависит от скорости.
        // Чем быстрее движется бот, тем дальше вперед должна появиться точка,
        // но оно ограничено минимальным и максимальным значениями.
        float forwardDistance = Mathf.Clamp(currentSpeed * bot.detourDistanceMultiplier,
            bot.minDetourDistance, bot.maxDetourDistance);

        // Итоговая точка объезда = исходная позиция + смещение вперед + латеральное смещение.
        Vector3 candidateDetourPoint = botTransform.position +
                                       botTransform.forward * forwardDistance +
                                       botTransform.right * (sideChoice * lateralOffset);
        candidateDetourPoint.y = botTransform.position.y; // Сохраняем уровень по Y

        Debug.DrawLine(botTransform.position, candidateDetourPoint, Color.green, 2.0f);
        DrawDetourPointMarker(candidateDetourPoint, Color.green, 2.0f);

        // Проверяем, что путь к точке объезда свободен
        bool clearToDetour = !CheckForObstaclesBetweenPoints(botTransform.position, candidateDetourPoint);
        if (clearToDetour && IsDetourPointValid(candidateDetourPoint))
        {
            detourPoint = candidateDetourPoint;
            bot.currentState = BotAI.BotState.Detouring;
            isObstacleStillPresent = true;
        }
        else
        {
            Debug.Log("[Detour] Candidate detour point is blocked or invalid.");
        }
    }

    private float CalculateSideChoice(Vector3 toObstacle, Transform botTransform)
    {
        float dot = Vector3.Dot(toObstacle, botTransform.right);
        return dot > 0 ? -1f : 1f;
    }


    private bool IsDetourPointValid(Vector3 point)
    {
        bool valid = !Physics.CheckSphere(point, 2f, bot.obstacleLayerMask);
        if (!valid)
        {
            Debug.Log($"[Detour] Detour point {point} is invalid (inside obstacle).");
        }

        return valid;
    }

    public void UpdateDetourState(Transform botTransform, Vector3 normalTarget)
    {
        if (Time.time - lastObstacleCheckTime > bot.obstacleRecheckInterval)
        {
            RaycastHit hit;
            // Рейкаст вперёд от позиции бота на расстояние bot.pathBlockCheckDistance
            if (Physics.Raycast(botTransform.position, botTransform.forward, out hit, bot.pathBlockCheckDistance))
            {
                isObstacleStillPresent = true;
                Debug.Log($"[Detour] Obstacle detected: {hit.collider.name}");
            }
            else
            {
                isObstacleStillPresent = false;
            }

            lastObstacleCheckTime = Time.time;
        }


        // Если препятствие больше не мешает, отменяем объезд
        if (!isObstacleStillPresent)
        {
            AbandonCurrentDetour();
            return;
        }

        Debug.DrawLine(botTransform.position, normalTarget, Color.blue, 0.5f);

        // Если точка объезда оказалась позади бота, а препятствие отсутствует, отменяем объезд,
        // чтобы бот не начинал разворачиваться для возврата к ней.
        Vector3 toDetour = detourPoint - botTransform.position;
        if (Vector3.Dot(botTransform.forward, toDetour) < 0)
        {
            Debug.Log("[Detour] Detour point is behind the bot.");
            if (!isObstacleStillPresent)
            {
                bot.SetState(BotAI.BotState.Normal);
                Debug.Log("[Detour] Obstacle no longer present. Cancelling detour.");
                return;
            }
        }

        // Если бот уже достиг (или близок к) точки объезда, переключаемся на нормальный режим
        if (Vector3.Distance(botTransform.position, detourPoint) < bot.detourThreshold)
        {
            bot.SetState(BotAI.BotState.Normal);
            Debug.Log("[Detour] Reached detour point. Switching back to Normal state.");
            return;
        }

        // Определяем манёвр (прямой или задний) по скорости
        VehicleMovement vm = botTransform.GetComponent<VehicleMovement>();
        float currentSpeed = vm != null ? vm.rb.linearVelocity.magnitude : 0f;

        if (currentSpeed < bot.detourSpeedThreshold)
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
        float appliedSteering = targetAngle * bot.steeringSensitivity * bot.reverseSteeringMultiplier;
        bot.SetSteering(appliedSteering);
        Debug.Log($"[Detour] Reverse Detour: targetAngle = {targetAngle} (applied steering: {appliedSteering})");
        Debug.DrawRay(botTransform.position, -botTransform.forward * 5f, Color.red, 0.5f);
        ApplyAcceleration(botTransform, true);
    }

    private void ProcessForwardDetour(Vector3 toDetour, Transform botTransform)
    {
        float targetAngle = Vector3.SignedAngle(botTransform.forward, toDetour.normalized, Vector3.up);
        float appliedSteering = targetAngle * bot.steeringSensitivity;
        bot.SetSteering(appliedSteering);
        Debug.DrawRay(botTransform.position, botTransform.forward * 5f, Color.green, 0.5f);
        if (!bot.HasTurnBraking(botTransform))
            ApplyAcceleration(botTransform, false);
    }

    private void ApplyAcceleration(Transform botTransform, bool negativeAcceleration)
    {
        VehicleMovement vm = botTransform.GetComponent<VehicleMovement>();
        if (vm == null)
        {
            Debug.LogWarning("[Detour] VehicleMovement component not found on botTransform.");
            return;
        }

        if (negativeAcceleration)
            vm.ApplyAcceleration(-bot.accelerationFactor);
        else
            vm.ApplyAcceleration(bot.accelerationFactor);
    }

    private void AbandonCurrentDetour()
    {
        bot.SetState(BotAI.BotState.Normal);
        isObstacleStillPresent = false;
    }

    /// <summary>
    /// Рисует крест в указанной точке для визуальной отладки.
    /// </summary>
    /// <param name="position">Позиция точки.</param>
    /// <param name="color">Цвет линий.</param>
    /// <param name="duration">Время отображения линий.</param>
    private void DrawDetourPointMarker(Vector3 position, Color color, float duration)
    {
        float size = 0.5f;
        Debug.DrawLine(position + Vector3.up * size, position - Vector3.up * size, color, duration);
        Debug.DrawLine(position + Vector3.right * size, position - Vector3.right * size, color, duration);
        Debug.DrawLine(position + Vector3.forward * size, position - Vector3.forward * size, color, duration);
    }
}