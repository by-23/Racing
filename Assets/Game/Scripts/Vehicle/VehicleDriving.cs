using System;
using UnityEngine;

public class VehicleDriving : MonoBehaviour
{
    [Header("Wheels")] [SerializeField] private float wheelsRotationSpeed = 100f;
    [SerializeField] private float wheelsTurnPercentage = 1f;
    [SerializeField] private float wheelsTurnSpeed = 20f;
    [SerializeField] private Transform FLwheel;
    [SerializeField] private Transform FRwheel;
    [SerializeField] private Transform BLwheel;
    [SerializeField] private Transform BRwheel;
    [SerializeField] private Transform FLwheelPivot;
    [SerializeField] private Transform FRwheelPivot;
    [SerializeField] private Transform BLwheelPivot;
    [SerializeField] private Transform BRwheelPivot;

    private VehicleMovement _vehicleMovement;
    private float currentTurnAngle;
    private Vehicle _vehicle;


    private void Awake()
    {
        _vehicle = GetComponent<Vehicle>();
        _vehicleMovement = GetComponent<VehicleMovement>();
    }

    private void FixedUpdate()
    {
        if (!_vehicle.isLocalPlayer) return;

        SpinWheels();
    }


    public void TurnWheels(float turnInput)
    {
        float targetAngle = Mathf.Clamp(turnInput, -wheelsTurnPercentage,
            wheelsTurnPercentage);
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetAngle, Time.deltaTime * wheelsTurnSpeed);

        FLwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        FRwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
        BLwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
        BRwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
    }

    private void SpinWheels()
    {
        float rotation = _vehicleMovement.ForwardSpeed * wheelsRotationSpeed * Time.deltaTime;
        foreach (var wheel in new[] { FLwheel, FRwheel, BLwheel, BRwheel })
            wheel.Rotate(Vector3.right, rotation);
    }
}