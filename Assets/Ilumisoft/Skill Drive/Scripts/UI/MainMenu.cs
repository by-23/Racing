using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using AYellowpaper.SerializedCollections;
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

        [SerializeField] private new CameraFollow camera;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            CreateLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
        }

        private void OnPlayButtonClicked()
        {
            RoomManager.Instance.StartGame();
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