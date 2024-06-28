using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    public class MainMenu : UIPanel
    {
        [SerializeField] CanvasGroup canvasGroup = null;

        [SerializeField] Selectable selectable = null;

        SceneLoader sceneLoader;

        [SerializeField] Button playButton;

        [SerializeField] Button multiplayerPlayButton;

        [SerializeField] GameObject zoomInCam = null;

        [SerializeField] GameObject playCam = null;

        [SerializeField] AudioSource confirmAudioSource;


        private IEnumerator Start()
        {
            LevelSelectionManager.LastLevel = -1;

            playButton.onClick.AddListener(OnPlayButtonClick);
            multiplayerPlayButton.onClick.AddListener(OnMultiplayerPlayButtonClick);

            sceneLoader = FindObjectOfType<SceneLoader>();

            yield return null;

            zoomInCam.SetActive(true);
        }

        private void OnPlayButtonClick()
        {
            StopAllCoroutines();
            StartCoroutine(LoadCoroutine());
        }

        private void OnMultiplayerPlayButtonClick()
        {
           
        }

        public override void Show()
        {
            canvasGroup.interactable = true;

            if (selectable != null)
            {
                selectable.Select();
            }
        }

        public override void Hide()
        {
            canvasGroup.interactable = false;
        }

        IEnumerator LoadCoroutine()
        {
            playCam.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            confirmAudioSource.Play();

            yield return new WaitForSecondsRealtime(1.25f);

            sceneLoader.LoadScene(1);
        }
    }
}