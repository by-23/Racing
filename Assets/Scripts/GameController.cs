using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    private int totalCheckpoints;
    internal int currentCheckpointIndex = 0;
    private Dictionary<int, int> playerLaps = new Dictionary<int, int>();
    private int currentPlayerId;

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

    public void PlayerPassedCheckpoint(int playerId, int checkpointIndex)
    {
        if (checkpointIndex == currentCheckpointIndex)
        {
            currentCheckpointIndex++;
            if (currentCheckpointIndex >= totalCheckpoints)
            {
                currentCheckpointIndex = 0;
                PlayerCompletedLap(playerId);
                currentPlayerId = playerId;
            }
        }
    }

    private void PlayerCompletedLap(int playerId)
    {
        if (!playerLaps.ContainsKey(playerId))
        {
            playerLaps[playerId] = 0;
        }

        playerLaps[playerId]++;
        int currentLap = playerLaps[playerId];

        RoomManager.Instance.instantiatedPlayerInfoUIs[playerId].currentLap.text = currentLap.ToString();

        if (currentLap >= totalLaps)
        {
            OnAllLapsCompleted();
        }

        foreach (var checkpoint in checkpoints)
        {
            checkpoint.isActivated = false;
        }
    }

    [PunRPC]
    private void UpdateLapCount(int playerId, int currentLap)
    {
        if (playerLaps.ContainsKey(playerId))
        {
            playerLaps[playerId] = currentLap;
            // Здесь можно обновить UI или другие элементы, чтобы отобразить количество кругов для каждого игрока
        }
        else
        {
            playerLaps.Add(playerId, currentLap);
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