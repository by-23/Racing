using Newtonsoft.Json;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using Ilumisoft.SkillDrive.UI;
using System.Collections.Generic;
using Ilumisoft.SkillDrive;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [SerializeField] private GameObject player;
    [Space] [SerializeField] List<SpawnPoint> spawnPoints;
    [SerializeField] private Canvas Joystick;

    public bool isOnline;


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
            instantiatedPlayer = PhotonNetwork.Instantiate(player.name, currentSpawnPoint, Quaternion.identity);

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
            instantiatedPlayer = Instantiate(player, currentSpawnPoint, Quaternion.identity);
        }

        instantiatedPlayer.GetComponent<Vehicle>().SetLocalPlayer();

        Joystick.gameObject.SetActive(true);
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