using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private int totalCheckpoints;
    private int currentCheckpointIndex = 0;
    private int currentLap = 0;

    [SerializeField] private List<Checkpoint> checkpoints;
    [SerializeField] private int totalLaps = 3;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += OnSceneLoaded;

        checkpoints.AddRange(FindObjectsOfType<Checkpoint>());
        totalCheckpoints = checkpoints.Count;
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.gameController = this;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }

    public void PlayerPassedCheckpoint(int checkpointIndex)
    {
        if (checkpointIndex == currentCheckpointIndex)
        {
            currentCheckpointIndex++;
            if (currentCheckpointIndex >= totalCheckpoints)
            {
                currentCheckpointIndex = 0;
                PlayerCompletedLap();
            }
        }
    }

    private void PlayerCompletedLap()
    {
        Debug.Log(currentLap);
        currentLap++;
        if (currentLap >= totalLaps)
        {
            OnAllLapsCompleted();
        }
    }

    private void OnAllLapsCompleted()
    {
        Debug.Log("Player completed a Level!");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}