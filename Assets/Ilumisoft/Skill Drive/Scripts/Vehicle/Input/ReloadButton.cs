using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ilumisoft.SkillDrive.Input
{
    public class ReloadButton : MonoBehaviour
    {
        public InputAction reloadAction;

        SceneLoader sceneLoader;

        private void Awake()
        {
            reloadAction.performed += OnReload;
        }

        private void Start()
        {
            sceneLoader = FindObjectOfType<SceneLoader>();
        }

        private void OnEnable()
        {
            reloadAction.Enable();
        }

        private void OnDisable()
        {
            reloadAction.Disable();
        }

        private void OnReload(InputAction.CallbackContext obj)
        {
            GameManager.IsRetry = true;

            sceneLoader.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
