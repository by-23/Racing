using System.Collections.Generic;
using PathCreation;
using UnityEngine;

public class PathHolder : Singleton<PathHolder>
{
    // Кешированные точки и дистанции по пути
    internal List<Vector3> cachedPoints = new List<Vector3>();
    private List<float> cachedDistances = new List<float>();
    internal float totalPathLength = 0f;

    [Header("Path Following")] [SerializeField]
    internal PathCreator pathCreator;

    [SerializeField] internal float lookAheadDistance = 5f;
    [SerializeField] private float pathSampleStep = 0.5f; // Расстояние между точками

    protected void Start()
    {
        CachePathPoints();
    }

    private void CachePathPoints()
    {
        if (pathCreator == null)
            return;

        totalPathLength = pathCreator.path.length;
        cachedPoints.Clear();
        cachedDistances.Clear();

        // Используем for-цикл вместо while
        for (float distance = 0f; distance <= totalPathLength; distance += pathSampleStep)
        {
            cachedPoints.Add(pathCreator.path.GetPointAtDistance(distance, EndOfPathInstruction.Stop));
            cachedDistances.Add(distance);
        }

        // Если последняя точка не совпадает с концом пути – добавляем её
        if (cachedDistances.Count == 0 || cachedDistances[cachedDistances.Count - 1] < totalPathLength)
        {
            cachedPoints.Add(pathCreator.path.GetPointAtDistance(totalPathLength, EndOfPathInstruction.Stop));
            cachedDistances.Add(totalPathLength);
        }
    }

    /// <summary>
    /// Ищет ближайшую дистанцию вдоль пути к заданной позиции, используя кеш.
    /// </summary>
    internal float FindClosestDistance(out int lastSavedCloseIndex, Vector3 position, int lastClosestIndex)
    {
        if (cachedPoints.Count == 0)
        {
            lastSavedCloseIndex = 0;
            return 0f;
        }

        int bestIndex = lastClosestIndex;
        float bestSqrDist = (cachedPoints[bestIndex] - position).sqrMagnitude;

        // Движение вперёд по кешу
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

        // Движение назад по кешу
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

        lastSavedCloseIndex = bestIndex;
        return cachedDistances[bestIndex];
    }

    /// <summary>
    /// Возвращает точку на пути по заданной дистанции с интерполяцией между кешированными точками.
    /// </summary>
    internal Vector3 GetPointAtDistance(float distance)
    {
        if (distance <= 0f)
            return cachedPoints[0];
        if (distance >= totalPathLength)
            return cachedPoints[cachedPoints.Count - 1];

        int low = 0, high = cachedDistances.Count - 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (cachedDistances[mid] < distance)
                low = mid + 1;
            else
                high = mid - 1;
        }

        int index = low;
        if (index == 0)
            return cachedPoints[0];

        float d0 = cachedDistances[index - 1];
        float d1 = cachedDistances[index];
        float t = (distance - d0) / (d1 - d0);
        return Vector3.Lerp(cachedPoints[index - 1], cachedPoints[index], t);
    }

    /// <summary>
    /// Получает информацию о предстоящем повороте, используя кешированные данные.
    /// </summary>
    internal bool TryGetUpcomingTurnInfo(out float turnAngle, out float distanceToTurn,
        float turnScanDistance, float turnBrakingDistance, float minTurnAngle, Vector3 playerPos, int lastClosestIndex)
    {
        turnAngle = 0f;
        distanceToTurn = 0f;

        int dummyIndex;
        float currentDistance = FindClosestDistance(out dummyIndex, playerPos, lastClosestIndex);
        float endDistance = Mathf.Min(currentDistance + turnScanDistance, totalPathLength);

        const float delta = 0.1f;
        Vector3 pointA = GetPointAtDistance(currentDistance);
        Vector3 pointB = GetPointAtDistance(currentDistance + delta);
        Vector3 startTangent = (pointB - pointA).normalized;

        Vector3 pointC = GetPointAtDistance(endDistance - delta);
        Vector3 pointD = GetPointAtDistance(endDistance);
        Vector3 endTangent = (pointD - pointC).normalized;

        turnAngle = Vector3.Angle(startTangent, endTangent);
        if (turnAngle > minTurnAngle)
        {
            distanceToTurn = turnBrakingDistance;
            return true;
        }

        return false;
    }
}