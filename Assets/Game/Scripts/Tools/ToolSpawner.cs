using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NTC.Pool;

[System.Serializable]
public class ToolSpawnSettings
{
    [Header("Объект для спавна")]
    public GameObject toolPrefab;

    [Header("Настройки вероятности")]
    [Range(0f, 1f)]
    public float spawnProbability = 1f;
}

public class ToolSpawner : MonoBehaviour
{
    [Header("Объекты для спавна")]
    [SerializeField] private ToolSpawnSettings[] toolsToSpawn;

    [Header("Настройки спавна")]
    [SerializeField] private int maxSimultaneousTools = 5;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float toolLifetime = 10f;
    [SerializeField] private float minDistanceBetweenTools = 3f;
    
    [Header("Настройки смещения от пути")]
    [SerializeField] private Vector2 sideOffsetRange = new Vector2(-5f, 5f);
    [SerializeField] private Vector2 forwardOffsetRange = new Vector2(-3f, 3f);
    [SerializeField] private float heightOffset = 1f;
    
    [Header("Настройки пути")]
    [SerializeField] private float pathCheckDistance = 20f;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    [SerializeField] private LayerMask groundLayerMask = 1;

    private EzPath ezPath;
    private List<GameObject> spawnedTools = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int currentPathIndex = 0;
    private int maxSpawnAttempts = 10;

    private void Awake()
    {
        ezPath = GetComponent<EzPath>();
        if (ezPath == null)
            ezPath = EzPath.Instance;
    }

    private void Start()
    {
        if (ezPath != null && toolsToSpawn.Length > 0)
        {
            // Сразу спавним максимальное количество объектов в начале игры
            SpawnInitialTools();
            
            // Затем запускаем периодический спавн
            StartSpawning();
        }
    }

    private void SpawnInitialTools()
    {
        for (int i = 0; i < maxSimultaneousTools; i++)
        {
            if (!TrySpawnTool())
            {
                // Если не удалось заспавнить объект, прекращаем попытки
                break;
            }
        }
    }

    private void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    private void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (spawnedTools.Count < maxSimultaneousTools)
            {
                TrySpawnTool();
            }
        }
    }

    private bool TrySpawnTool()
    {
        if (ezPath == null || ezPath.pathPoints == null || ezPath.pathPoints.Length == 0)
            return false;

        // Пытаемся найти подходящую позицию несколько раз
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Выбираем случайную точку пути
            int randomIndex = Random.Range(0, ezPath.pathPoints.Length);
            Vector3 spawnPosition = GetSpawnPosition(randomIndex);
            
            // Проверяем, можно ли спавнить в этой позиции
            if (IsValidSpawnPosition(spawnPosition))
            {
                SpawnRandomTool(spawnPosition);
                return true;
            }
        }
        
        // Не удалось найти подходящее место за максимальное количество попыток
        return false;
    }

    private Vector3 GetSpawnPosition(int pathIndex)
    {
        if (pathIndex >= ezPath.pathPoints.Length)
            return transform.position;

        Vector3 basePosition = ezPath.pathPoints[pathIndex].pointTransform.position;

        // Добавляем случайное смещение
        Vector3 rightDirection = Vector3.right;
        Vector3 forwardDirection = Vector3.forward;

        // Пытаемся получить направление от текущей точки к следующей
        if (pathIndex < ezPath.pathPoints.Length - 1)
        {
            Vector3 pathDirection = (ezPath.pathPoints[pathIndex + 1].pointTransform.position - basePosition).normalized;
            forwardDirection = pathDirection;
            rightDirection = Vector3.Cross(Vector3.up, pathDirection).normalized;
        }

        // Используем общие настройки смещения
        float sideOffset = Random.Range(sideOffsetRange.x, sideOffsetRange.y);
        float forwardOffset = Random.Range(forwardOffsetRange.x, forwardOffsetRange.y);

        Vector3 finalPosition = basePosition + rightDirection * sideOffset + forwardDirection * forwardOffset;

        // Улучшенное определение высоты поверхности
        Vector3 rayStart = finalPosition + Vector3.up * 20f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, groundLayerMask))
        {
            // Спавним объект выше поверхности на заданную высоту
            finalPosition.y = hit.point.y + heightOffset;
        }
        else
        {
            // Если поверхность не найдена, используем высоту точки пути + смещение
            finalPosition.y = basePosition.y + heightOffset;
        }

        return finalPosition;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Проверяем, нет ли препятствий
        if (Physics.CheckSphere(position, 1f, obstacleLayerMask))
        {
            return false;
        }
        
        // Проверяем, нет ли поблизости других заспавненных объектов
        foreach (GameObject tool in spawnedTools)
        {
            if (tool != null && Vector3.Distance(tool.transform.position, position) < minDistanceBetweenTools)
            {
                return false;
            }
        }
        
        return true;
    }

    private void SpawnRandomTool(Vector3 position)
    {
        if (toolsToSpawn.Length == 0) return;

        // Выбираем инструмент на основе вероятности
        ToolSpawnSettings selectedTool = SelectRandomTool();
        if (selectedTool == null || selectedTool.toolPrefab == null) return;

        // Спавним через NightPool
        GameObject spawnedTool = NightPool.Spawn(selectedTool.toolPrefab, position, Quaternion.identity);

        if (spawnedTool != null)
        {
            spawnedTools.Add(spawnedTool);

            // Добавляем случайную ротацию
            spawnedTool.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Запускаем корутину для автоматического деспавна
            StartCoroutine(DespawnAfterTime(spawnedTool, toolLifetime));
        }
    }

    private ToolSpawnSettings SelectRandomTool()
    {
        float totalProbability = 0f;
        foreach (var tool in toolsToSpawn)
        {
            totalProbability += tool.spawnProbability;
        }

        if (totalProbability == 0f) return null;

        float randomValue = Random.Range(0f, totalProbability);
        float currentProbability = 0f;

        foreach (var tool in toolsToSpawn)
        {
            currentProbability += tool.spawnProbability;
            if (randomValue <= currentProbability)
            {
                return tool;
            }
        }

        return toolsToSpawn[toolsToSpawn.Length - 1];
    }

    private IEnumerator DespawnAfterTime(GameObject tool, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (tool != null && spawnedTools.Contains(tool))
        {
            spawnedTools.Remove(tool);
            NightPool.Despawn(tool);
        }
    }

    public bool ManualSpawn()
    {
        return TrySpawnTool();
    }

    public void ClearAllSpawnedTools()
    {
        foreach (GameObject tool in spawnedTools)
        {
            if (tool != null)
            {
                NightPool.Despawn(tool);
            }
        }
        spawnedTools.Clear();
    }

    private void OnDestroy()
    {
        StopSpawning();
        ClearAllSpawnedTools();
    }

    private void OnDrawGizmosSelected()
    {
        if (ezPath == null) return;
        
        Gizmos.color = Color.green;
        
        // Отображаем зону спавна вокруг точек пути
        for (int i = 0; i < ezPath.pathPoints.Length; i++)
        {
            if (ezPath.pathPoints[i].pointTransform != null)
            {
                Vector3 point = ezPath.pathPoints[i].pointTransform.position;
                Gizmos.DrawWireSphere(point, pathCheckDistance * 0.5f);
            }
        }
        
        // Отображаем заспавненные объекты и их зоны минимального расстояния
        Gizmos.color = Color.red;
        foreach (GameObject tool in spawnedTools)
        {
            if (tool != null)
            {
                Gizmos.DrawWireSphere(tool.transform.position, 1f);
                
                // Показываем зону минимального расстояния
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(tool.transform.position, minDistanceBetweenTools);
                Gizmos.color = Color.red;
            }
        }
    }
}