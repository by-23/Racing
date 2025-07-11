using System;
using Game.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

public class Shield : MonoBehaviour, IDefensive
{
    private enum DeactivationType
    {
        Destroy,
        Deactivate
    }

    [SerializeField] private float shieldDuration = 10f;
    [SerializeField] private DeactivationType shieldDeactivationType;
    [SerializeField] private Item _item;

    private void OnValidate()
    {
        Init();
    }

    private void Start()
    {
        Init();
        ShieldTimer();
        _item.OnDeactivate += Deactivate;
        _item.OnReactivate += Reactivate;
    }

    private void Init()
    {
        if (!_item) _item = GetComponent<Item>();
    }

    private async Awaitable ShieldTimer()
    {
        await Awaitable.WaitForSecondsAsync(shieldDuration);
        Destroy(gameObject);
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IAttacking attacker) && attacker.Item.Owner != _item.Owner)
        {
            Defense(attacker);
        }
    }

    public void Defense(IAttacking attacker)
    {
        switch (shieldDeactivationType)
        {
            case DeactivationType.Deactivate:
                attacker.Item.Deactivate();
                break;
            case DeactivationType.Destroy:
                Destroy(attacker.Item.gameObject);
                break;
        }
    }

    private void Deactivate()
    {
        enabled = false;
    }

    private void Reactivate()
    {
        enabled = true;
    }

    private void OnDestroy()
    {
        _item.OnDeactivate -= Deactivate;
        _item.OnReactivate -= Reactivate;
    }
}