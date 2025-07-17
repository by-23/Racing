using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
public class UI : Singleton<UI>
{
    private PhotonView photonView;

    [SerializeField]
    internal Dictionary<int, LapInfoUI> instantiatedPlayerInfoUIs = new Dictionary<int, LapInfoUI>();

    [FormerlySerializedAs("playerInfoUIPrefab")]
    [SerializeField] private LapInfoUI lapInfoUIPrefab;
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
        var instantiatedPlayerInfoUI = Instantiate(lapInfoUIPrefab, playerInfoListUI.transform);
        instantiatedPlayerInfoUIs.Add(actorNumber, instantiatedPlayerInfoUI);
    }

    internal void ControlsUIVisibility(bool isActive)
    {
        foreach (var element in ControlsUI)
        {
            element.SetActive(true);
        }
    }
}