using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    public class LevelSelection : UIPanel
    {
        [SerializeField]
        GameObject virtualCamera = null;

        [SerializeField]
        CanvasGroup canvasGroup = null;

        [SerializeField]
        LevelData levelData = null;

        [SerializeField]
        LevelButton levelButtonPrefab = null;

        [SerializeField]
        Transform buttonContainer = null;

        [SerializeField]
        UIPanel returnPanel = null;

        public InputAction returnAction;

        private void Awake()
        {
            returnAction.performed += OnReturnActionPerformed;
        }

        private void OnEnable()
        {
            returnAction.Enable();
        }

        private void OnDisable()
        {
            returnAction.Disable();
        }

        private void OnReturnActionPerformed(InputAction.CallbackContext obj)
        {
            if(canvasGroup.interactable)
            {
                Hide();
                returnPanel.Show();
            }
        }


        private void Start()
        {
            InstantiateLevelButtons();
        }

        public override void Show()
        {
            virtualCamera.SetActive(true);
            canvasGroup.interactable = true;

            var firstButton = buttonContainer.GetChild(0);

            if(firstButton != null && firstButton.TryGetComponent<Selectable>(out var selectable))
            {
                selectable.Select();
            }
        }

        public override void Hide()
        {
            virtualCamera.SetActive(false);
            canvasGroup.interactable = false;
        }

        void InstantiateLevelButtons()
        {
            if (levelData != null && levelButtonPrefab != null)
            {
                int[] levels = levelData.GetLevelBuildIndeces();

                for (int i = 0; i < levels.Length; i++)
                {
                    int sceneIndex = levels[i];
                    int levelIndex = i + 1;

                    var levelButton = InstantiateLevelButton(sceneIndex, levelIndex);
                }
            }
        }

        LevelButton InstantiateLevelButton(int sceneIndex, int levelIndex)
        {
            var levelButton = Instantiate(levelButtonPrefab, buttonContainer);

            levelButton.SetText(levelIndex.ToString());
            levelButton.SetSceneIndex(sceneIndex);
            levelButton.SetLevelIndex(levelIndex);

            return levelButton;
        }
    }
}