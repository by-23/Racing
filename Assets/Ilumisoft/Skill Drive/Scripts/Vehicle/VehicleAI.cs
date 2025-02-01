using System.Collections.Generic;
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
        [SerializeField] private float pathSampleStep = 0.5f; // расстояние между предвычисленными точками

        [Header("Turn Braking")] [SerializeField]
        private float brakingPower = 10f;

        [SerializeField] private float minTurnAngle = 5f;

        [Header("Turn Detection")] [SerializeField]
        private float turnScanDistance = 20f;

        [SerializeField] private float turnBrakingDistance = 10f;

        [Header("Turn Speed Settings")] [SerializeField]
        private float maxTurnSpeed = 60f;

        [SerializeField] private float minTurnSpeed = 20f;
        [SerializeField] private float maxTurnAngle = 90f;

        [Header("Obstacle Handling")] [SerializeField]
        private float obstacleDetectionDistance = 5f;

        [SerializeField, Range(0.1f, 1f)] private float obstacleSlowdown = 0.5f;
        [SerializeField] private float avoidanceStrength = 1f;
        [SerializeField] private int rayCount = 5;
        [SerializeField] private float detectionAngle = 90f;

        [SerializeField] private float turnSearchStep = 1f;
        private int lastClosestIndex = 0;


        // Новые поля для кеширования маршрута
        private List<Vector3> cachedPoints = new List<Vector3>();
        private List<float> cachedDistances = new List<float>();
        private float totalPathLength = 0f;

        private Vector3 targetPosition;
        private Vector3 lastAvoidanceDirection;
        private float currentSpeedMultiplier = 1f;

        private void Start()
        {
            vehicle = GetComponent<Vehicle>();

            if (pathCreator == null)
                pathCreator = FindObjectOfType<PathCreator>();

            if (pathCreator == null)
            {
                Debug.LogError("PathCreator не найден в сцене!");
                return;
            }

            // Предварительное кеширование точек пути
            CachePathPoints();
        }

        /// <summary>
        /// Разбивает путь на точки с шагом pathSampleStep и вычисляет накопленные расстояния.
        /// </summary>
        private void CachePathPoints()
        {
            totalPathLength = pathCreator.path.length;
            cachedPoints.Clear();
            cachedDistances.Clear();

            // Начинаем с дистанции 0 и двигаемся до конца пути
            float distance = 0f;
            while (distance <= totalPathLength)
            {
                Vector3 point = pathCreator.path.GetPointAtDistance(distance, EndOfPathInstruction.Stop);
                cachedPoints.Add(point);
                cachedDistances.Add(distance);
                distance += pathSampleStep;
            }

            // Если последняя точка не совпадает с концом пути, добавляем её
            if (cachedDistances[cachedDistances.Count - 1] < totalPathLength)
            {
                cachedPoints.Add(pathCreator.path.GetPointAtDistance(totalPathLength, EndOfPathInstruction.Stop));
                cachedDistances.Add(totalPathLength);
            }
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
                    FollowCachedPath();
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

        /// <summary>
        /// Следование по маршруту, используя кешированные точки.
        /// Находим ближайшую точку к текущей позиции и вычисляем целевую точку по lookAheadDistance.
        /// </summary>
        private void FollowCachedPath()
        {
            float currentDistance = FindClosestDistance(transform.position);
            float targetDistance = Mathf.Min(currentDistance + lookAheadDistance, totalPathLength);
            targetPosition = GetPointAtDistance(targetDistance);

            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0;

            float targetAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            float steeringPowerValue = targetAngle * steeringSensitivity * vehicle.FinalStats.SteeringPower;

            vehicle.Rigidbody.AddRelativeTorque(0f, steeringPowerValue, 0f, ForceMode.Acceleration);
        }

        /// <summary>
        /// Ищет ближайшую дистанцию вдоль пути к заданной позиции, используя кешированные точки.
        /// Предполагается, что автомобиль движется вперёд по маршруту, поэтому можно начинать поиск с последнего найденного индекса.
        /// </summary>
        // Добавляем поле для хранения последнего найденного индекса
        private float FindClosestDistance(Vector3 position)
        {
            // Если кеш пустой, вернуть 0
            if (cachedPoints.Count == 0)
                return 0f;

            int bestIndex = lastClosestIndex;
            float bestSqrDist = (cachedPoints[bestIndex] - position).sqrMagnitude;

            // Поиск вперёд: двигаемся к увеличению индекса, пока расстояние уменьшается
            while (bestIndex + 1 < cachedPoints.Count)
            {
                float nextSqrDist = (cachedPoints[bestIndex + 1] - position).sqrMagnitude;
                if (nextSqrDist < bestSqrDist)
                {
                    bestSqrDist = nextSqrDist;
                    bestIndex++;
                }
                else
                {
                    break;
                }
            }

            // Поиск назад: на случай, если автомобиль немного отстал
            while (bestIndex - 1 >= 0)
            {
                float prevSqrDist = (cachedPoints[bestIndex - 1] - position).sqrMagnitude;
                if (prevSqrDist < bestSqrDist)
                {
                    bestSqrDist = prevSqrDist;
                    bestIndex--;
                }
                else
                {
                    break;
                }
            }

            lastClosestIndex = bestIndex;
            return cachedDistances[bestIndex];
        }


        /// <summary>
        /// Возвращает точку на маршруте по заданной дистанции, используя интерполяцию между кешированными точками.
        /// </summary>
        private Vector3 GetPointAtDistance(float distance)
        {
            // Если дистанция вне диапазона, возвращаем крайние точки
            if (distance <= 0f)
                return cachedPoints[0];
            if (distance >= totalPathLength)
                return cachedPoints[cachedPoints.Count - 1];

            // Находим индекс ближайшей точки, у которой дистанция больше или равна заданной
            int index = cachedDistances.FindIndex(d => d >= distance);
            if (index == -1)
                return cachedPoints[cachedPoints.Count - 1];

            // Если это первая точка, возвращаем её
            if (index == 0)
                return cachedPoints[0];

            // Интерполируем между предыдущей и найденной точками
            float d0 = cachedDistances[index - 1];
            float d1 = cachedDistances[index];
            float t = (distance - d0) / (d1 - d0);
            return Vector3.Lerp(cachedPoints[index - 1], cachedPoints[index], t);
        }

        private void ApplyTurnBraking()
        {
            if (TryGetUpcomingTurnInfo(out float upcomingTurnAngle, out float distanceToTurn))
            {
                if (distanceToTurn <= turnBrakingDistance)
                {
                    float minSpeedForTurn = GetMinSpeedForTurn(upcomingTurnAngle);
                    if (vehicle.Rigidbody.velocity.magnitude > minSpeedForTurn)
                    {
                        vehicle.ApplyBraking(brakingPower);
                    }
                }
            }
            else
            {
                vehicle.ApplyBraking(0f);
            }
        }

        private float GetMinSpeedForTurn(float turnAngle)
        {
            float angleRatio = Mathf.Clamp01(turnAngle / maxTurnAngle);
            return Mathf.Lerp(maxTurnSpeed, minTurnSpeed, angleRatio);
        }

        /// <summary>
        /// Получает информацию о предстоящем повороте, используя кешированные данные.
        /// Для упрощения можно брать направления в начале и в конце сканируемого участка.
        /// </summary>
        private bool TryGetUpcomingTurnInfo(out float turnAngle, out float distanceToTurn)
        {
            turnAngle = 0f;
            distanceToTurn = 0f;

            float currentDistance = FindClosestDistance(transform.position);
            float endDistance = Mathf.Min(currentDistance + turnScanDistance, totalPathLength);

            // Получаем направления, используя наши кешированные точки (с интерполяцией)
            Vector3 startTangent = (GetPointAtDistance(currentDistance + 0.1f) - GetPointAtDistance(currentDistance))
                .normalized;
            Vector3 endTangent = (GetPointAtDistance(endDistance) - GetPointAtDistance(endDistance - 0.1f)).normalized;

            turnAngle = Vector3.Angle(startTangent, endTangent);
            if (turnAngle > minTurnAngle)
            {
                distanceToTurn = turnBrakingDistance;
                return true;
            }

            return false;
        }

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

        private void AvoidObstacle(Vector3 avoidanceDirection)
        {
            float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
            float steeringPowerValue = angleToAvoidance * avoidanceStrength * vehicle.FinalStats.SteeringPower;
            vehicle.Rigidbody.AddRelativeTorque(0f, steeringPowerValue, 0f, ForceMode.Acceleration);
        }

        private void OnDrawGizmos()
        {
            if (pathCreator != null && cachedPoints.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);

                // Визуализация участка для поиска поворота
                float currentDistance = FindClosestDistance(transform.position);
                Gizmos.color = Color.yellow;
                for (float d = currentDistance; d <= currentDistance + turnBrakingDistance; d += turnSearchStep)
                {
                    Vector3 point = GetPointAtDistance(d);
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