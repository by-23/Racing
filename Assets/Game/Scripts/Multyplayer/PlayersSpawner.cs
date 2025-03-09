using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayersSpawner : Singleton<PlayersSpawner>
{
    private GameObject currentInstantiatedPlayer;
    private RoomManager roomManager;

    [SerializedDictionary("ID", "Player")] [SerializeField]
    internal SerializedDictionary<int, PlayerComponents>
        playersList = new SerializedDictionary<int, PlayerComponents>();

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;
    [SerializeField] internal List<GameObject> instantiatedPlayers;
    [Space] [SerializeField] List<SpawnPoint> spawnPoints;
    [SerializeField, Range(0, 7)] internal int botCount;

    public GameObject CurrentInstantiatedPlayer
    {
        get => currentInstantiatedPlayer;
        set => currentInstantiatedPlayer = value;
    }

    private void Start()
    {
        roomManager = RoomManager.Instance;
    }

    protected internal void ChancheBotCount(int botCount)
    {
        this.botCount = botCount;
    }

    protected internal void SpawnPlayer()
    {
        Transform currentSpawnPoint = null;

        if (!roomManager.IsOnline)
        {
            for (int i = 0; i < botCount; i++)
            {
                currentSpawnPoint = GetAwailableSpawnPoint(currentSpawnPoint);
                instantiatedPlayers.Add(Instantiate(botPrefab, currentSpawnPoint.position, currentSpawnPoint.rotation));
                var playerComponents = instantiatedPlayers[i].GetComponent<PlayerComponents>();
                playerComponents.vehicle.SetBot();
            }

            if (!GameController.Instance.isDevMode)
            {
                currentSpawnPoint = GetAwailableSpawnPoint(currentSpawnPoint);
                currentInstantiatedPlayer =
                    Instantiate(playerPrefab, currentSpawnPoint.position, currentSpawnPoint.rotation);
                instantiatedPlayers.Add(currentInstantiatedPlayer);
                var instantiatedPlayerComponents = currentInstantiatedPlayer.GetComponent<PlayerComponents>();
                instantiatedPlayerComponents.vehicle.SetLocalPlayer();
            }

            roomManager.StartGame();
        }
        else
        {
            if (GameController.Instance.isDevMode) return;

            currentInstantiatedPlayer =
                PhotonNetwork.Instantiate(playerPrefab.name, currentSpawnPoint.position,
                    currentSpawnPoint.rotation);


            Hashtable props = new Hashtable
            {
                {
                    "spawnPoints",
                    JsonConvert.SerializeObject(spawnPoints,
                        new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            PlayerComponents instantiatedPlayerComponents =
                currentInstantiatedPlayer.GetComponent<PlayerComponents>();
            string playerName = instantiatedPlayerComponents.playerInfo.PlayerName;
            instantiatedPlayerComponents.photonView.RPC("ChangePlayerName", RpcTarget.AllBuffered, playerName);
            instantiatedPlayerComponents.vehicle.ChangePlayerName(playerName);
            instantiatedPlayerComponents.photonView.Owner.NickName = playerName;
            MyMainMenu.Instance.AddPlayerUI(instantiatedPlayerComponents.photonView.Owner);
        }
    }

    private Transform GetAwailableSpawnPoint(Transform currentSpawnPoint)
    {
        foreach (var spawnPoint in spawnPoints)
        {
            if (!spawnPoint.isFull)
            {
                currentSpawnPoint = spawnPoint.spawnPoint;
                spawnPoint.isFull = true;
                break;
            }
        }

        return currentSpawnPoint;
    }

    internal void UpdateSpawnPoints()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("spawnPoints"))
        {
            string json = PhotonNetwork.CurrentRoom.CustomProperties["spawnPoints"] as string;
            spawnPoints = JsonConvert.DeserializeObject<List<SpawnPoint>>(json);
        }
    }
}

[System.Serializable]
public class SpawnPoint
{
    public Transform spawnPoint;
    public bool isFull;
}