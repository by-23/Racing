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

    [Header("Reverse Settings")] public float stuckVelocityThreshold = 0.1f;
    public float stuckTimeThreshold = 2f;
    public float reverseDuration = 2f;
    public float reverseAccelerationFactor = 1f;
    public float carResetTime = 2f;
    public float detectionDistance = 5f;

    [Header("Detour Settings")] public LayerMask obstacleLayerMask;

    [FormerlySerializedAs("dístanceToAvoidPoint")] [FormerlySerializedAs("detourThreshold")]
    public float dístanceToReachAvoidPoint = 2f;

    public float pathBlockCheckDistance = 10f;
    public float obstacleRecheckInterval = 0.5f;
    public float sideOffsetMultiplier = 1.5f;
    public float detourDistanceMultiplier = 0.5f;
    public float minDetourDistance = 15f;
    public float maxDetourDistance = 30f;

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

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, pathBlockCheckDistance))
        {
            if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("Bot") ||
                hit.transform.CompareTag("Player"))
                obstacleHandler.HandleTriggerStay(hit.collider, transform);
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