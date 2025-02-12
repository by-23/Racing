using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] protected Button goButton;

    [SerializeField] protected Button multiplayerButton;

    [SerializeField] protected Button createLobbyButton;

    [SerializeField] protected Button backFromRoomButton;

    [SerializeField] protected Button readyButton;

    [SerializeField] TextMeshProUGUI newLobbyName;

    [SerializeField] private TextMeshProUGUI countdownText;

    [SerializeField] private TextMeshProUGUI botCountText;

    [SerializeField] private Slider botCountSlider;

    [SerializeField] protected UiPlayerElement[] uiPlayerElements;

    [SerializeField] private PhotonView photonView;

    internal Dictionary<int, UiPlayerElement> playerUIMap = new Dictionary<int, UiPlayerElement>();


    private void Start()
    {
        goButton.onClick.AddListener(OnOfflineStartClicked);
        createLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
        botCountSlider.onValueChanged.AddListener(OnBotCountSliderChanged);
        multiplayerButton.onClick.AddListener(OnMultiplayerButtonClicked);
        backFromRoomButton.onClick.AddListener(OnQuitFromRoom);
        readyButton.onClick.AddListener(OnReadyButtonClicked);
    }

    protected internal void OnMultiplayerButtonClicked()
    {
        RoomManager.Instance.direktor.MoveCamera("Multiplayer");
        RoomManager.Instance.ChancheBotCount(0);
    }

    protected internal void OnQuitFromRoom()
    {
        RoomManager.Instance.direktor.MoveCamera("Multiplayer");
        RoomList.Instance.UpdaterUI();
        PhotonNetwork.LeaveRoom();
    }

    public void AddPlayerUI(Player player)
    {
        int actorNumber = player.ActorNumber;
        string nickName = player.NickName;

        photonView.RPC("RPC_AddPlayerUI", RpcTarget.AllBuffered, actorNumber, nickName);
    }

    [PunRPC]
    public void RPC_AddPlayerUI(int actorNumber, string nickName)
    {
        if (playerUIMap.ContainsKey(actorNumber))
            return;

        UiPlayerElement freeElement = null;
        foreach (var element in uiPlayerElements)
        {
            if (element.isEmpty)
            {
                freeElement = element;
                break;
            }
        }

        if (freeElement != null)
        {
            freeElement.gameObject.SetActive(true);
            freeElement.playerName.text = nickName;
            playerUIMap.Add(actorNumber, freeElement);
            freeElement.isEmpty = false;
        }
        else
        {
            Debug.LogWarning("Нет свободного UI-элемента для нового игрока");
        }
    }

    public void RemovePlayerUI(Player player)
    {
        int actorNumber = player.ActorNumber;
        if (playerUIMap.TryGetValue(actorNumber, out var element))
        {
            element.gameObject.SetActive(false);
            playerUIMap.Remove(actorNumber);
        }
    }

    [PunRPC]
    public void RPC_SetPlayerReady(int actorNumber, bool isReady)
    {
        if (playerUIMap.TryGetValue(actorNumber, out UiPlayerElement playerElement))
        {
            bool newReadyState = !playerUIMap[actorNumber].isReady;
            playerElement.readyText.color = newReadyState ? Color.green : Color.gray;
            playerElement.isReady = isReady;
            CheckAllPlayersReady();
        }
    }

    public void OnReadyButtonClicked()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        bool newReadyState = !playerUIMap[actorNumber].isReady;
        photonView.RPC("RPC_SetPlayerReady", RpcTarget.AllBuffered, actorNumber, newReadyState);
    }

    private void CheckAllPlayersReady()
    {
        foreach (var playerElement in playerUIMap.Values)
        {
            if (!playerElement.isReady)
                return;
        }

        Debug.Log("All players are ready!");
        OnAllPlayersReady();
    }


    private void OnBotCountSliderChanged(float arg0)
    {
        botCountText.text = arg0.ToString();
        RoomManager.Instance.ChancheBotCount((int)arg0);
    }

    private void OnOfflineStartClicked()
    {
        RoomManager.Instance.GetReadyToStartGame();
        StartCoroutine(CountdownRoutine());
        countdownText.gameObject.SetActive(true);
    }

    protected internal void OnAllPlayersReady()
    {
        RoomManager.Instance.StartGame();
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
        if (newLobbyName.text.Length > 4)
        {
            RoomManager.Instance.CreateRoom(newLobbyName.text);
            RoomManager.Instance.isOnline = true;
            RoomManager.Instance.direktor.MoveCamera("Lobby");
        }
        else
        {
            print("Please enter a name for the lobby");
        }
    }
}