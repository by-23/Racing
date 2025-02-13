using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    public class BotAI : MonoBehaviour
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

        [SerializeField] private float avoidanceStrength = 1f;
        [SerializeField] private int rayCount = 5;
        [SerializeField] private float detectionAngle = 60f;
        [SerializeField] private LayerMask obstacleLayerMask;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float carResetTime;

        [Header("Path Deviation")] [SerializeField]
        private float maxLateralDeviation = 2f; // максимальное боковое отклонение

        [SerializeField] private float deviationUpdateInterval = 2f; // интервал обновления отклонения

        private int lastClosestIndex = 0;

        // Кеширование маршрута
        private List<Vector3> cachedPoints = new List<Vector3>();
        private List<float> cachedDistances = new List<float>();
        private float totalPathLength = 0f;

        private Vector3 targetPosition;
        private Vector3 lastAvoidanceDirection;
        private float currentSpeedMultiplier = 1f;

        // Параметры для случайного отклонения от маршрута
        private float currentLateralDeviation = 0f;
        private float deviationTimer = 0f;

        private Coroutine resetCoroutine;
        private float steeringPowerValue;

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

            float distance = 0f;
            while (distance <= totalPathLength)
            {
                Vector3 point = pathCreator.path.GetPointAtDistance(distance, EndOfPathInstruction.Stop);
                cachedPoints.Add(point);
                cachedDistances.Add(distance);
                distance += pathSampleStep;
            }

            if (cachedDistances[cachedDistances.Count - 1] < totalPathLength)
            {
                cachedPoints.Add(pathCreator.path.GetPointAtDistance(totalPathLength, EndOfPathInstruction.Stop));
                cachedDistances.Add(totalPathLength);
            }
        }


        private void Update()
        {
            if (vehicle.CanMove && pathCreator != null && GameController.Instance.isGameStarted)
            {
                ApplyTurnBraking();

                if (DetectObstacle(out Vector3 avoidanceDirection))
                {
                    // При объезде препятствия не добавляем отклонение
                    AvoidObstacle(avoidanceDirection);
                    lastAvoidanceDirection = avoidanceDirection;
                }
                else
                {
                    lastAvoidanceDirection = Vector3.zero;
                    UpdateLateralDeviation();
                    FollowCachedPath();
                }

                vehicle.ApplyAcceleration(accelerationFactor * currentSpeedMultiplier);
            }
        }

        private void FixedUpdate()
        {
            vehicle.ApplySteering(steeringPowerValue);
            
            if (vehicle.rb.velocity.magnitude < 0.5f && currentSpeedMultiplier <= 0)
            {
                vehicle.rb.velocity = Vector3.zero;
                vehicle.rb.angularVelocity = Vector3.zero;
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Explosive explosive) && resetCoroutine == null)
                resetCoroutine = StartCoroutine(ResetCarPosition());
        }

        private IEnumerator ResetCarPosition()
        {
            yield return new WaitForSeconds(carResetTime);
            ;

            if (!Physics.Raycast(transform.position + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2f,
                    groundLayer) || hit.collider.tag != "Ground")
            {
                vehicle.ResetCarPosition();
            }

            resetCoroutine = null;
        }


        /// <summary>
        /// Обновляет значение бокового отклонения через заданный интервал времени.
        /// </summary>
        private void UpdateLateralDeviation()
        {
            deviationTimer += Time.deltaTime;
            if (deviationTimer >= deviationUpdateInterval)
            {
                // Новое случайное отклонение в диапазоне [-maxLateralDeviation, maxLateralDeviation]
                currentLateralDeviation = Random.Range(-maxLateralDeviation, maxLateralDeviation);
                deviationTimer = 0f;
            }
        }

        /// <summary>
        /// Следование по маршруту с добавлением небольшого бокового смещения.
        /// Находим ближайшую точку к текущей позиции и вычисляем целевую точку по lookAheadDistance.
        /// Затем вычисляем вектор тангенса и смещаем целевую точку вбок.
        /// </summary>
        private void FollowCachedPath()
        {
            float currentDistance = FindClosestDistance(transform.position);
            float targetDistance = Mathf.Min(currentDistance + lookAheadDistance, totalPathLength);
            targetPosition = GetPointAtDistance(targetDistance);

            // Вычисляем тангенс маршрута в точке targetPosition (используем небольшую дельту для интерполяции)
            Vector3 forwardOnPath =
                (GetPointAtDistance(Mathf.Min(targetDistance + 0.1f, totalPathLength)) - targetPosition).normalized;
            // Вычисляем вектор, перпендикулярный направлению движения (с учетом оси Y)
            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardOnPath).normalized;

            // Добавляем к целевой точке смещение по боковой оси
            targetPosition += lateralDirection * currentLateralDeviation;

            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0;

            float targetAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            steeringPowerValue = targetAngle * steeringSensitivity * vehicle.steeringPower;
        }

        /// <summary>
        /// Ищет ближайшую дистанцию вдоль пути к заданной позиции, используя кешированные точки.
        /// Предполагается, что автомобиль движется вперёд по маршруту, поэтому можно начинать поиск с последнего найденного индекса.
        /// </summary>
        private float FindClosestDistance(Vector3 position)
        {
            if (cachedPoints.Count == 0)
                return 0f;

            int bestIndex = lastClosestIndex;
            float bestSqrDist = (cachedPoints[bestIndex] - position).sqrMagnitude;

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
            if (distance <= 0f)
                return cachedPoints[0];
            if (distance >= totalPathLength)
                return cachedPoints[cachedPoints.Count - 1];

            int index = cachedDistances.FindIndex(d => d >= distance);
            if (index == -1)
                return cachedPoints[cachedPoints.Count - 1];

            if (index == 0)
                return cachedPoints[0];

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
                    if (vehicle.rb.velocity.magnitude > minSpeedForTurn)
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
        /// </summary>
        private bool TryGetUpcomingTurnInfo(out float turnAngle, out float distanceToTurn)
        {
            turnAngle = 0f;
            distanceToTurn = 0f;

            float currentDistance = FindClosestDistance(transform.position);
            float endDistance = Mathf.Min(currentDistance + turnScanDistance, totalPathLength);

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

        /// <summary>
        /// Обнаруживает препятствия с тегом "Obstacle".
        /// </summary>
        private bool DetectObstacle(out Vector3 avoidanceDirection)
        {
            avoidanceDirection = Vector3.zero;
            Vector3 rayOrigin = transform.position + transform.forward * 1.0f;

            float angleStep = detectionAngle / (rayCount - 1);
            float startAngle = -detectionAngle / 2;

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 rayDirection = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, obstacleDetectionDistance,
                        obstacleLayerMask))
                {
                    if (hit.collider.CompareTag("Obstacle"))
                    {
                        Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                        avoidanceDirection = (localHitPoint.x < 0) ? transform.right : -transform.right;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Поворачивает транспортное средство для обхода обнаруженного препятствия.
        /// </summary>
        private void AvoidObstacle(Vector3 avoidanceDirection)
        {
            float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
            float steeringPowerValue = angleToAvoidance * avoidanceStrength * vehicle.steeringPower;
            vehicle.rb.AddRelativeTorque(0f, steeringPowerValue, 0f, ForceMode.Acceleration);
        }

        private void OnDrawGizmos()
        {
            if (pathCreator != null && cachedPoints.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);
            }

            Gizmos.color = Color.red;
            float angleStep = detectionAngle / (rayCount - 1);
            float startAngle = -detectionAngle / 2;
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0, startAngle + angleStep * i, 0) * transform.forward;
                Gizmos.DrawLine(transform.position, transform.position + dir * obstacleDetectionDistance);
            }

            Vector3 rayStart = transform.position + Vector3.up * 1.0f; // Поднятая начальная точка
            Vector3 rayEnd = rayStart + Vector3.down * 2f; // Конечная точка луча

            Gizmos.color = Color.white; // Цвет для визуализации
            Gizmos.DrawLine(rayStart, rayEnd); // Рисуем линию
        }
    }
}