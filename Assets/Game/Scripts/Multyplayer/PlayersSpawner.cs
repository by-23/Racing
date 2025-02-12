using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using UnityEngine;

public class PlayersSpawner : Singleton<PlayersSpawner>
{
    private GameObject currentInstantiatedPlayer;
    private RoomManager roomManager;

    [SerializedDictionary("ID", "Player")] [SerializeField]
    internal SerializedDictionary<int, PlayerComponents>
        playersList = new SerializedDictionary<int, PlayerComponents>();

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private List<GameObject> instantiatedBots;
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
        Vector3 currentSpawnPoint = Vector3.zero;

        currentSpawnPoint = GetAwailableSpawnPoint(currentSpawnPoint);

        if (!roomManager.IsOnline)
        {
            currentInstantiatedPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity);
            var instantiatedPlayerComponents = currentInstantiatedPlayer.GetComponent<PlayerComponents>();
            instantiatedPlayerComponents.vehicle.SetLocalPlayer();

            for (int i = 0; i < botCount; i++)
            {
                currentSpawnPoint = GetAwailableSpawnPoint(currentSpawnPoint);
                instantiatedBots.Add(Instantiate(botPrefab, currentSpawnPoint, Quaternion.identity));
                var instantiatedBotComponents = instantiatedBots[i].GetComponent<PlayerComponents>();
                instantiatedBotComponents.vehicle.SetBot();
            }

            roomManager.StartGame();
        }
        else
        {
            currentInstantiatedPlayer =
                PhotonNetwork.Instantiate(playerPrefab.name, currentSpawnPoint, Quaternion.identity);


            Hashtable props = new Hashtable
            {
                {
                    "spawnPoints",
                    JsonConvert.SerializeObject(spawnPoints,
                        new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            PlayerComponents instantiatedPlayerComponents = currentInstantiatedPlayer.GetComponent<PlayerComponents>();
            string playerName = instantiatedPlayerComponents.playerInfo.PlayerName;
            instantiatedPlayerComponents.photonView.RPC("ChangePlayerName", RpcTarget.AllBuffered, playerName);
            instantiatedPlayerComponents.vehicle.ChangePlayerName(playerName);
            instantiatedPlayerComponents.photonView.Owner.NickName = playerName;
            MainMenu.Instance.AddPlayerUI(instantiatedPlayerComponents.photonView.Owner);
        }
    }

    private Vector3 GetAwailableSpawnPoint(Vector3 currentSpawnPoint)
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
    public Vector3 spawnPoint;
    public bool isFull;
}