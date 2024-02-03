using Ilumisoft.SkillDrive.Game;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.LevelSelection
{
    public class LevelSelectionManager : MonoBehaviour
    {
        public static int LastLevel = -1;

        [Header("Button Setup")] [SerializeField]
        LevelSelectButton levelSelectButtonPrefab = null;

        [SerializeField] GameObject buttonContainer = null;

        [Header("Level Setup")] [SerializeField]
        LevelData levelData = null;

        [SerializeField, FormerlySerializedAs("levelSelects")]
        List<LevelPreview> levelPreviews = null;

        [Header("SFX")] [SerializeField] AudioSource confirmSFX = null;

        [SerializeField] AudioSource deniedSFX = null;

        [Header("Other")] [SerializeField] InputAction returnInputAction = null;

        SceneLoader sceneLoader;

        int currentIndex = 0;

        Coroutine levelSwitchCoroutine = null;

        private void OnEnable()
        {
            returnInputAction.Enable();
        }

        private void OnDisable()
        {
            returnInputAction.Disable();
        }

        private void Awake()
        {
            returnInputAction.started += (arg) => { LoadMenu(); };
        }

        private IEnumerator Start()
        {
            sceneLoader = FindObjectOfType<SceneLoader>();

            SetupLevelPreviewsAndButtons();

            if (LastLevel != -1)
            {
                currentIndex = LastLevel;

                levelPreviews[currentIndex].Select();

                buttonContainer.transform.GetChild(currentIndex).GetComponent<Selectable>().Select();

                yield return new WaitForSecondsRealtime(1.0f);

                int currentLevelNumber = currentIndex + 1;
                int nextLevelNumber = currentLevelNumber + 1;

                if (LevelLockManager.IsLocked(nextLevelNumber) == false)
                {
                    SelectNextLevel();
                }
            }
            else
            {
                currentIndex = 0;

                for (int i = 0; i < levelPreviews.Count; i++)
                {
                    int levelNumber = i + 1;

                    if (IsLevelComplete(levelNumber) == false)
                    {
                        currentIndex = i;

                        break;
                    }
                }

                levelPreviews[currentIndex].Select();
            }

            buttonContainer.transform.GetChild(currentIndex).GetComponent<Selectable>().Select();
        }

        void SetupLevelPreviewsAndButtons()
        {
            for (int i = 0; i < levelPreviews.Count; i++)
            {
                int levelNumber = i + 1;

                bool isLocked = IsLevelLocked(levelNumber);

                levelPreviews[i].ShowLock(isLocked);
                levelPreviews[i].ShowCompletedText(IsLevelComplete(levelNumber));

                var buttonInstance = Instantiate(levelSelectButtonPrefab, buttonContainer.transform);
                buttonInstance.Initialize(this, levelNumber);
            }
        }

        bool IsLevelLocked(int levelNumber) => LevelLockManager.IsLocked(levelNumber);

        bool IsLevelComplete(int levelNumber) => !LevelLockManager.IsLocked(levelNumber + 1);

        bool IsCurrentLevelLocked => LevelLockManager.IsLocked(currentIndex + 1);

        public void LoadMenu()
        {
            sceneLoader.LoadScene(0);
        }

        public void GoToLevel(int levelNumber)
        {
            if (levelSwitchCoroutine != null)
            {
                StopCoroutine(levelSwitchCoroutine);
            }

            levelSwitchCoroutine = StartCoroutine(ExecuteDelayed(() =>
            {
                levelPreviews[currentIndex].Deselect();

                currentIndex = levelNumber - 1;

                levelPreviews[currentIndex].Select();
            }, 0.5f));
        }

        public void SelectNextLevel()
        {
            if (currentIndex < levelPreviews.Count - 1)
            {
                levelPreviews[currentIndex].Deselect();

                currentIndex++;

                levelPreviews[currentIndex].Select();
            }

            var button = buttonContainer.transform.GetChild(currentIndex);
            button.GetComponent<Selectable>().Select();
        }

        public void LoadLevel(int levelNumber)
        {
            if (IsCurrentLevelLocked == false)
            {
                StopAllCoroutines();
                StartCoroutine(LoadLevelCoroutine(levelNumber));
            }
            else
            {
                deniedSFX.Play();
            }
        }

        IEnumerator LoadLevelCoroutine(int levelNumber)
        {
            LastLevel = levelNumber - 1;

            confirmSFX.Play();

            levelPreviews[levelNumber - 1].ZoomIn();

            yield return new WaitForSecondsRealtime(0.5f);

            int buildIndex = levelData.GetBuildIndex(levelNumber);

            sceneLoader.LoadScene(buildIndex);
        }

        IEnumerator ExecuteDelayed(UnityAction action, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            action?.Invoke();
        }
    }
}