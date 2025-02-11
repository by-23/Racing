using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using AYellowpaper.SerializedCollections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    public class MainMenu : UIPanel
    {
        [SerializeField] CanvasGroup canvasGroup = null;

        [SerializeField] Selectable selectable = null;

        [SerializeField] public Button GoButton;

        [SerializeField] public Button CreateLobbyButton;

        [SerializeField] TextMeshProUGUI NewLobbyName;

        [SerializeField] private new CameraFollow camera;

        [SerializeField] private TextMeshProUGUI countdownText; // UI-текст для отображения отчета

        [SerializeField] private TextMeshProUGUI botCountText;

        [SerializeField] private Slider botCountSlider;


        private void Start()
        {
            GoButton.onClick.AddListener(OnOfflineStartClicked);
            CreateLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
            botCountSlider.onValueChanged.AddListener(OnBotCountSliderChanged);
        }

        private void OnBotCountSliderChanged(float arg0)
        {
            botCountText.text = arg0.ToString();
            RoomManager.Instance.ChancheBotCount((int)arg0);
        }

        private void OnOfflineStartClicked()
        {
            RoomManager.Instance.StartGame();
            StartCoroutine(CountdownRoutine());
            countdownText.gameObject.SetActive(true);
        }

        protected internal void OnOnlineStartClicked()
        {
            StartCoroutine(CountdownRoutine());
            countdownText.gameObject.SetActive(true);
        }

        IEnumerator CountdownRoutine()
        {
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            countdownText.text = "START";
            yield return new WaitForSeconds(1f);
            countdownText.gameObject.SetActive(false);
            GameController.Instance.isGameStarted = true;
        }


        private void OnCreateLobbyButtonClicked()
        {
            if (NewLobbyName.text.Length > 4)
            {
                RoomManager.Instance.CreateRoom(NewLobbyName.text);
                RoomManager.Instance.isOnline = true;
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
    }
}