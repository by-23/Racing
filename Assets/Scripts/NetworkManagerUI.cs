using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class NetworkManagerUI : Singleton<NetworkManagerUI>
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField HostLobbyText;
    [SerializeField] private TMP_InputField ClientLobbyText;
    private Lobby lobby;

    private async void Awake()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void StartHost()
    {
       var taskLobby = LobbyManager.singleton.CreateLobby(HostLobbyText.text);
    }

    public void SetLobby(Lobby newLobby)
    {
        lobby = newLobby;
        HostLobbyText.text = lobby.LobbyCode;
    }

    private void StartClient()
    {
        LobbyManager.singleton.JoinLobby(ClientLobbyText.text);
    }
}