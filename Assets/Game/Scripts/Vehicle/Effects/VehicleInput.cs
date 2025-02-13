using System;
using Ilumisoft.SkillDrive;
using UnityEngine;

public class VehicleInput : MonoBehaviour
{
    private Vehicle vehicle;
    private float newSteeringPower = 0f;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
    }

    private void Update()
    {
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            float turnInput = UnityEngine.Input.GetAxisRaw("Horizontal");
#elif UNITY_ANDROID || UNITY_IOS
                float turnInput = floatingJoystick.Horizontal;
#endif
            newSteeringPower = turnInput * vehicle.steeringPower;

            vehicle.TurnWheels(turnInput * 30f);

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                vehicle.ResetCarPosition();
            }
        }
    }

    private void FixedUpdate()
    {
        vehicle.ApplySteering(newSteeringPower);
    }
}