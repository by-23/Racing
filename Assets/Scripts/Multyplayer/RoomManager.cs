using System.Collections;
using System.Collections.Generic;
using Ilumisoft.SkillDrive;
using UnityEngine;
using Photon.Pun;
using Ilumisoft.SkillDrive.UI;
using UnityEngine.Serialization;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [SerializeField] private GameObject player;
    [Space] [SerializeField] SpawnPoints spawnPoints;
    [SerializeField] MainMenu mainMenu;

    private void OnValidate()
    {
        if (spawnPoints == null)
        {
            spawnPoints = FindObjectOfType<SpawnPoints>();
        }
    }


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


    private void StartGame()
    {
        Vector3 currentSpawnPoint = Vector3.zero;

        foreach (var spawnPoint in spawnPoints.spawnPoints)
        {
            if (!spawnPoint.isFull)
            {
                currentSpawnPoint = spawnPoint.spawPoint.position;
                spawnPoint.isFull = true;
            }
        }

        GameObject instantiatedPlayer = PhotonNetwork.Instantiate(player.name, currentSpawnPoint, Quaternion.identity);
        instantiatedPlayer.GetComponent<Vehicle>().SetLocalPlayer();
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
        StartGame();
    }

    public void CreateRoom(string name)
    {
        PhotonNetwork.CreateRoom(name);
    }

    public void JoinRoomByName(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }
}