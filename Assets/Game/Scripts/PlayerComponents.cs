using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Vehicle))]
[RequireComponent(typeof(PlayerInfo))]
[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(HealthController))]
public class PlayerComponents : MonoBehaviour
{
    [SerializeField] public NetworkObject networkObject;
    [SerializeField] public Vehicle vehicle;
    [SerializeField] public PlayerInfo playerInfo;
    [SerializeField] public Attack attack;
    [SerializeField] public HealthController healthController;
    [SerializeField] public PhotonView photonView;
    [SerializeField] public CameraFollow cameraFollow;

    private void OnValidate()
    {
        if (networkObject == null) networkObject = GetComponent<NetworkObject>();
        if (vehicle == null) vehicle = GetComponent<Vehicle>();
        if (playerInfo == null) playerInfo = GetComponent<PlayerInfo>();
        if (attack == null) attack = GetComponent<Attack>();
        if (healthController == null) healthController = GetComponent<HealthController>();
        if (photonView == null) photonView = GetComponent<PhotonView>();
        if (cameraFollow == null) cameraFollow = GetComponentInChildren<CameraFollow>();
    }

    private void Awake()
    {
        if (RoomManager.Instance.IsOnline)
            PlayersSpawner.Instance.playersList.Add(photonView.Owner.ActorNumber, this);
    }

    [PunRPC]
    public void ChangePlayerName(string newName)
    {
        if (playerInfo != null)
        {
            playerInfo.PlayerName = newName;
            Debug.Log("PlayerComponents: Имя игрока изменено на " + newName);
        }
    }

    [System.Serializable]
    public class PlayerInfo
    {
        public string PlayerName;
    }
}