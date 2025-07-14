using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FunctionalButtons : MonoBehaviour
{
    public Vehicle vehicle;
    public ItemsController itemsController;
    public Button gasButton;
    public Button brakeButton;
    public Button fireButton;
    public Slider healthSlider;
    public Button menuButton;
    public Button continueButton;
    public Button quitToMenuButton;
    public GameObject pauseMenu;
    public FloatingJoystick floatingJoystick;

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
            accelerationAmount = 1f;
        }
        else if (button == brakeButton)
        {
            isAccelerating = true;
            accelerationAmount = -1f;
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
            vehicle.vehicleMovement.ApplyAcceleration(amount);
        }
    }

    public void FirePressed()
    {
        itemsController.ActivateTool(0);
    }

    private void OnDestroy()
    {
        if (healthController != null) healthController.OnHealthChanged -= OnHealthChanged;
    }
}