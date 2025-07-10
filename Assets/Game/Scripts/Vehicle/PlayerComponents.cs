using Photon.Pun;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Vehicle))]
[RequireComponent(typeof(PlayerInfo))]
[RequireComponent(typeof(ItemsController))]
[RequireComponent(typeof(HealthController))]
public class PlayerComponents : MonoBehaviour
{
    [SerializeField] public NetworkObject networkObject;
    [SerializeField] public Vehicle vehicle;
    [SerializeField] public PlayerInfo playerInfo;
    [SerializeField] public ItemsController itemsController;
    [SerializeField] public HealthController healthController;
    [SerializeField] public PhotonView photonView;
    [SerializeField] public CameraFollow cameraFollow;

    private void OnValidate()
    {
        if (networkObject == null) networkObject = GetComponent<NetworkObject>();
        if (vehicle == null) vehicle = GetComponent<Vehicle>();
        if (playerInfo == null) playerInfo = GetComponent<PlayerInfo>();
        if (itemsController == null) itemsController = GetComponent<ItemsController>();
        if (healthController == null) healthController = GetComponent<HealthController>();
        if (photonView == null) photonView = GetComponent<PhotonView>();
        if (cameraFollow == null) cameraFollow = GetComponentInChildren<CameraFollow>();
    }

    private void Awake()
    {
        if (RoomManager.Instance != null && RoomManager.Instance.IsOnline)
            PlayersSpawner.Instance.playersList.Add(photonView.Owner.ActorNumber, this);
    }
}