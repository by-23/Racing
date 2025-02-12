using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UiPlayerElement : MonoBehaviour
{
    [SerializeField] protected internal bool isEmpty;
    [SerializeField] protected internal bool isReady = false;
    [SerializeField] protected internal TextMeshProUGUI playerName;
    [SerializeField] protected internal TextMeshProUGUI readyText;

    public void SetReadyState(bool isReady)
    {
        readyText.color = isReady ? Color.green : Color.gray;
    }
}