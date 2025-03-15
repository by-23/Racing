using System;
using UnityEngine;
using PathCreation;
using System.Collections.Generic;

// Расширенная структура для хранения информации о повороте.
[Serializable]
public struct TurnInfo
{
    public float distance; // Расстояние вдоль пути, где начинается поворот.
    public float angle; // Угол поворота (в градусах).
    public Vector3 position; // Вычисленная позиция поворота на пути.
}

public class PathHolder : Singleton<PathHolder>
{
    [Header("Path Following")] [SerializeField]
    internal PathCreator pathCreator;

    [Header("Turn Caching Settings")] [SerializeField, Tooltip("Шаг сканирования пути для поиска поворотов")]
    private float turnScanResolution = 0.1f;

    [SerializeField, Tooltip("Минимальный угол, чтобы считать участок поворотом (в градусах)")]
    private float minTurnAngle = 15f;

    // Кэш обнаруженных поворотов.
    internal List<TurnInfo> cachedTurns = new List<TurnInfo>();

    EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;

    /// <summary>
    /// Общая длина пути, полученная из PathCreator.
    /// </summary>
    public float TotalPathLength
    {
        get
        {
            if (pathCreator == null) return 0f;
            return pathCreator.path.length;
        }
    }

    // При инициализации кэшируем повороты.
    protected override void Awake()
    {
        CacheTurns();
    }

    /// <summary>
    /// Метод для вычисления точки на пути по заданной дистанции с использованием вершин пути.
    /// Не использует pathCreator.path.GetPointAtDistance.
    /// </summary>
    private Vector3 GetPointFromVertexPath(float distance)
    {
        if (pathCreator == null) return Vector3.zero;
        VertexPath vertexPath = pathCreator.path;
        int numPoints = vertexPath.NumPoints;
        if (numPoints < 2)
            return Vector3.zero;

        float accumulatedDistance = 0f;
        for (int i = 0; i < numPoints - 1; i++)
        {
            Vector3 A = vertexPath.GetPoint(i);
            Vector3 B = vertexPath.GetPoint(i + 1);
            float segmentLength = Vector3.Distance(A, B);
            if (accumulatedDistance + segmentLength >= distance)
            {
                float t = (distance - accumulatedDistance) / segmentLength;
                return Vector3.Lerp(A, B, t);
            }

            accumulatedDistance += segmentLength;
        }

        return vertexPath.GetPoint(numPoints - 1);
    }

    public void CacheTurns()
    {
        cachedTurns.Clear();
        float totalPathLength = TotalPathLength;
        if (totalPathLength <= 0f)
            return;

        float d = 0f;
        while (d < totalPathLength)
        {
            // Получаем касательную в текущей позиции d.
            Vector3 startTangent = GetTangentAtDistance(d);
            bool foundTurn = false;
            float turnAngle = 0f;
            float turnDistance = d;

            // Сканируем вперёд от позиции d с шагом turnScanResolution.
            for (float scan = d + turnScanResolution; scan <= totalPathLength; scan += turnScanResolution)
            {
                Vector3 scanTangent = GetTangentAtDistance(scan);
                float angleDiff = Vector3.Angle(startTangent, scanTangent);
                if (angleDiff >= minTurnAngle)
                {
                    foundTurn = true;
                    turnAngle = angleDiff;
                    turnDistance = scan;
                    break;
                }
            }

            if (foundTurn)
            {
                // Вычисляем позицию поворота с помощью вершин пути.
                Vector3 turnPosition = GetPointFromVertexPath(turnDistance);
                cachedTurns.Add(new TurnInfo { distance = turnDistance, angle = turnAngle, position = turnPosition });
                // Смещаем начало сканирования чуть дальше найденного поворота, чтобы избежать повторного обнаружения.
                d = turnDistance + turnScanResolution;
            }
            else
            {
                break;
            }
        }
    }


    /// <summary>
    /// Метод позволяет получить информацию о следующем повороте относительно текущей позиции (расстояния по пути).
    /// Если найден поворот, возвращает true и out-параметром выдает TurnInfo.
    /// </summary>
    public bool GetNextTurnInfo(float currentDistance, out TurnInfo nextTurn)
    {
        nextTurn = new TurnInfo();
        foreach (var turn in cachedTurns)
        {
            if (turn.distance > currentDistance)
            {
                nextTurn = turn;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Получает точку на пути по заданной дистанции.
    /// </summary>
    internal Vector3 GetPointAtDistance(float distance)
    {
        if (pathCreator == null) return Vector3.zero;
        return pathCreator.path.GetPointAtDistance(distance, endOfPathInstruction);
    }

    /// <summary>
    /// Возвращает нормализованный вектор касательной к пути в заданной дистанции.
    /// </summary>
    internal Vector3 GetTangentAtDistance(float distance)
    {
        const float delta = 0.1f;
        Vector3 pointA = GetPointAtDistance(distance);
        Vector3 pointB = GetPointAtDistance(distance + delta);
        return (pointB - pointA).normalized;
    }

    internal float FindClosestDistance(out int bestSegmentIndex, Vector3 position, int lastClosestIndex)
    {
        bestSegmentIndex = -1;
        if (pathCreator == null)
            return 0f;

        VertexPath vertexPath = pathCreator.path;
        int numPoints = vertexPath.NumPoints;
        if (numPoints < 2)
            return 0f;

        float bestDistanceAlongPath = 0f;
        float bestSqrDistance = float.MaxValue;
        float accumulatedDistance = 0f;

        // Перебираем все сегменты пути
        for (int i = 0; i < numPoints - 1; i++)
        {
            Vector3 A = vertexPath.GetPoint(i);
            Vector3 B = vertexPath.GetPoint(i + 1);
            Vector3 AB = B - A;
            float segmentLength = AB.magnitude;
            if (segmentLength < 0.0001f)
            {
                accumulatedDistance += segmentLength;
                continue;
            }

            // Вычисляем параметр проекции t вдоль сегмента
            float t = Mathf.Clamp01(Vector3.Dot(position - A, AB) / (segmentLength * segmentLength));
            Vector3 projection = A + t * AB;
            float sqrDist = (position - projection).sqrMagnitude;
            if (sqrDist < bestSqrDistance)
            {
                bestSqrDistance = sqrDist;
                bestDistanceAlongPath = accumulatedDistance + segmentLength * t;
                bestSegmentIndex = i;
            }

            accumulatedDistance += segmentLength;
        }

        return bestDistanceAlongPath;
    }
}