using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class BotPathFollower
{
    private BotAI bot;
    public float SteeringPower { get; private set; } = 0f;
    private int currentTurnIndex = 0;

    public BotPathFollower(BotAI bot)
    {
        this.bot = bot;
    }

    /// <summary>
    /// Возвращает целевую точку для движения бота.
    /// Если есть ещё кешированные повороты, выбирается позиция следующего поворота.
    /// При приближении к повороту индекс увеличивается.
    /// </summary>
    public Vector3 GetTargetPoint(Vector3 botPosition)
    {
        List<TurnInfo> turns = PathHolder.Instance.cachedTurns;
        if (currentTurnIndex < turns.Count)
        {
            // Используем кешированную позицию поворота
            Vector3 point = turns[currentTurnIndex].position;
            if (Vector3.Distance(botPosition, point) < bot.turnThresholdDistance)
                currentTurnIndex++;
            return point;
        }

        // Если поворотов больше нет, возвращаем позицию последнего поворота,
        // либо Vector3.zero, если кеш пуст.
        if (turns.Count > 0)
            return turns[turns.Count - 1].position;
        return Vector3.zero;
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
        List<TurnInfo> turns = PathHolder.Instance.cachedTurns;
        if (currentTurnIndex < turns.Count)
        {
            // Получаем позицию поворота из кеша.
            Vector3 turnPoint = turns[currentTurnIndex].position;
            float distanceToTurn = Vector3.Distance(botTransform.position, turnPoint);
            if (distanceToTurn <= bot.turnBrakingDistance)
            {
                float allowedSpeed = GetMaxSpeedForTurn(turns[currentTurnIndex].angle);
                if (vehicleMovement._rb.linearVelocity.magnitude > allowedSpeed)
                {
                    vehicleMovement.ApplyBraking(bot.brakingPower);
                    return true;
                }
            }
        }

        return false;
    }

    private float GetMaxSpeedForTurn(float angle)
    {
        return Mathf.Lerp(bot.maxTurnSpeed, bot.minTurnSpeed, Mathf.Clamp01(angle / bot.maxTurnAngle));
    }

    public void SetSteering(float value)
    {
        SteeringPower = value;
    }
}