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

    /// <summary>
    /// Вызывается при нахождении бота в тригере препятствия.
    /// Если бот ещё не в режиме объезда, пытается подобрать доступную точку объезда.
    /// </summary>
    public void HandleTriggerStay(Collider other, Transform botTransform)
    {
        // Если игра не запущена или бот уже в режиме объезда – не ищем новую точку
        if (!(GameController.Instance.isGameStarted && bot.currentState != BotAI.BotState.Detouring))
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

    /// <summary>
    /// Пытается подобрать точку объезда. Подбор идёт до тех пор, пока не найдётся доступный вариант
    /// или не исчерпается максимальное число попыток. При успешном подборе бот переводится в режим Detouring,
    /// и новая точка не подбирается, пока бот не достигнет её или не сменит своё состояние.
    /// </summary>
    private void UpdateDetourPoint(Collider obstacle, Transform botTransform)
    {
        // Если бот уже в режиме объезда, не подбираем новую точку
        if (bot.currentState == BotAI.BotState.Detouring)
            return;

        // Позиция и размер препятствия
        Vector3 obstaclePos = obstacle.bounds.center;
        Vector3 toObstacle = obstaclePos - botTransform.position;

        // Получаем текущую скорость бота
        VehicleMovement vm = botTransform.GetComponent<VehicleMovement>();
        float currentSpeed = vm != null ? vm.rb.linearVelocity.magnitude : 0f;

        // Определяем сторону объезда: 1 – вправо, -1 – влево
        float sideChoice = CalculateSideChoice(toObstacle, botTransform);

        // Определяем латеральное смещение, основываясь на размере препятствия и заданном запасе
        float projectedWidth = Mathf.Abs(Vector3.Dot(obstacle.bounds.size, botTransform.right.normalized));
        float lateralOffset = (projectedWidth / 2f) + bot.sideOffsetMultiplier;

        // Фронтальное смещение зависит от скорости
        float baseForwardDistance = Mathf.Clamp(currentSpeed * bot.detourDistanceMultiplier,
            bot.minDetourDistance, bot.maxDetourDistance);

        int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Плавно увеличиваем дистанцию на каждой попытке
            float adjustedForwardDistance = baseForwardDistance + i * 1.0f; // прибавляем 1 единицу за попытку

            Vector3 candidateDetourPoint = botTransform.position +
                                             botTransform.forward * adjustedForwardDistance +
                                             botTransform.right * (sideChoice * lateralOffset);
            candidateDetourPoint.y = botTransform.position.y; // сохраняем уровень по Y

            // Проверяем, что точка находится перед ботом
            Vector3 toCandidate = candidateDetourPoint - botTransform.position;
            if (Vector3.Dot(botTransform.forward, toCandidate) <= 0)
            {
                // Пробуем инвертировать латеральный сдвиг
                candidateDetourPoint = botTransform.position +
                                       botTransform.forward * adjustedForwardDistance +
                                       botTransform.right * (-sideChoice * lateralOffset);
                candidateDetourPoint.y = botTransform.position.y;
                toCandidate = candidateDetourPoint - botTransform.position;
                if (Vector3.Dot(botTransform.forward, toCandidate) <= 0)
                {
                    // Если точка всё ещё позади, переходим к следующей попытке с увеличенной дистанцией
                    continue;
                }
            }

            Debug.DrawLine(botTransform.position, candidateDetourPoint, Color.green, 2.0f);
            DrawDetourPointMarker(candidateDetourPoint, Color.green, 2.0f);

            // Проверяем, свободен ли путь к точке объезда
            bool clearToDetour = !CheckForObstaclesBetweenPoints(botTransform.position, candidateDetourPoint);
            if (clearToDetour && !Physics.CheckSphere(candidateDetourPoint, 2f, bot.obstacleLayerMask))
            {
                // Успешно подобрана точка – сохраняем и переключаем состояние
                detourPoint = candidateDetourPoint;
                bot.currentState = BotAI.BotState.Detouring;
                isObstacleStillPresent = true;
                return; // выходим из метода, дальнейшие попытки не выполняются
            }
        }
        // Если ни одна из попыток не увенчалась успехом, detourPoint остаётся неизменной,
        // и в следующих обновлениях поиск будет повторяться.
    }

    private float CalculateSideChoice(Vector3 toObstacle, Transform botTransform)
    {
        float dot = Vector3.Dot(toObstacle, botTransform.right);
        return dot > 0 ? -1f : 1f;
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
            bot.SetState(BotAI.BotState.Normal);
            isObstacleStillPresent = false;
            return;
        }

        Debug.DrawLine(botTransform.position, normalTarget, Color.blue, 0.5f);

        // Если бот уже достиг (или близок к) точки объезда, переключаемся на нормальный режим
        if (Vector3.Distance(botTransform.position, detourPoint) < bot.dístanceToReachAvoidPoint)
        {
            bot.SetState(BotAI.BotState.Normal);
            return;
        }

        Vector3 toDetour = detourPoint - botTransform.position;
        ProcessForwardDetour(toDetour, botTransform);
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
