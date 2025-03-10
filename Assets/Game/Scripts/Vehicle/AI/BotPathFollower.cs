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

    public Vector3 GetTargetPoint(Vector3 botPosition)
    {
        List<TurnInfo> turns = PathHolder.Instance.cachedTurns;
        if (currentTurnIndex < turns.Count)
        {
            Vector3 point = PathHolder.Instance.GetPointAtDistance(turns[currentTurnIndex].distance);
            if (Vector3.Distance(botPosition, point) < bot.turnThresholdDistance)
                currentTurnIndex++;
            return point;
        }

        return PathHolder.Instance.GetPointAtDistance(PathHolder.Instance.TotalPathLength);
    }

    // Основной метод управления движением с учетом объезда препятствий
    public void ProcessPathFollowing(Transform botTransform, Vector3 targetPoint, VehicleMovement vehicleMovement)
    {
        // Направление к целевой точке
        Vector3 directionToTarget = (targetPoint - botTransform.position).normalized;

        // Вычисляем угол поворота от текущего направления к желаемому
        float targetAngle = Vector3.SignedAngle(botTransform.forward, directionToTarget, Vector3.up);
        SteeringPower = targetAngle * bot.steeringSensitivity * vehicleMovement.steeringPower;
        // Debug.Log(directionToTarget);
        if (!ApplyTurnBraking(botTransform, vehicleMovement))
            vehicleMovement.ApplyAcceleration(bot.accelerationFactor);
    }

    public bool ApplyTurnBraking(Transform botTransform, VehicleMovement vehicleMovement)
    {
        List<TurnInfo> turns = PathHolder.Instance.cachedTurns;
        if (currentTurnIndex < turns.Count)
        {
            Vector3 turnPoint = PathHolder.Instance.GetPointAtDistance(turns[currentTurnIndex].distance);
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