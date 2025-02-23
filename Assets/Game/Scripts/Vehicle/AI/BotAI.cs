using System.Collections;
using UnityEngine;
using PathCreation;
using UnityEngine.Serialization;

public class BotAI : MonoBehaviour
{
    #region Settings

    [Header("Vehicle Settings")] public float steeringSensitivity = 1f;
    public float accelerationFactor = 1f;

    [Header("Turn Braking")] public float brakingPower = 10f;
    public float turnBrakingDistance = 10f;

    [Header("Turn Speed Settings")] public float maxTurnSpeed = 60f;
    public float minTurnSpeed = 20f;
    public float maxTurnAngle = 90f;
    public float turnThresholdDistance = 20f;

    [Header("Obstacle Handling")] public float obstacleDetectionDistance = 5f;
    public float avoidanceStrength = 1f;
    public LayerMask obstacleLayerMask;
    public int detectionRaysCount = 5;
    public float detectionSpreadAngle = 30f;

    [Header("Reverse Settings")] public float stuckVelocityThreshold = 0.1f;
    public float stuckTimeThreshold = 2f;
    public float reverseDuration = 2f;
    public float reverseAccelerationFactor = 1f;
    public float carResetTime = 2f;

    [Header("Detour Settings")] public float detourThreshold = 2f;
    public float detourCooldown = 3f;
    public float reverseSteeringMultiplier = 0.7f;
    public float reverseAngleThreshold = 100f;
    public float DetectionDistance = 5f;
    public float pathBlockCheckDistance = 10f;
    public float obstacleRecheckInterval = 0.5f;
    public float sideOffsetMultiplier = 1.5f;

    #endregion

    private VehicleMovement vehicleMovement;

    // Вспомогательные классы
    private BotPathFollower pathFollower;
    private BotObstacleHandler obstacleHandler;
    private BotReverseHandler reverseHandler;

    // Дополнительные переменные состояния
    public enum BotState
    {
        Normal,
        Reversing,
        Detouring
    }

    public BotState currentState = BotState.Normal;
    [HideInInspector] public Vector3 currentNormalTarget;
    private float steeringAdjustment = 0f;
    internal Coroutine resetCoroutine = null;


    private void Awake()
    {
        vehicleMovement = GetComponent<VehicleMovement>();

        // Инициализируем вспомогательные классы, передавая настройки (this)
        pathFollower = new BotPathFollower(this);
        obstacleHandler = new BotObstacleHandler(this);
        reverseHandler = new BotReverseHandler(this);
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case BotState.Normal:
                HandleNormalState();
                break;
            case BotState.Detouring:
                HandleDetourState();
                break;
            case BotState.Reversing:
                HandleReverseState();
                break;
        }

        // Комбинируем корректировку руля из pathFollower и временную настройку (например, при обходе игрока)
        float totalSteering = pathFollower.SteeringPower + steeringAdjustment;
        vehicleMovement.ApplySteering(totalSteering);
        steeringAdjustment = 0f;

        CheckResetOrientation();
    }

    private void HandleNormalState()
    {
        currentNormalTarget = pathFollower.GetTargetPoint(transform.position);
        pathFollower.ProcessPathFollowing(transform, currentNormalTarget, vehicleMovement);
        reverseHandler.CheckStuck(vehicleMovement, transform);
    }

    private void HandleDetourState()
    {
        obstacleHandler.UpdateDetourState(transform, currentNormalTarget);
        reverseHandler.CheckStuck(vehicleMovement, transform);
    }

    private void HandleReverseState()
    {
        reverseHandler.ProcessReverse(vehicleMovement);
    }

    private void OnTriggerStay(Collider other)
    {
        // Передаём обработку столкновений классу-обработчику препятствий
        obstacleHandler.HandleTriggerStay(other, transform);
    }

    private void CheckResetOrientation()
    {
        Vector3 angles = transform.rotation.eulerAngles;
        if ((angles.x > 75f && angles.x < 285f) ||
            (angles.z > 75f && angles.z < 285f))
        {
            if (resetCoroutine == null)
                resetCoroutine = StartCoroutine(ResetCarPosition());
        }
    }

    private IEnumerator ResetCarPosition()
    {
        yield return new WaitForSeconds(carResetTime);
        if (!vehicleMovement.groundDetection.IsGrounded)
        {
            vehicleMovement.ResetCarPosition();
        }

        resetCoroutine = null;
    }

    // Эти методы позволяют вспомогательным классам корректировать рулевое управление
    public void AddSteeringAdjustment(float adjustment)
    {
        steeringAdjustment += adjustment;
    }

    public bool HasTurnBraking(Transform botTransform)
    {
        return pathFollower.ApplyTurnBraking(botTransform, vehicleMovement);
    }

    public void SetSteering(float value)
    {
        pathFollower.SetSteering(value);
    }

    // Позволяет получить доступ к обработчику препятствий (например, для получения detourPoint)
    public BotObstacleHandler ObstacleHandler
    {
        get { return obstacleHandler; }
    }

    // Метод для смены состояния, вызывается из вспомогательных классов
    public void SetState(BotState newState)
    {
        currentState = newState;
    }
}