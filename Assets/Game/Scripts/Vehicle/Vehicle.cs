using Unity.Netcode;
using UnityEngine;
using Photon.Pun;
using Random = UnityEngine.Random;

public class Vehicle : NetworkBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private Attack attack;
    [SerializeField] private TriggerCallBack triggerCallback;
    [SerializeField] internal HealthController healthController;
    [SerializeField] internal VehicleMovement vehicleMovement;

    internal bool isLocalPlayer;

    private void Awake()
    {
        triggerCallback.OnTriggerEntered += OnTriggerEntered;
        healthController.OnDeath += OnDeath;
    }

    private void OnTriggerEntered(Collider other)
    {
        if (other.TryGetComponent(out Follower fireball))
            fireball.SetTarget(this);
    }

    [PunRPC]
    internal void ChangePlayerName(string newName) => gameObject.name = newName;

    private void OnDeath() => vehicleMovement.CanMove = false;


    internal void SetLocalPlayer()
    {
        FunctionalButtons.Instance.vehicle = this;
        FunctionalButtons.Instance.attack = attack;
        FunctionalButtons.Instance.ListenToHealthController(healthController);
        isLocalPlayer = true;
        playerCam.gameObject.SetActive(true);
        vehicleMovement.enabled = true;
    }

    internal void SetBot()
    {
        isLocalPlayer = true;
        gameObject.name = Random.Range(0, 10).ToString();
        vehicleMovement.enabled = true;
    }

    private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(vehicleMovement.CanMove);
        else vehicleMovement.CanMove = (bool)stream.ReceiveNext();
    }

    public override void OnDestroy()
    {
        triggerCallback.OnTriggerEntered -= OnTriggerEntered;
        healthController.OnDeath -= OnDeath;
    }
}