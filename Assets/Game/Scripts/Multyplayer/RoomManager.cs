using Photon.Pun;
using Photon.Realtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;
    private bool isOnline;

    [SerializeField] private MyMainMenu mainMenu;
    [SerializeField] protected internal Director direktor;
    [SerializeField] private int maxPlayersInLobby = 4;
    [SerializeField] private PlayersSpawner playerSpawner;
    [SerializeField] private UI ui;

    public bool IsOnline
    {
        get => isOnline;
        set => isOnline = value;
    }

    private void Awake()
    {
        Instance = this;
        playerSpawner = GetComponent<PlayersSpawner>();
        ui = GetComponent<UI>();
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
        playerSpawner.playersList.Clear();
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = maxPlayersInLobby;
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

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log("Player Left Room: " + otherPlayer.NickName);

        mainMenu.RemovePlayerUI(otherPlayer);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Connected to Room");
        playerSpawner.UpdateSpawnPoints();
        isOnline = true;
        direktor.MoveCamera("Lobby");
        playerSpawner.SpawnPlayer();
    }


    protected internal void StartGame()
    {
        if (playerSpawner.CurrentInstantiatedPlayer != null)
        {
            ui.ControlsUIVisibility(true);

            var instantiatedPlayerComponents = playerSpawner.CurrentInstantiatedPlayer.GetComponent<PlayerComponents>();
            instantiatedPlayerComponents.vehicle.SetLocalPlayer();
            if (isOnline)
            {
                var actorNumber = instantiatedPlayerComponents.photonView.Owner.ActorNumber;
                ui.UpdateUI(actorNumber, instantiatedPlayerComponents.playerInfo.PlayerName);
                playerSpawner.UpdateSpawnPoints();
            }
        }
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        RoomList.Instance.UpdaterUI();
        Debug.Log("Disconnected: " + cause.ToString());
    }
}