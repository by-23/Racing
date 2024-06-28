using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

[RequireComponent(typeof(UnityTransport))]
public class RelayManager : MonoBehaviour
{

    public static RelayManager singleton;

    public int maxPlayerCount = 12;

    private UnityTransport transport;

    private void Awake()
    {
        RelayManager.singleton = this;
        transport = GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("Unity transport missing");
        }
    }

    public async Task<string> CreateGame()
    {
        Allocation a = await RelayService.Instance.CreateAllocationAsync(maxPlayerCount);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
        Debug.Log("Join Code: " + joinCode);
        transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
        NetworkManager.Singleton.StartHost();
        print("Host started");
        return joinCode;
    }

    public async Task<string> CreateDedicatedServer()
    {
        Allocation a = await RelayService.Instance.CreateAllocationAsync(maxPlayerCount);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
        Debug.Log("Join Code: " + joinCode);
        transport.SetRelayServerData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
        NetworkManager.Singleton.StartServer();
        return joinCode;
    }

    public async Task JoinGame(string joinCode)
    {
        if (joinCode == "")
        {
            Debug.LogError("Join code rempty, cannot join game");
            return;
        }
        JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(joinCode);
        transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
        NetworkManager.Singleton.StartClient();
    }
}
