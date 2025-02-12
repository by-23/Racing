using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemButton : MonoBehaviour
{
    private new TextMeshProUGUI name;
    private TextMeshProUGUI playerCount;
    [SerializeField] private Button button;

    private void Start()
    {
        button.onClick.AddListener(OnButtonPressed);
    }

    public void OnButtonPressed()
    {
        RoomManager.Instance.JoinRoomByName(name.text);
    }

    public void SetRoom(string roomName, int playerCount)
    {
        name.text = roomName;
        this.playerCount.text = playerCount.ToString();
    }
}