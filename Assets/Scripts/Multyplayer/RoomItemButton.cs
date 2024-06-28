using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemButton : MonoBehaviour
{
    public TextMeshProUGUI name;
    public TextMeshProUGUI playerCount;
    [SerializeField] private Button button;

    private void Start()
    {
        button.onClick.AddListener(OnButtonPressed);
    }

    public void OnButtonPressed()
    {
        RoomManager.Instance.JoinRoomByName(name.text);
    }
}