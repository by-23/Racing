using System.Collections.Generic;
using Game.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class BotController : MonoBehaviour
{
    public float rotationSpeed = 360f; // Скорость поворота (градусов в секунду)
    public float baseSpeed = 3.5f; // Базовая скорость бота
    public float slowdownDistance = 5f; // Расстояние для начала замедления
    public float lookaheadDistance = 3f; // Расстояние от текущей точки до создаваемой для вычисления угла
    public float reachedDistanceThreshold = 0.5f; // Расстояние, на котором цель считается достигнутой
    public float reachedVelocityThreshold = 0.5f; // Максимальная скорость для переключения цели
    private NavMeshAgent agent;
    private int currentTargetIndex = 0;
    private VehicleDriving vehicleDriving;
    private GameController _gameController;

    void Start()
    {
        _gameController = GameController.Instance;
        vehicleDriving = GetComponent<VehicleDriving>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.autoBraking = false;
        agent.stoppingDistance = 0;
        agent.speed = baseSpeed;

        if (_gameController.targets.Length > 0)
            agent.SetDestination(_gameController.targets[currentTargetIndex].position);
    }

    void Update()
    {
        if (_gameController.targets.Length == 0)
            return;

        if (agent.pathPending)
            return;

        Transform currentTarget = _gameController.targets[currentTargetIndex];
        Vector3 dirToCurrent = (currentTarget.position - transform.position).normalized;

        Vector3 lookaheadPoint = currentTarget.position;
        if (currentTargetIndex < _gameController.targets.Length - 1)
        {
            Vector3 dirCurrentToNext =
                (_gameController.targets[currentTargetIndex + 1].position - currentTarget.position).normalized;
            lookaheadPoint = currentTarget.position + dirCurrentToNext * lookaheadDistance;
        }

        // Вектор от бота к точке lookahead
        Vector3 dirToLookahead = (lookaheadPoint - transform.position).normalized;

        // Вычисляем угол поворота для колес
        float angle = Vector3.SignedAngle(transform.forward, dirToLookahead, Vector3.up);
        float maxTurnPerFrame = rotationSpeed * Time.deltaTime;
        float steeringInput = Mathf.Clamp(angle / maxTurnPerFrame, -1f, 1f);
        vehicleDriving.TurnWheels(steeringInput);

        // Вычисляем угол между вектором к текущей цели и вектором к lookahead точке
        float turnAngle = Vector3.Angle(dirToCurrent, dirToLookahead);

        // Фактор замедления по расстоянию до текущей цели
        float distanceToCurrent = Vector3.Distance(transform.position, currentTarget.position);
        float distanceFactor = Mathf.Clamp01(distanceToCurrent / slowdownDistance);

        float slowdownFactor = Mathf.Min(distanceFactor, turnAngle);

        // Рассчитываем целевую скорость
        float targetSpeed = baseSpeed * slowdownFactor;
        agent.speed = Mathf.Lerp(agent.speed, targetSpeed, Time.deltaTime * 1f);

        // Плавное вращение: поворачиваемся в направлении точки lookahead
        if (dirToLookahead != Vector3.zero)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(dirToLookahead);
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }

        // Переключение цели выполняется только если бот достаточно близко к текущей цели 
        // И его скорость ниже заданного порога, чтобы избежать резкого переключения
        if (distanceToCurrent < reachedDistanceThreshold && agent.velocity.magnitude < reachedVelocityThreshold)
        {
            if (currentTargetIndex < _gameController.targets.Length - 1)
            {
                currentTargetIndex++;
                agent.SetDestination(_gameController.targets[currentTargetIndex].position);
            }
            else
            {
                Debug.Log("Бот достиг последней цели!");
            }
        }
    }

    [Header("Gizmos Settings")] public float waypointSize = 0.5f;
    public Color waypointColor = Color.blue;
    public Color currentWaypointColor = Color.red;
    public Color pathColor = Color.yellow;
    public Color lookaheadColor = Color.cyan;
    public Color currentTargetLineColor = Color.magenta;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        _gameController = GameController.Instance;
        // Рисуем точки маршрута и линии между ними
        if (_gameController.targets.Length > 0)
        {
            for (int i = 0; i < _gameController.targets.Length; i++)
            {
                if (_gameController.targets[i] == null) continue;

                Gizmos.color = (i == currentTargetIndex) ? currentWaypointColor : waypointColor;
                Gizmos.DrawSphere(_gameController.targets[i].position, waypointSize);

                if (i < _gameController.targets.Length - 1 && _gameController.targets[i + 1] != null)
                {
                    Gizmos.color = pathColor;
                    // Gizmos.DrawLine(_gameController.targets[i].position, _gameController.targets[i + 1].position);
                }
            }
        }

        // Рисуем текущие векторы и информацию, если игра запущена
        if (Application.isPlaying && agent != null && _gameController.targets.Length > 0)
        {
            Transform currentTarget = _gameController.targets[currentTargetIndex];

            // Вектор к текущей цели
            Gizmos.color = currentTargetLineColor;
            // Gizmos.DrawLine(transform.position, currentTarget.position);

            // Вычисляем lookahead точку
            Vector3 lookaheadPoint = currentTarget.position;
            if (currentTargetIndex < _gameController.targets.Length - 1)
            {
                Vector3 dirCurrentToNext =
                    (_gameController.targets[currentTargetIndex + 1].position - currentTarget.position).normalized;
                lookaheadPoint = currentTarget.position + dirCurrentToNext * lookaheadDistance;
            }

            // Вектор к lookahead точке
            Gizmos.color = lookaheadColor;
            // Gizmos.DrawLine(transform.position, lookaheadPoint);
            Gizmos.DrawWireSphere(lookaheadPoint, 0.3f);

            // Рисуем угол поворота
            Vector3 dirToCurrent = (currentTarget.position - transform.position).normalized;
            Vector3 dirToLookahead = (lookaheadPoint - transform.position).normalized;
            float turnAngle = Vector3.Angle(dirToCurrent, dirToLookahead);

            Handles.color = Color.Lerp(Color.green, Color.red, turnAngle / 90f);
            Handles.DrawWireArc(transform.position, Vector3.up, dirToCurrent, turnAngle, 2f);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;

            Handles.Label(transform.position + Vector3.up * 2,
                $"Turn Angle: {turnAngle:F1}°\nSpeed Factor: {agent.speed / baseSpeed:F2}", style);
        }
    }
#endif
}