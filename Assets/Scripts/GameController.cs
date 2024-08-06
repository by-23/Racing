using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    private int totalCheckpoints;
    private int currentCheckpointIndex = 0;
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

        photonView.RPC("UpdateLapCount", RpcTarget.All, playerId, currentLap);
        if (currentLap >= totalLaps)
        {
            OnAllLapsCompleted();
        }
    }

    [PunRPC]
    private void UpdateLapCount(int playerId, int lapCount)
    {
        if (currentPlayerId == playerId)
        {
            if (playerLaps.ContainsKey(currentPlayerId))
            {
                playerLaps[currentPlayerId] = lapCount;
                foreach (var player in playerLaps)
                {
                    Debug.LogError($"Player {player.Key} completed {player.Value} laps.");
                }
                // Здесь можно обновить UI или другие элементы, чтобы отобразить количество кругов для каждого игрока
            }
            else
            {
                Debug.LogError($"Player ID {currentPlayerId} not found.");
            }
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