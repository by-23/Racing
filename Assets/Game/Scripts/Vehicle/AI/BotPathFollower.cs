using UnityEngine;

public class BotPathFollower
{
    private BotAI bot;
    public float SteeringPower { get; private set; } = 0f;

    public BotPathFollower(BotAI bot)
    {
        this.bot = bot;
    }

    /// <summary>
    /// Основной метод управления движением бота с учетом объезда препятствий.
    /// Вычисляет необходимое рулевое воздействие и применяет торможение на поворотах.
    /// </summary>
    public void ProcessPathFollowing(Transform botTransform, Vector3 targetPoint, VehicleMovement vehicleMovement)
    {
        // Направление к целевой точке.
        Vector3 directionToTarget = (targetPoint - botTransform.position).normalized;

        // Вычисляем угол между направлением машины и направлением к целевой точке.
        float targetAngle = Vector3.SignedAngle(botTransform.forward, directionToTarget, Vector3.up);

        // Если угол больше порогового значения, вычисляем рулевое воздействие.
        float targetSteering = Mathf.Abs(targetAngle) > bot.steeringDeadZone
            ? targetAngle * bot.steeringSensitivity * vehicleMovement.steeringPower
            : 0f;

        // Применяем сглаживание поворота.
        SteeringPower = Mathf.Lerp(SteeringPower, targetSteering, bot.turnSmoothing * Time.deltaTime);

        // Если необходимо, тормозим на поворотах, иначе применяем ускорение.
        if (!ApplyTurnBraking(botTransform, vehicleMovement))
            vehicleMovement.ApplyAcceleration(bot.accelerationFactor);
    }

    /// <summary>
    /// Проверяет, требуется ли торможение на подходе к повороту, и при необходимости применяет тормоза.
    /// </summary>
    public bool ApplyTurnBraking(Transform botTransform, VehicleMovement vehicleMovement)
    {
        var path = EzPath.Instance;

        // Получаем позицию поворота.
        Vector3 turnPoint = path.GetNextNearestPoint(botTransform).pointTransform.position;
        float distanceToTurn = Vector3.Distance(botTransform.position, turnPoint);

        if (distanceToTurn <= bot.turnBrakingDistance)
        {
            // Вычисляем максимально допустимую скорость для поворота
            float allowedSpeed = GetMaxSpeedForTurn(path.GetNextNearestPoint(botTransform).angle);

            // Если реальная скорость выше допустимой — тормозим
            if (vehicleMovement._rb.linearVelocity.magnitude > allowedSpeed)
            {
                // distanceFactor от 0 до 1 показывает, как близко мы к повороту
                float distanceFactor = 1f - (distanceToTurn / bot.turnBrakingDistance);
                distanceFactor = Mathf.Clamp01(distanceFactor);

                // Усиливаем торможение в зависимости от distanceFactor
                float dynamicBrakingPower =
                    bot.brakingPower * distanceFactor * vehicleMovement._rb.linearVelocity.magnitude;

                vehicleMovement.ApplyBraking(dynamicBrakingPower);

                return true;
            }
        }

        return false;
    }


    private float GetMaxSpeedForTurn(float angle)
    {
        // Считаем отношение угла к максимальному
        float normalized = Mathf.Clamp01(angle / bot.maxTurnAngle);

        // Инвертируем его, чтобы при маленьком угле было ближе к 1, а при большом — к 0
        float inverted = 1f - normalized;

        // Теперь делаем Lerp так, чтобы при inverted=1 скорость была минимальная (сильное торможение),
        // а при inverted=0 скорость была максимальная.
        return Mathf.Lerp(bot.maxTurnSpeed, bot.minTurnSpeed, inverted);
    }


    public void SetSteering(float value)
    {
        SteeringPower = value;
    }
}