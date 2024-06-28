using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    public class MainMenu : UIPanel
    {
        [SerializeField] CanvasGroup canvasGroup = null;

        [SerializeField] Selectable selectable = null;

        [SerializeField] public Button playButton;

        [SerializeField] Button multiplayerPlayButton;

        [SerializeField] GameObject zoomInCam = null;

        [SerializeField] GameObject playCam = null;

        [SerializeField] AudioSource confirmAudioSource;


        private IEnumerator Start()
        {
            LevelSelectionManager.LastLevel = -1;

            playButton.onClick.AddListener(OnPlayButtonClicked);
            multiplayerPlayButton.onClick.AddListener(OnMultiplayerPlayButtonClicked);

            yield return null;

            zoomInCam.SetActive(true);
        }

        private void OnPlayButtonClicked()
        {
            StopAllCoroutines();
            StartCoroutine(LoadCoroutine());
        }

        private void OnMultiplayerPlayButtonClicked()
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
        }
    }
}