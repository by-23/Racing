using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class BotAI : MonoBehaviour
{
    #region Settings

    [Header("Vehicle Settings")] [SerializeField]
    internal float steeringSensitivity = 1f;

    [SerializeField] internal float accelerationFactor = 1f;
    [SerializeField] internal float steeringDeadZone;
    [SerializeField] internal float turnSmoothing;


    [Header("Turn Braking")] public float brakingPower = 10f;
    [SerializeField] internal float turnBrakingDistance = 10f;

    [Header("Turn Speed Settings")] public float maxTurnSpeed = 60f;
    [SerializeField] internal float minTurnSpeed = 20f;
    [SerializeField] internal float maxTurnAngle = 90f;

    [Header("Reverse Settings")] [SerializeField]
    internal float stuckVelocityThreshold = 0.1f;

    [SerializeField] internal float stuckTimeThreshold = 2f;
    [SerializeField] internal float reverseDuration = 2f;
    [SerializeField] internal float reverseAccelerationFactor = 1f;
    [SerializeField] private float carResetTime = 2f;
    [SerializeField] internal float detectionDistance = 5f;

    [Header("Detour Settings")] [SerializeField]
    internal LayerMask obstacleLayerMask;

    [FormerlySerializedAs("dístanceToAvoidPoint")] [FormerlySerializedAs("detourThreshold")] [SerializeField]
    internal float dístanceToReachAvoidPoint = 2f;

    [SerializeField] internal float pathBlockCheckDistance = 10f;
    [SerializeField] internal float obstacleRecheckInterval = 0.5f;
    [SerializeField] internal float sideOffsetMultiplier = 1.5f;
    [SerializeField] internal float detourDistanceMultiplier = 0.5f;
    [SerializeField] internal float minDetourDistance = 15f;
    [SerializeField] internal float maxDetourDistance = 30f;

    #endregion

    private VehicleMovement vehicleMovement;
    private EzPath ezPath;

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
    private VehicleDriving vehicleDriving;


    private void Awake()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
        vehicleDriving = GetComponent<VehicleDriving>();
        ezPath = EzPath.Instance;
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
            if (hit.transform.CompareTag("Obstacle")
                // || hit.transform.CompareTag("Bot") || hit.transform.CompareTag("Player")
               )
                obstacleHandler.HandleTriggerStay(hit.collider, transform);
        }

        // Комбинируем корректировку руля из pathFollower и временную настройку (например, при обходе игрока)
        float totalSteering = pathFollower.SteeringPower + steeringAdjustment;
        
        vehicleDriving.TurnWheels(totalSteering / 75f);

        vehicleMovement.ApplySteering(totalSteering);

        steeringAdjustment = 0f;

        CheckResetOrientation();
    }

    private void HandleNormalState()
    {
        currentNormalTarget = ezPath.GetNextNearestPoint(transform).pointTransform.position;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Если target точка установлена, рисуем сферу (радиус можно настроить)
        Gizmos.DrawSphere(currentNormalTarget, 2f);
    }
}