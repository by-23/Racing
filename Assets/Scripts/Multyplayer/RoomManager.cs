using System.Collections;
using Newtonsoft.Json;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine;
using Ilumisoft.SkillDrive.UI;
using System.Collections.Generic;
using Ilumisoft.SkillDrive;
using UnityEngine.Serialization;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;
    public bool isOnline;
    public Dictionary<int, PlayerInfoUI> instantiatedPlayerInfoUIs = new Dictionary<int, PlayerInfoUI>();

    [SerializeField] private GameObject playerPrefab;
    [Space] [SerializeField] List<SpawnPoint> spawnPoints;
    [SerializeField] private PlayerInfoUI playerInfoUIPrefab;

    [SerializeField] private GameObject playerInfoListUI;
    [SerializeField] private Canvas Joystick;
    [SerializeField] private List<GameObject> playersList;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        DontDestroyOnLoad(this);
        PhotonNetwork.ConnectUsingSettings();
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to master server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("Connected to Lobby");
    }

    public void CreateRoom(string name)
    {
        RoomOptions roomOptions = new RoomOptions();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        roomOptions.CustomRoomProperties = new Hashtable
        {
            { "spawnPoints", JsonConvert.SerializeObject(spawnPoints, settings) }
        };
        PhotonNetwork.CreateRoom(name, roomOptions);
    }

    public void JoinRoomByName(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        // Обработка нового игрока в комнате
        Debug.Log($"Player {newPlayer.NickName} joined the room");

        // Обновление UI для всех игроков
        foreach (var player in PhotonNetwork.PlayerList)
        {
            // var playerGameObject = GetPlayerGameObject(player);

            // if (playerGameObject != null)
            // {
            //     UpdateUI(newPlayer.ActorNumber, playerGameObject.GetComponent<PlayerInfo>().PlayerName);
            // }
        }
    }


    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Connected to Room");

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("spawnPoints"))
        {
            string json = PhotonNetwork.CurrentRoom.CustomProperties["spawnPoints"] as string;
            spawnPoints = JsonConvert.DeserializeObject<List<SpawnPoint>>(json);
        }

        isOnline = true;
        StartGame();
    }

    public void StartGame()
    {
        Vector3 currentSpawnPoint = Vector3.zero;
        bool spawnPointSelected = false;
        GameObject instantiatedPlayer;

        foreach (var spawnPoint in spawnPoints)
        {
            if (!spawnPoint.isFull)
            {
                currentSpawnPoint = spawnPoint.spawnPoint;
                spawnPoint.isFull = true;
                spawnPointSelected = true;
                break;
            }
        }

        if (spawnPointSelected && isOnline)
        {
            instantiatedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, currentSpawnPoint, Quaternion.identity);
           
            Hashtable props = new Hashtable
            {
                {
                    "spawnPoints",
                    JsonConvert.SerializeObject(spawnPoints,
                        new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            instantiatedPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity);
        }

        instantiatedPlayer.GetComponent<Vehicle>().SetLocalPlayer();
        var actorNumber = instantiatedPlayer.GetComponent<PhotonView>().Owner.ActorNumber;
        UpdateUI(actorNumber, instantiatedPlayer.GetComponent<PlayerInfo>().PlayerName);
    }

    [PunRPC]
    private void UpdatePlayerNameUI(int actorNumber, string name)
    {
        if (instantiatedPlayerInfoUIs.ContainsKey(actorNumber))
        {
            instantiatedPlayerInfoUIs[actorNumber].playerName.text = name;
        }
        else
        {
            var instantiatedPlayerInfoUI = Instantiate(playerInfoUIPrefab, playerInfoListUI.transform);
            instantiatedPlayerInfoUIs.Add(actorNumber, instantiatedPlayerInfoUI);
            instantiatedPlayerInfoUI.playerName.text = name;
        }
    }

    private void UpdateUI(int actorNumber, string name)
    {
        photonView.RPC("UpdatePlayerNameUI", RpcTarget.All, actorNumber, name);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        // Обработка выхода игрока из комнаты
        Debug.Log($"Player {otherPlayer.NickName} left the room");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.Log("Disconnected from server for reason: " + cause.ToString());
        isOnline = false;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 spawnPoint;
        public bool isFull;
    }
}