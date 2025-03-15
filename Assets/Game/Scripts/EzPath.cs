using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;


[System.Serializable]
public struct PathPoint
{
    public Transform pointTransform; // Ссылка на объект-точку
    public float angle; // Угол поворота в градусах
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
        // Если массив пустой, вернём "пустую" структуру (угол = 0, pointTransform = null).
        if (pathPoints == null || pathPoints.Length == 0)
            return default;

        Vector3 botPos = obj.position;

        float minDistToSegment = float.MaxValue;
        int closestSegmentIndex = -1;
        float bestT = 0f;

        // Перебираем все пары соседних PathPoint (i, i+1),
        // чтобы найти ближайший к боту отрезок маршрута.
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 A = pathPoints[i].pointTransform.position;
            Vector3 B = pathPoints[i + 1].pointTransform.position;

            Vector3 AB = B - A;
            float abSqrMag = AB.sqrMagnitude;
            if (abSqrMag < Mathf.Epsilon)
                continue; // пропускаем вырожденные отрезки

            // Параметр t при проекции позиции бота на отрезок [A, B].
            // Если t в [0..1], бот "между" A и B; 
            // если t < 0, он "перед" A; если t > 1, "за" B.
            float t = Mathf.Clamp01(Vector3.Dot(botPos - A, AB) / abSqrMag);

            // Ближайшая точка на отрезке к позиции бота
            Vector3 pointOnSegment = A + AB * t;

            // Проверяем, насколько бот близко к этому отрезку
            float distSqr = (botPos - pointOnSegment).sqrMagnitude;
            if (distSqr < minDistToSegment)
            {
                minDistToSegment = distSqr;
                closestSegmentIndex = i;
                bestT = t;
            }
        }

        // Если по какой-то причине не нашли сегмент, вернём последний PathPoint
        if (closestSegmentIndex == -1)
            return pathPoints[pathPoints.Length - 1];

        // Если бот где-то "между" точками (или у начала отрезка),
        // возвращаем следующую точку — pathPoints[closestSegmentIndex + 1].
        if (bestT < 1f)
        {
            return pathPoints[closestSegmentIndex + 1];
        }
        else
        {
            // Если бот "на" точке i+1 или "за" ней, 
            // считаем, что он её уже достиг — переходим к i+2 (если есть).
            int nextIndex = closestSegmentIndex + 2;
            if (nextIndex < pathPoints.Length)
            {
                return pathPoints[nextIndex];
            }
            else
            {
                // Если i+2 уже вне массива, возвращаем последний PathPoint
                return pathPoints[pathPoints.Length - 1];
            }
        }
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