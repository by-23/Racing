using UnityEngine;
using PathCreation;
using System.Collections.Generic;

// Структура для хранения информации о повороте.
public struct TurnInfo
{
    public float distance; // Расстояние вдоль пути, где начинается поворот.
    public float angle; // Угол поворота (в градусах).
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
    private void Awake()
    {
        CacheTurns();
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
                cachedTurns.Add(new TurnInfo { distance = turnDistance, angle = turnAngle });
                // Смещаем начало сканирования чуть дальше найденного поворота, чтобы избежать повторного обнаружения
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

    /// <summary>
    /// Поиск ближайшей дистанции вдоль пути к заданной позиции.
    /// (Оставляем ваш оригинальный метод или адаптируем при необходимости)
    /// </summary>
    internal float FindClosestDistance(out int lastSavedCloseIndex, Vector3 position, int lastClosestIndex)
    {
        lastSavedCloseIndex = 0;
        if (pathCreator == null)
            return 0f;

        VertexPath vertexPath = pathCreator.path;
        int numPoints = vertexPath.NumPoints;
        if (numPoints == 0)
            return 0f;

        int bestIndex = Mathf.Clamp(lastClosestIndex, 0, numPoints - 1);
        float bestSqrDist = (vertexPath.GetPoint(bestIndex) - position).sqrMagnitude;

        while (bestIndex + 1 < numPoints)
        {
            float nextSqrDist = (vertexPath.GetPoint(bestIndex + 1) - position).sqrMagnitude;
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
            float prevSqrDist = (vertexPath.GetPoint(bestIndex - 1) - position).sqrMagnitude;
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

        float distanceAlongPath = 0f;
        for (int i = 1; i <= bestIndex; i++)
        {
            distanceAlongPath += Vector3.Distance(vertexPath.GetPoint(i - 1), vertexPath.GetPoint(i));
        }

        return distanceAlongPath;
    }
}