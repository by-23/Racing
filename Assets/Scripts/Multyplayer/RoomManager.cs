using Photon.Pun;
using Photon.Realtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private List<GameObject> playersList = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
    }

    public void CreateRoom(string name)
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        PhotonNetwork.CreateRoom(name, options);
    }

    public void JoinRoomByName(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log("Player Entered Room: " + newPlayer.NickName);
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

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("playersList"))
        {
            string json = PhotonNetwork.CurrentRoom.CustomProperties["playersList"] as string;
            playersList = JsonConvert.DeserializeObject<List<GameObject>>(json);
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
                },
                {
                    "playersList",
                    JsonConvert.SerializeObject(playersList,
                        new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            instantiatedPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity);
        }

        playersList.Add(instantiatedPlayer);

        var instantiatedPlayerComponents = instantiatedPlayer.GetComponent<PlayerComponents>();
        instantiatedPlayerComponents.vehicle.SetLocalPlayer();
        var actorNumber = instantiatedPlayerComponents.photonView.Owner.ActorNumber;
        UpdateUI(actorNumber, instantiatedPlayerComponents.playerInfo.PlayerName);
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
        photonView.RPC("UpdatePlayerNameUI", RpcTarget.AllBuffered, actorNumber, name);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log("Player Left Room: " + otherPlayer.NickName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected: " + cause.ToString());
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 spawnPoint;
        public bool isFull;
    }
}