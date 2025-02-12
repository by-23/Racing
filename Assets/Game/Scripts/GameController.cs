using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    #region Fields

    /// <summary>
    ///     The instance.
    /// </summary>
    private static GameController instance;

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static GameController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameController>();
                if (instance == null)
                {
                    var obj = new GameObject();
                    obj.name = typeof(GameController).Name;
                    instance = obj.AddComponent<GameController>();
                }
            }

            return instance;
        }
    }

    #endregion

    private int totalCheckpoints;
    internal int currentCheckpointIndex = 0;
    private Dictionary<int, int> playerLaps = new Dictionary<int, int>();

    [SerializeField] internal int totalLaps = 3;
    [SerializeField] internal Transform finishedMenu;
    [SerializeField] internal bool isGameStarted = false;

    private void Awake()
    {
        if (instance == null)
            instance = this as GameController;
        else
            Destroy(gameObject);

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        UI.Instance.instantiatedPlayerInfoUIs[playerId].currentLap.text = currentLap.ToString();

        if (currentLap >= totalLaps)
        {
            OnAllLapsCompleted(playerId);
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
        var cameraFollow = PlayersSpawner.Instance.playersList[playerId].cameraFollow;
        PlayersSpawner.Instance.playersList.Remove(playerId);
        if (PlayersSpawner.Instance.playersList.Count > 0)
        {
            cameraFollow.SetTarget(PlayersSpawner.Instance.playersList.Last().Value.vehicle.transform);
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