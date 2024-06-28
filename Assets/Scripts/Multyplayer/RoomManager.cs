using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Ilumisoft.SkillDrive.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject player;
    [Space]
    [SerializeField] SpawnPoints spawnPoints;
    [SerializeField] MainMenu mainMenu;

    private void OnValidate()
    {
        if (spawnPoints == null)
        {
            spawnPoints = FindObjectOfType<SpawnPoints>();
        }
    }
    private void Start()
    {
        DontDestroyOnLoad(this);
        PhotonNetwork.ConnectUsingSettings();
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);
        mainMenu.playButton.onClick.AddListener(OnPlayButtonClicked);
    }


    private void OnPlayButtonClicked()
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
        PhotonNetwork.JoinOrCreateRoom("Room", null, null);
        Debug.Log("Connected to Lobby");
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Connected to Room");
    }
}
