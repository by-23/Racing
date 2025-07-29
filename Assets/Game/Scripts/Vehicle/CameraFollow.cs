using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    public Transform Target => target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0, 0);

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

        UpdateCameraPosition();
        transform.rotation = Quaternion.Euler(90f, savedRotation.eulerAngles.y, savedRotation.eulerAngles.z);

    }

    private void UpdateCameraPosition()
    {
        Vector3 basePosition = target.position + Vector3.up;
        Vector3 finalPosition = basePosition + offset;

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
        transform.position = basePosition + offset;

       
        transform.rotation = Quaternion.Euler(90f, savedRotation.eulerAngles.y, savedRotation.eulerAngles.z);
        
    }
}