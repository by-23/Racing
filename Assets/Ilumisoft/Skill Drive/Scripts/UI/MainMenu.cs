using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WebSocketSharp;

namespace Ilumisoft.SkillDrive.UI
{
    public class MainMenu : UIPanel
    {
        [SerializeField] CanvasGroup canvasGroup = null;

        [SerializeField] Selectable selectable = null;

        [SerializeField] public Button playButton;

        [SerializeField] public Button CreateLobbyButton;

        [SerializeField] TextMeshProUGUI NewLobbyName;

        [SerializeField] GameObject zoomInCam = null;

        [SerializeField] GameObject playCam = null;

        [SerializeField] AudioSource confirmAudioSource;


        private IEnumerator Start()
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            CreateLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);

            yield return null;

            zoomInCam.SetActive(true);
        }

        private void OnPlayButtonClicked()
        {
            StartCoroutine(LoadCoroutine());
            RoomManager.Instance.StartGame();
        }

        private void OnCreateLobbyButtonClicked()
        {
            if (NewLobbyName.text.Length > 4)
            {
                RoomManager.Instance.CreateRoom(NewLobbyName.text);
                RoomManager.Instance.isOnline = true;
                StartCoroutine(LoadCoroutine());
            }
            else
            {
                print("Please enter a name for the lobby");
            }
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
            StopAllCoroutines();
            playCam.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            confirmAudioSource.Play();
        }
    }
}