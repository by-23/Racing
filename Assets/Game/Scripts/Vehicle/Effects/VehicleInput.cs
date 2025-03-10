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
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            float turnInput = UnityEngine.Input.GetAxisRaw("Horizontal");
#elif UNITY_ANDROID || UNITY_IOS
                float turnInput = floatingJoystick.Horizontal;
#endif
            newSteeringPower = turnInput * vehicleMovement.steeringPower;

            vehicleDriving.TurnWheels(turnInput * 30f);

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                vehicleMovement.ResetCarPosition();
            }
        }
    }

    private void FixedUpdate()
    {
        vehicleMovement.ApplySteering(newSteeringPower);
    }
}