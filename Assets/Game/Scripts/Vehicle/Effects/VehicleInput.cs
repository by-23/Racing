using System;
using UnityEngine;

public class VehicleInput : MonoBehaviour
{
    private VehicleMovement vehicleMovement;
    private VehicleDriving vehicleDriving;
    private float newSteeringPower = 0f;

    private void Awake()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
        vehicleDriving = GetComponent<VehicleDriving>();
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

        vehicleDriving.TurnWheels(turnInput * 30f);

        if (Input.GetKeyDown(KeyCode.R))
        {
            vehicleMovement.ResetCarPosition();
        }
    }
}