using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
[Serializable]
public struct PathPoint
{
    public Transform pointTransform; // Ссылка на объект-точку
    public float angle; // Угол поворота в градусах
    public int index;
}

public class EzPath : Singleton<EzPath>
{
    [Tooltip("Расстояние для вычисления виртуальных точек при расчёте угла.")]
    public float offsetDistance = 10f;

    public PathPoint[] pathPoints;

    // Вызывается в редакторе при изменении значений в инспекторе
    private void OnValidate()
    {
        // Собираем все дочерние объекты текущего GameObject
        var childTransforms = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            childTransforms[i] = transform.GetChild(i);
        }

        // Сортируем дочерние объекты по числовому значению в имени (например, "Point 1", "Point 2" и т.д.)
        childTransforms = childTransforms
            .OrderBy(child => ExtractNumber(child.name))
            .ToArray();

        // Создаём (или пересоздаём) массив PathPoint нужной длины
        pathPoints = new PathPoint[childTransforms.Length];

        // Заполняем массив, вычисляем угол для каждой точки
        for (int i = 0; i < childTransforms.Length; i++)
        {
            pathPoints[i].pointTransform = childTransforms[i];
            pathPoints[i].angle = CalculateAngle(childTransforms, i);
            pathPoints[i].index = i;
        }
    }

    // Извлекает первое число из строки имени
    int ExtractNumber(string name)
    {
        var match = Regex.Match(name, @"\d+");
        if (match.Success)
            return int.Parse(match.Value);
        return 0;
    }

    // Вычисляем угол для точки i, используя виртуальные точки
    private float CalculateAngle(Transform[] sortedTransforms, int i)
    {
        // Если нет предыдущей или следующей точки, пусть угол будет 0
        if (i == 0 || i == sortedTransforms.Length - 1)
            return 0f;

        Vector3 currPos = sortedTransforms[i].position;
        Vector3 prevPos = sortedTransforms[i - 1].position;
        Vector3 nextPos = sortedTransforms[i + 1].position;

        // Вектор к предыдущей точке
        Vector3 dirPrev = (prevPos - currPos).normalized;
        // Вектор к следующей точке
        Vector3 dirNext = (nextPos - currPos).normalized;

        // «Виртуальные» точки на offsetDistance
        Vector3 pA = currPos + dirPrev * offsetDistance;
        Vector3 pB = currPos + dirNext * offsetDistance;

        // Угол между векторами (pA - currPos) и (pB - currPos)
        float angle = Vector3.Angle(pA - currPos, pB - currPos);
        return angle;
    }


    public PathPoint GetNextNearestPoint(Transform obj)
    {
        // Если точек мало, вернём последнюю или "пустую"
        if (pathPoints == null || pathPoints.Length < 2)
            return pathPoints.Length > 0 ? pathPoints[0] : default;

        var (closestSegmentIndex, bestT) = FindClosestSegment(obj.position);

        // Если сегмент не найден, вернём последнюю точку
        if (closestSegmentIndex == -1)
            return pathPoints[pathPoints.Length - 1];

        // Если бот находится на отрезке (не достиг его конца)
        if (bestT < 1f)
        {
            // Возвращаем конечную точку этого отрезка
            return pathPoints[closestSegmentIndex + 1];
        }
        else
        {
            // Если бот достиг или прошёл конечную точку отрезка,
            // даём ему следующую точку (если она есть)
            int nextIndex = closestSegmentIndex + 2;
            if (nextIndex < pathPoints.Length)
            {
                return pathPoints[nextIndex];
            }
            else
            {
                // Если следующей точки нет, возвращаем последнюю
                return pathPoints[pathPoints.Length - 1];
            }
        }
    }

    public PathPoint GetNearestPoint(Transform obj)
    {
        // Если точек нет, возвращаем "пустую" структуру
        if (pathPoints == null || pathPoints.Length == 0)
            return default;

        // Если точка всего одна, она и есть ближайшая
        if (pathPoints.Length == 1)
            return pathPoints[0];

        var (closestSegmentIndex, _) = FindClosestSegment(obj.position);

        // Если сегмент не найден, вернём последнюю точку
        if (closestSegmentIndex == -1)
            return pathPoints[pathPoints.Length - 1];

        // Определяем точки, образующие ближайший сегмент
        PathPoint pointA = pathPoints[closestSegmentIndex];
        PathPoint pointB = pathPoints[closestSegmentIndex + 1];

        // Вычисляем расстояние до каждой из точек
        float distToASqr = (pointA.pointTransform.position - obj.position).sqrMagnitude;
        float distToBSqr = (pointB.pointTransform.position - obj.position).sqrMagnitude;

        // Определяем ближайшую точку и её индекс в массиве pathPoints
        PathPoint nearestPoint;
        int nearestPointIndex;

        if (distToASqr < distToBSqr)
        {
            nearestPoint = pointA;
            nearestPointIndex = closestSegmentIndex;
        }
        else
        {
            nearestPoint = pointB;
            nearestPointIndex = closestSegmentIndex + 1;
        }
        
        // Теперь вычисляем поворот по направлению к следующей точке.
        // Если следующая точка существует...
        if (nearestPointIndex + 1 < pathPoints.Length)
        {
            PathPoint nextPoint = pathPoints[nearestPointIndex + 1];
            Vector3 directionToNext = (nextPoint.pointTransform.position - nearestPoint.pointTransform.position).normalized;

            // ...и если точки не совпадают (вектор направления не нулевой)
            if (directionToNext != Vector3.zero)
            {
                // Вычисляем угол поворота по оси Y
                float newAngle = Quaternion.LookRotation(directionToNext).eulerAngles.y;

                // Возвращаем новую структуру PathPoint с обновлённым углом
                return new PathPoint
                {
                    pointTransform = nearestPoint.pointTransform,
                    angle = newAngle,
                    index = nearestPointIndex
                };
            }
        }
        
        // Если следующей точки нет (это конец пути) или точки совпадают,
        // возвращаем ближайшую точку с её исходным, предрасчитанным углом.
        return nearestPoint;
    }

    private (int, float) FindClosestSegment(Vector3 position)
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return (-1, 0);

        float minDistToSegment = float.MaxValue;
        int closestSegmentIndex = -1;
        float bestT = 0f;

        // Перебираем все пары соседних PathPoint (i, i+1),
        // чтобы найти ближайший к объекту отрезок маршрута.
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 A = pathPoints[i].pointTransform.position;
            Vector3 B = pathPoints[i + 1].pointTransform.position;

            Vector3 AB = B - A;
            float abSqrMag = AB.sqrMagnitude;
            if (abSqrMag < Mathf.Epsilon)
                continue; // пропускаем вырожденные отрезки

            // Параметр t при проекции позиции объекта на отрезок [A, B].
            // Если t в [0..1], объект "между" A и B; 
            // если t < 0, он "перед" A; если t > 1, "за" B.
            float t = Mathf.Clamp01(Vector3.Dot(position - A, AB) / abSqrMag);

            // Ближайшая точка на отрезке к позиции объекта
            Vector3 pointOnSegment = A + AB * t;

            // Проверяем, насколько объект близко к этому отрезку
            float distSqr = (position - pointOnSegment).sqrMagnitude;
            if (distSqr < minDistToSegment)
            {
                minDistToSegment = distSqr;
                closestSegmentIndex = i;
                bestT = t;
            }
        }
        return (closestSegmentIndex, bestT);
    }



// Метод для переименования всех дочерних объектов по порядку.
    [ContextMenu("Rename Children in Order")]
    public void RenameChildrenInOrder()
    {
        // Обновляем массив точек (сортировка выполняется в OnValidate)
        OnValidate();

        for (int i = 0; i < pathPoints.Length; i++)
        {
            // Задаём новое имя в виде "Point N", где N – номер по порядку
            pathPoints[i].pointTransform.name = "Point " + (i + 1);
        }
    }

    private void OnDrawGizmos()
    {
        // Если массив не заполнен, можно попытаться обновить его (только в редакторе)
#if UNITY_EDITOR
        if (!Application.isPlaying)
            OnValidate();
#endif

        if (pathPoints == null || pathPoints.Length == 0)
            return;

        // Проходим по всем точкам, кроме крайних (для них угол = 0)
        for (int i = 1; i < pathPoints.Length - 1; i++)
        {
            if (pathPoints[i].pointTransform == null)
                continue;

            Vector3 currPos = pathPoints[i].pointTransform.position;
            Vector3 prevPos = pathPoints[i - 1].pointTransform.position;
            Vector3 nextPos = pathPoints[i + 1].pointTransform.position;

            // Вычисляем направления к соседним точкам
            Vector3 dirPrev = (prevPos - currPos).normalized;
            Vector3 dirNext = (nextPos - currPos).normalized;

            // Вычисляем виртуальные точки для отрисовки угла
            Vector3 pA = currPos + dirPrev * offsetDistance;
            Vector3 pB = currPos + dirNext * offsetDistance;

            // Рисуем линии от текущей точки к виртуальным точкам
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currPos, pA);
            Gizmos.DrawLine(currPos, pB);

            // Рисуем небольшие сферы в виртуальных точках для лучшей видимости
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pA, 0.1f);
            Gizmos.DrawSphere(pB, 0.1f);

            // Рисуем сферу в самой точке
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currPos, 0.15f);

#if UNITY_EDITOR
            // Выводим угол над точкой; смещаем немного вверх, чтобы надпись не накладывалась
            Vector3 labelOffset = Vector3.up * 0.25f;
            Handles.color = Color.yellow;
            Handles.Label(currPos + labelOffset, pathPoints[i].angle.ToString("F1") + "°");
#endif
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(EzPath))]
[CanEditMultipleObjects]
public class EzPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // Если выбрано более одного объекта, выводим предупреждение
        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Multi-object editing is not supported for Rename Children in Order.",
                MessageType.Warning);
            return;
        }

        EzPath ezPath = (EzPath)target;
        if (GUILayout.Button("Rename Children In Order"))
        {
            ezPath.RenameChildrenInOrder();
        }
    }
}
#endif