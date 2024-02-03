using Ilumisoft.SkillDrive.Game;
using Ilumisoft.SkillDrive.LevelSelection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ilumisoft.SkillDrive.Input
{
    public class ExitLevelButton : MonoBehaviour
    {
        public InputAction exitAction;

        SceneLoader sceneLoader;

        private void Awake()
        {
            exitAction.performed += OnExit;
        }

        private void Start()
        {
            sceneLoader = FindObjectOfType<SceneLoader>();
        }

        private void OnEnable()
        {
            exitAction.Enable();
        }

        private void OnDisable()
        {
            exitAction.Disable();
        }

        private void OnExit(InputAction.CallbackContext obj)
        {
            LevelSelectionManager.LastLevel = -1;

            Time.timeScale = 1.0f;

            GameManager.IsRetry = false;

            sceneLoader.LoadScene(1);
        }
    }
}
