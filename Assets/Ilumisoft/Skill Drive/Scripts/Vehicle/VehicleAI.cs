using UnityEngine;
using PathCreation;

namespace Ilumisoft.SkillDrive
{
    public class VehicleAI : MonoBehaviour
    {
        [Header("Vehicle Settings")] [SerializeField]
        private Vehicle vehicle;

        [SerializeField] private float steeringSensitivity = 1f;
        [SerializeField] private float accelerationFactor = 1f;

        [Header("Path Following")] [SerializeField]
        private PathCreator pathCreator;

        [SerializeField] private float lookAheadDistance = 5f;

        [Header("Turn Braking")] [SerializeField]
        private float brakingPower = 10f;

        [SerializeField] private float minTurnAngle = 5f; // Повороты с углом меньше 5° не учитываются

        [Header("Turn Detection")] [SerializeField]
        private float turnScanDistance = 20f; // Расстояние сканирования поворотов

        [SerializeField] private float turnBrakingDistance = 10f; // Расстояние начала торможения

        [Header("Turn Speed Settings")] [SerializeField]
        private float maxTurnSpeed = 60f; // Максимальная скорость на небольших поворотах

        [SerializeField] private float minTurnSpeed = 20f; // Минимальная скорость на крутых поворотах
        [SerializeField] private float maxTurnAngle = 90f; // Угол, при котором скорость будет минимальной

        [Header("Obstacle Handling")] [SerializeField]
        private float obstacleDetectionDistance = 5f;

        [SerializeField, Range(0.1f, 1f)] private float obstacleSlowdown = 0.5f;
        [SerializeField] private float avoidanceStrength = 1f;
        [SerializeField] private int rayCount = 5;
        [SerializeField] private float detectionAngle = 90f;

        // Шаг при поиске поворота (единицы длины вдоль пути)
        [SerializeField] private float turnSearchStep = 1f;

        private Vector3 targetPosition;
        private Vector3 lastAvoidanceDirection;
        private float currentSpeedMultiplier = 1f;

        private void Start()
        {
            vehicle = GetComponent<Vehicle>();
            if (pathCreator == null)
                pathCreator = FindObjectOfType<PathCreator>();
            if (pathCreator == null)
                Debug.LogError("PathCreator не найден в сцене!");
        }

        private void Update()
        {
            if (vehicle.CanMove && pathCreator != null)
            {
                // Анализируем предстоящий поворот и применяем торможение, если найден поворот с углом > minTurnAngle
                ApplyTurnBraking();

                // Если обнаружено препятствие, выбираем более сильное замедление
                if (DetectObstacle(out Vector3 avoidanceDirection))
                {
                    currentSpeedMultiplier = Mathf.Min(currentSpeedMultiplier, obstacleSlowdown);
                    AvoidObstacle(avoidanceDirection);
                    lastAvoidanceDirection = avoidanceDirection;
                }
                else
                {
                    lastAvoidanceDirection = Vector3.zero;
                    FollowBezierPath();
                }

                // Применяем ускорение с учётом множителя скорости
                vehicle.ApplyAcceleration(accelerationFactor * currentSpeedMultiplier);
            }
        }

        private void FixedUpdate()
        {
            if (vehicle.Rigidbody.velocity.magnitude < 0.5f && currentSpeedMultiplier <= 0)
            {
                vehicle.Rigidbody.velocity = Vector3.zero;
                vehicle.Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void ApplyTurnBraking()
        {
            if (TryGetUpcomingTurnInfo(out float upcomingTurnAngle, out float distanceToTurn))
            {
                // Если расстояние до поворота меньше или равно turnBrakingDistance, начинаем торможение
                if (distanceToTurn <= turnBrakingDistance)
                {
                    // Вычисляем минимальную скорость на основе угла поворота
                    float minSpeedForTurn = GetMinSpeedForTurn(upcomingTurnAngle);

                    // Если текущая скорость выше минимальной, применяем торможение
                    if (vehicle.Rigidbody.velocity.magnitude > minSpeedForTurn)
                    {
                        // Применяем торможение с заданной силой
                        vehicle.ApplyBraking(brakingPower);
                    }
                }
            }
            else
            {
                // Если поворот не найден, не применяем торможение
                vehicle.ApplyBraking(0f);
            }
        }

        /// <summary>
        /// Вычисляет минимальную скорость на основе угла поворота.
        /// </summary>
        private float GetMinSpeedForTurn(float turnAngle)
        {
            // Нормализуем угол поворота относительно maxTurnAngle
            float angleRatio = Mathf.Clamp01(turnAngle / maxTurnAngle);

            // Интерполируем между maxTurnSpeed и minTurnSpeed на основе угла
            return Mathf.Lerp(maxTurnSpeed, minTurnSpeed, angleRatio);
        }

        private bool TryGetUpcomingTurnInfo(out float turnAngle, out float distanceToTurn)
        {
            turnAngle = 0f;
            distanceToTurn = 0f;

            float currentDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
            float totalPathLength = pathCreator.path.length;

            // Определяем начальную и конечную точки отрезка для сканирования
            float startDistance = currentDistance;
            float endDistance = Mathf.Min(currentDistance + turnScanDistance, totalPathLength);

            // Получаем начальное направление
            Vector3 startTangent = pathCreator.path.GetDirectionAtDistance(startDistance, EndOfPathInstruction.Stop);

            // Получаем конечное направление
            Vector3 endTangent = pathCreator.path.GetDirectionAtDistance(endDistance, EndOfPathInstruction.Stop);

            // Вычисляем угол между начальным и конечным направлением
            turnAngle = Vector3.Angle(startTangent, endTangent);

            // Если угол больше minTurnAngle, это поворот
            if (turnAngle > minTurnAngle)
            {
                // Расстояние до поворота — это расстояние начала торможения
                distanceToTurn = turnBrakingDistance;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Следование по кривой Безье.
        /// Вычисляет целевую точку и немедленно поворачивает автомобиль в её направлении.
        /// </summary>
        private void FollowBezierPath()
        {
            float closestDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
            targetPosition = pathCreator.path.GetPointAtDistance(closestDistance + lookAheadDistance);

            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0;

            float targetAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            float steeringPower = targetAngle * steeringSensitivity * vehicle.FinalStats.SteeringPower;

            vehicle.Rigidbody.AddRelativeTorque(0f, steeringPower, 0f, ForceMode.Acceleration);
        }

        /// <summary>
        /// Выполняет обнаружение препятствий с помощью набора лучей.
        /// При обнаружении препятствия возвращает направление для обхода.
        /// </summary>
        private bool DetectObstacle(out Vector3 avoidanceDirection)
        {
            avoidanceDirection = Vector3.zero;
            float angleStep = detectionAngle / (rayCount - 1);
            float startAngle = -detectionAngle / 2;

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 rayDirection = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, obstacleDetectionDistance))
                {
                    if (hit.collider.CompareTag("Obstacle"))
                    {
                        avoidanceDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Производит обход препятствия, поворачивая автомобиль в направлении,
        /// перпендикулярном нормали столкновения.
        /// </summary>
        private void AvoidObstacle(Vector3 avoidanceDirection)
        {
            float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
            float steeringPower = angleToAvoidance * avoidanceStrength * vehicle.FinalStats.SteeringPower;
            vehicle.Rigidbody.AddRelativeTorque(0f, steeringPower, 0f, ForceMode.Acceleration);
        }

        private void OnDrawGizmos()
        {
            if (pathCreator != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);

                // Визуализация поиска предстоящего поворота
                float currentDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
                Gizmos.color = Color.yellow;
                for (float d = currentDistance; d <= currentDistance + turnBrakingDistance; d += turnSearchStep)
                {
                    Vector3 point = pathCreator.path.GetPointAtDistance(d);
                    Gizmos.DrawSphere(point, 0.3f);
                }
            }

            Gizmos.color = Color.red;
            float angleStep = detectionAngle / (rayCount - 1);
            float startAngle = -detectionAngle / 2;
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
                Gizmos.DrawLine(transform.position, transform.position + dir * obstacleDetectionDistance);
            }
        }
    }
}