using UnityEngine;
public class VehicleInput : MonoBehaviour
{
    private VehicleMovement vehicleMovement;
    private VehicleWheelController _vehicleWheelController;
    private float newSteeringPower = 0f;
    private FloatingJoystick _joystick;


    private void Awake()
    {
        vehicleMovement = GetComponent<VehicleMovement>();
        _vehicleWheelController = GetComponent<VehicleWheelController>();
        _joystick = FindAnyObjectByType<FunctionalButtons>()?.floatingJoystick;

    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Vertical");
        float turnInput = Input.GetAxisRaw("Horizontal");

        if (_joystick != null)
        {
            if (Mathf.Abs(_joystick.Vertical) > Mathf.Abs(moveInput))
                moveInput = _joystick.Vertical;

            if (Mathf.Abs(_joystick.Horizontal) > Mathf.Abs(turnInput))
                turnInput = _joystick.Horizontal;
        }

        vehicleMovement.VerticalInput = moveInput;
        vehicleMovement.HorizontalInput = turnInput;
        
        vehicleMovement.ApplyAcceleration(moveInput);

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