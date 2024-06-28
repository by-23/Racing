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
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Произошла ошибка: {ex.Message}");
        }
    }

    private async void StartHost()
    {
        try
        {
            lobby = await LobbyManager.singleton.CreateLobby(HostLobbyText.text);
            if (lobby != null)
            {
                HostLobbyText.text = lobby.LobbyCode;
            }
            else
            {
                Debug.LogError("Ошибка при создании лобби");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при создании лобби: {ex.Message}");
        }
    }

    public void SetLobby(Lobby newLobby)
    {
        if (newLobby == null)
        {
            Debug.LogError("Попытка установить лобби с null значением");
            return;
        }
        lobby = newLobby;
        HostLobbyText.text = lobby.LobbyCode;
    }

    private void StartClient()
    {
        if (string.IsNullOrEmpty(ClientLobbyText.text))
        {
            Debug.LogError("Попытка присоединиться к лобби без указания кода лобби");
            return;
        }
        LobbyManager.singleton.JoinLobby(ClientLobbyText.text);
    }
}