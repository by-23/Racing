using Ilumisoft.SkillDrive;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FunctionalButtons : Singleton<FunctionalButtons>
{
    public Vehicle vehicle;
    public Attack attack;
    public Button gasButton;
    public Button brakeButton;
    public Button fireButton;

    private bool isAccelerating = false;
    private float accelerationAmount = 0f;

    void Update()
    {
        if (isAccelerating)
        {
            ApplyAcceleration(accelerationAmount);
        }
    }

    public void OnPointerDownCustom(Button button)
    {
        Debug.Log("OnPointerDownCustom");
        if (button == gasButton)
        {
            isAccelerating = true;
            accelerationAmount = 1.0f;
        }
        else if (button == brakeButton)
        {
            isAccelerating = true;
            accelerationAmount = -1.0f;
        }
    }

    public void OnPointerUpCustom()
    {
        isAccelerating = false;
    }

    private void ApplyAcceleration(float amount)
    {
        if (vehicle != null)
        {
            vehicle.ApplyAcceleration(amount);
        }
    }

    public void FirePressed()
    {
        attack.TryFire();
    }
}