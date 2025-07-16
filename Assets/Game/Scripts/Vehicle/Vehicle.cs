using Photon.Pun;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Vehicle : NetworkBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private ItemsController itemsController;
    [SerializeField] private TriggerCallBack triggerCallback;
    [SerializeField] internal HealthController healthController;
    [SerializeField] internal VehicleMovement vehicleMovement;
    [SerializeField] internal bool isLocalPlayer;
    [SerializeField] public Transform muzzlePosition;
    [SerializeField] private FunctionalButtons functionalButtons;

    private void Start()
    {
        GameController.Instance.RegisterVehicle(this);
    }

    private void Awake()
    {
        triggerCallback.OnTriggerEntered += OnTriggerEntered;
        healthController.OnDeath += OnDeath;
        Init();
    }

    private void Init()
    {
        if (!functionalButtons) functionalButtons = FindAnyObjectByType<FunctionalButtons>();
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
        functionalButtons.vehicle = this;
        functionalButtons.itemsController = itemsController;
        functionalButtons.ListenToHealthController(healthController);
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
        if(GameController.Instance != null)
            GameController.Instance.UnregisterVehicle(this);
            
        triggerCallback.OnTriggerEntered -= OnTriggerEntered;
        healthController.OnDeath -= OnDeath;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Init();
        EditorUtility.SetDirty(this);
    }
#endif
}