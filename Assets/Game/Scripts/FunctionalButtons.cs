using System;
using System.Collections;
using Ilumisoft.SkillDrive;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FunctionalButtons : Singleton<FunctionalButtons>
{
    public Vehicle vehicle;
    public Attack attack;
    public Button gasButton;
    public Button brakeButton;
    public Button fireButton;
    public Slider healthSlider;
    public Button menuButton;
    public Button continueButton;
    public Button quitToMenuButton;
    public GameObject pauseMenu;

    private bool isAccelerating = false;
    private float accelerationAmount = 0f;
    private HealthController healthController;


    public void ListenToHealthController(HealthController healthController)
    {
        this.healthController = healthController;
        healthController.OnHealthChanged += OnHealthChanged;
    }

    void Update()
    {
        if (isAccelerating)
        {
            ApplyAcceleration(accelerationAmount);
        }
    }

    public void OpenPauseMenu()
    {
        pauseMenu.SetActive(true);
        menuButton.gameObject.SetActive(false);
    }

    public void ClosePauseMenu()
    {
        pauseMenu.SetActive(false);
        menuButton.gameObject.SetActive(true);
    }

    public void RestartLevel()
    {
        pauseMenu.SetActive(false);
        menuButton.gameObject.SetActive(true);
    }

    public void QuitToMenu()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHealthChanged(float health)
    {
        healthSlider.value = health / 100;
    }

    public void OnPointerDownCustom(Button button)
    {
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

    private void OnDestroy()
    {
        if (healthController != null) healthController.OnHealthChanged -= OnHealthChanged;
    }
}