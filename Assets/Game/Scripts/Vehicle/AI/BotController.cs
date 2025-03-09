using UnityEngine;
using UnityEngine.AI;

public class BotController : MonoBehaviour
{
    public Transform[] targets; // Массив целей
    private NavMeshAgent agent;
    private NavMeshPath cachedPath;
    private int currentTargetIndex = 0;
    public float switchDistance = 1.5f; // Дистанция, на которой бот переключается на следующую цель

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath = false;
        cachedPath = new NavMeshPath();
        MoveToNextTarget();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= switchDistance)
        {
            if (currentTargetIndex < targets.Length - 1)
            {
                currentTargetIndex++;
                MoveToNextTarget();
            }
            else
            {
                Debug.Log("Бот достиг последней цели!");
            }
        }
    }

    void MoveToNextTarget()
    {
        if (targets.Length == 0) return;

        if (NavMesh.CalculatePath(transform.position, targets[currentTargetIndex].position, NavMesh.AllAreas,
                cachedPath))
        {
            agent.SetPath(cachedPath);
        }
        else
        {
            Debug.LogError("Не удалось построить путь к цели: " + currentTargetIndex);
        }
    }

    void OnDrawGizmos()
    {
        if (cachedPath != null && cachedPath.corners.Length > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < cachedPath.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(cachedPath.corners[i], cachedPath.corners[i + 1]);
            }
        }
    }
}