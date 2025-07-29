using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    public Transform Target => target;

    [Header("Настройки вида сзади")]
    [SerializeField] private Vector3 rearViewOffset = new Vector3(0f, 20f, -30f);
    [SerializeField] private Vector3 rearViewTargetOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private KeyCode lookBehindKey = KeyCode.B;

    [Header("Общие настройки")]
    [SerializeField] private float followHeight = 80;
    
    private bool isLookingBehind;
    private Quaternion savedRotation;
    private Vehicle _ownerVehicle;

    private void Start()
    {
        _ownerVehicle = GetComponentInParent<Vehicle>();
        if (_ownerVehicle != null)
        {
            savedRotation = _ownerVehicle.transform.rotation;
            GameController.Instance.OnVehicleFinished += OnVehicleFinished;
        }
    }
    
    private void OnDestroy()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.OnVehicleFinished -= OnVehicleFinished;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(lookBehindKey))
        {
            isLookingBehind = !isLookingBehind;
        }
    }

    private void OnVehicleFinished(Vehicle finishedVehicle)
    {
        // Если финишировал владелец этой камеры, или тот, за кем мы наблюдаем
        if (finishedVehicle == _ownerVehicle || finishedVehicle.transform == target)
        {
            SwitchToSpectatorCamera();
        }
    }

    private void SwitchToSpectatorCamera()
    {
        var racingVehicles = GameController.Instance.GetRacingVehicles()
            .Where(v => v != _ownerVehicle && !GameController.Instance.IsVehicleFinished(v))
            .ToList();
            
        if (racingVehicles.Any())
        {
            Vehicle vehicleToSpectate = racingVehicles[Random.Range(0, racingVehicles.Count)];
            isLookingBehind = false; // Отключаем вид сзади при переключении на другого игрока
            SetTarget(vehicleToSpectate.transform);
        }
        else
        {
            gameObject.SetActive(false);
            // тут можно показать финальную таблицу
        }
    }


    void LateUpdate()
    {
        if (!target) return;

        if (isLookingBehind)
        {
            UpdateRearView();
        }
        else
        {
            UpdateNormalView();
        }
    }

    private void UpdateNormalView()
    {
        Vector3 finalPosition = target.position;
        finalPosition.y = followHeight;
        transform.position = finalPosition;
        transform.rotation = Quaternion.Euler(90f, savedRotation.eulerAngles.y, savedRotation.eulerAngles.z);
    }

    private void UpdateRearView()
    {
        // Используем TransformPoint, но скорректируем позицию по высоте, чтобы избежать проваливания
        Vector3 desiredPosition = target.position + target.right * rearViewOffset.x + Vector3.up * rearViewOffset.y + target.forward * rearViewOffset.z;
        
        // Точка, на которую смотрит камера
        Vector3 lookAtPoint = target.position + rearViewTargetOffset;
        
        transform.position = desiredPosition;
        transform.LookAt(lookAtPoint);
    }

    private void UpdateCameraPosition()
    {
        Vector3 basePosition = target.position + Vector3.up;
        Vector3 finalPosition = basePosition;

        transform.position = finalPosition;
    }
    
    
    public void SetTarget(Transform newTarget)
    {
        if (transform.parent != null)
        {
            transform.SetParent(null); // Отсоединяем камеру при первой смене цели
        }
        
        target = newTarget;

        // Мгновенный телепорт к цели
        Vector3 basePosition = newTarget.position + Vector3.up;
        transform.position = basePosition;

       
        transform.rotation = Quaternion.Euler(90f, savedRotation.eulerAngles.y, savedRotation.eulerAngles.z);
        
    }
}