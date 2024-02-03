using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager singleton;
    private string playerId;
    private UnityTransport transport;
    public const string joinCodeKey = "jc";
    public const string sceneNameKey = "scnm";
    public const string hostNameKey = "hname";

    private Lobby curLobby;

    private void Awake()
    {
        singleton = this;
        transport = FindObjectOfType<UnityTransport>();
    }

    private void Start()
    {
        Authenticate();
    }

    private async Task Authenticate()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
            //Needs to be Disabled when you build
            /*
                #if UNITY_EDITOR
                            //Used to differentiate clients when using ParrelSync
                            options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
             #endif
           */
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        playerId = AuthenticationService.Instance.PlayerId;
    }

    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public async Task<List<Lobby>> GatherLobbies()
    {
        var options = new QueryLobbiesOptions { Count = 15, };
        var allLobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
        return allLobbies.Results;
    }

    public async Task<Lobby> CreateLobby(string hostName)
    {
        try
        {
            string joinCode = await RelayManager.singleton.CreateGame();
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { joinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    {
                        sceneNameKey,
                        new DataObject(DataObject.VisibilityOptions.Public, SceneManager.GetActiveScene().name)
                    },
                    { hostNameKey, new DataObject(DataObject.VisibilityOptions.Public, hostName) }
                }
            };
            var lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby Name",
                RelayManager.singleton.maxPlayerCount, options);
            curLobby = lobby;
            NetworkManagerUI.Instance.SetLobby(lobby);
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
            return lobby;
        }
        catch (System.Exception e)
        {
            Debug.Log("Failed to create lobby");
            Debug.Log(e);
            throw;
        }
    }


    public async Task JoinLobby(string lobbyId)
    {
        try
        {
            curLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyId.ToUpperInvariant());
            await RelayManager.singleton.JoinGame(curLobby.Data[LobbyManager.joinCodeKey].Value);
        }
        catch (System.Exception e)
        {
            Debug.Log("Failed to join lobby");
            Debug.Log(e);
            throw;
        }
    }

    //Returns to menu
    private void ReturnToMenu()
    {
        Destroy(NetworkManager.Singleton.gameObject);
        // SceneManager.LoadScene(0);
    }

    public Lobby GetCurLobby()
    {
        return curLobby;
    }

    public string GetCurPlayerId()
    {
        return playerId;
    }
}