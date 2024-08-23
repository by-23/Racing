using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    private int totalCheckpoints;
    internal int currentCheckpointIndex = 0;
    private Dictionary<int, int> playerLaps = new Dictionary<int, int>();

    [SerializeField] private List<Checkpoint> checkpoints;
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private Transform finishedMenu;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
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
            OnAllLapsCompleted(playerId);
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

    private void OnAllLapsCompleted(int playerId)
    {
        Debug.Log("Player completed a Level!");
        var cameraFollow = RoomManager.Instance.playersList[playerId].cameraFollow;
        RoomManager.Instance.playersList.Remove(playerId);
        if (RoomManager.Instance.playersList.Count > 0)
        {
            cameraFollow.SetTarget(RoomManager.Instance.playersList.Last().Value.vehicle.transform);
        }
        else
        {
            cameraFollow.SetTarget(finishedMenu);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}