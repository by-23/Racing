using System;
using UnityEngine;

public class VehicleInput : MonoBehaviour
{
    private VehicleMovement vehicleMovement;
    private VehicleWheelController _vehicleWheelController;
    private float newSteeringPower = 0f;

    private void Awake()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
        _vehicleWheelController = GetComponent<VehicleWheelController>();
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Vertical");
        vehicleMovement.ApplyAcceleration(moveInput);

        float turnInput = Input.GetAxisRaw("Horizontal");
        newSteeringPower = turnInput * vehicleMovement.steeringPower;
        Vector3 velocity = vehicleMovement._rb.linearVelocity;
        float dot = Vector3.Dot(transform.forward, velocity);
        if (dot < 0)
        {
            newSteeringPower = -newSteeringPower;
        }

        vehicleMovement.ApplySteering(newSteeringPower);

        _vehicleWheelController.TurnWheels(turnInput * 30f);

        if (Input.GetKeyDown(KeyCode.R))
        {
            vehicleMovement.ResetCarPosition();
        }
    }
}