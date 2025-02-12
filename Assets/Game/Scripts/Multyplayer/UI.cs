using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class UI : Singleton<UI>
{
    private PhotonView photonView;

    [SerializeField]
    internal Dictionary<int, PlayerInfoUI> instantiatedPlayerInfoUIs = new Dictionary<int, PlayerInfoUI>();

    [SerializeField] private PlayerInfoUI playerInfoUIPrefab;
    [SerializeField] private GameObject playerInfoListUI;
    [SerializeField] GameObject[] ControlsUI;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    internal void UpdateUI(int actorNumber, string name)
    {
        photonView.RPC("UpdatePlayerNameUI", RpcTarget.AllBuffered, actorNumber, name);
    }

    [PunRPC]
    private void UpdatePlayerNameUI(int actorNumber, string name)
    {
        if (instantiatedPlayerInfoUIs.TryGetValue(actorNumber, out var playerInfoUI))
        {
            playerInfoUI.playerName.text = name;
        }
        else
        {
            var instantiatedPlayerInfoUI = Instantiate(playerInfoUIPrefab, playerInfoListUI.transform);
            instantiatedPlayerInfoUIs.Add(actorNumber, instantiatedPlayerInfoUI);
            instantiatedPlayerInfoUI.playerName.text = name;
        }
    }

    internal void ControlsUIVisibility(bool isActive)
    {
        foreach (var element in ControlsUI)
        {
            element.SetActive(true);
        }
    }
}