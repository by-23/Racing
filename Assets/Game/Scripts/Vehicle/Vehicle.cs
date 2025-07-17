using System.Linq;
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
        GameController.Instance.OnVehicleFinished += OnFinish;
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

    private void OnFinish(Vehicle finishedVehicle)
    {
        if (finishedVehicle == this)
        {
            vehicleMovement.CanMove = false;
            // Проверяем не только флаг, но и наличие камеры, чтобы убедиться, что это игрок, а не бот.
            if (isLocalPlayer && playerCam != null)
            {
                playerCam.transform.SetParent(null); // Отсоединяем камеру
                SwitchToSpectatorCamera();
            }
        }
        // Переключаем камеру, только если мы наблюдаем за кем-то, и этот кто-то финишировал.
        else if(isLocalPlayer && playerCam != null && playerCam.GetComponent<CameraFollow>().Target == finishedVehicle.transform)
        {
            // Если игрок, за которым мы наблюдаем, финишировал, ищем нового
            SwitchToSpectatorCamera();
        }
    }

    private void SwitchToSpectatorCamera()
    {
        var racingVehicles = GameController.Instance.GetRacingVehicles()
            .Where(v => v != this && !GameController.Instance.IsVehicleFinished(v))
            .ToList();
            
        if (racingVehicles.Any())
        {
            Vehicle vehicleToSpectate = racingVehicles[Random.Range(0, racingVehicles.Count)];
            playerCam.GetComponent<CameraFollow>().SetTarget(vehicleToSpectate.transform);
        }
        else
        {
            playerCam.gameObject.SetActive(false);
            // тут можно показать финальную таблицу
        }
    }

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
        {
            GameController.Instance.UnregisterVehicle(this);
            GameController.Instance.OnVehicleFinished -= OnFinish;
        }
            
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