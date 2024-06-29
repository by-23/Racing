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
    [Space][SerializeField] List<SpawnPoint> spawnPoints;
    [SerializeField] MainMenu mainMenu;


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

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Connected to Room");

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("spawnPoints"))
        {
            string json = PhotonNetwork.CurrentRoom.CustomProperties["spawnPoints"] as string;
            spawnPoints = JsonConvert.DeserializeObject<List<SpawnPoint>>(json);
        }

        StartGame();
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

    private void StartGame()
    {
        Vector3 currentSpawnPoint = Vector3.zero;
        bool spawnPointSelected = false;

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

        if (spawnPointSelected)
        {
            GameObject instantiatedPlayer = PhotonNetwork.Instantiate(player.name, currentSpawnPoint, Quaternion.identity);
            instantiatedPlayer.GetComponent<Vehicle>().SetLocalPlayer();

            // Обновляем свойство комнаты с новым списком spawnPoints
            Hashtable props = new Hashtable
        {
            { "spawnPoints", JsonConvert.SerializeObject(spawnPoints, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) }
        };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }


    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 spawnPoint;
        public bool isFull;
    }
}