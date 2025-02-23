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
        // Вектор объезда, вычисленный с использованием нескольких лучей
        Vector3 avoidance = CalculateAvoidance(botTransform);
        // Итоговое направление – сумма векторов движения к цели и объезда
        Vector3 desiredDirection = (directionToTarget + avoidance).normalized;

        // Вычисляем угол поворота от текущего направления к желаемому
        float targetAngle = Vector3.SignedAngle(botTransform.forward, desiredDirection, Vector3.up);
        SteeringPower = targetAngle * bot.steeringSensitivity * vehicleMovement.steeringPower;

        if (!ApplyTurnBraking(botTransform, vehicleMovement))
            vehicleMovement.ApplyAcceleration(bot.accelerationFactor);
    }

    // Метод для вычисления корректирующего вектора объезда с использованием нескольких лучей
    private Vector3 CalculateAvoidance(Transform botTransform)
    {
        Vector3 avoidance = Vector3.zero;
        // Начало лучей смещается немного вперед от центра бота
        Vector3 rayOrigin = botTransform.position + botTransform.forward * 1f;
        int raysCount = bot.detectionRaysCount > 0 ? bot.detectionRaysCount : 1;
        float spreadAngle = bot.detectionSpreadAngle;

        // Лучи равномерно распределяются от -spreadAngle/2 до +spreadAngle/2 относительно направления вперед
        for (int i = 0; i < raysCount; i++)
        {
            float angleOffset = 0f;
            if (raysCount > 1)
                angleOffset = -spreadAngle / 2f + i * (spreadAngle / (raysCount - 1));

            // Поворачиваем вектор направления на вычисленный угол относительно вертикальной оси
            Vector3 rayDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * botTransform.forward;
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, bot.DetectionDistance))
            {
                if (hit.collider.CompareTag("Bot") || hit.collider.CompareTag("Obstacle"))
                {
                    // Нормаль столкновения умножается на силу объезда
                    avoidance += hit.normal * bot.avoidanceStrength;
                }
            }

            // Для отладки можно визуализировать лучи:
            Debug.DrawRay(rayOrigin, rayDirection * bot.obstacleDetectionDistance, Color.red);
        }

        return avoidance;
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
                if (vehicleMovement.rb.linearVelocity.magnitude > allowedSpeed)
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