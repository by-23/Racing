using System;
using UnityEngine;

public class Item : MonoBehaviour
{
    internal Vehicle Owner{get; private set;}
    public event Action OnInit ;

    internal void Init(Vehicle owner)
    {
        Owner = owner;
        OnInit?.Invoke();
    }
}