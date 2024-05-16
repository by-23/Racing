using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCallBack : MonoBehaviour
{
    public event Action<Collider> OnTriggerEntered;

    public event Action<Collider> OnTriggerExited;

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEntered?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerExited?.Invoke(other);
    }
}