using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Item : MonoBehaviour
{
    [SerializeField] private float beingDeactivatedDuration = 5f;

    internal Vehicle Owner { get; private set; }
    public event Action OnInit;
    public event Action OnDeactivate;
    public event Action OnReactivate;


    internal void Init(Vehicle owner)
    {
        Owner = owner;
        OnInit?.Invoke();
    }

    internal async Awaitable Deactivate()
    {
        OnDeactivate?.Invoke();
        await Awaitable.WaitForSecondsAsync(beingDeactivatedDuration);
        OnReactivate?.Invoke();
    }
}