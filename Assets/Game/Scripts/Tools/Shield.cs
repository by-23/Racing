using Game.Scripts.Interfaces;
using UnityEngine;

public class Shield : MonoBehaviour, IDefensive, IItemEffect
{
    [SerializeField] private float shieldDuration = 10f;
    [SerializeField] private Item _item;

    private void OnValidate()
    {
        Init();
    }

    private void Awake()
    {
        Init();
        ShieldTimer();
    }

    private void Init()
    {
        if (!_item) _item = GetComponent<Item>();
    }

    public void Activate(Vehicle owner)
    {
        transform.SetParent(owner.transform);
        transform.position = owner.transform.position;
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
        attacker.Item.Deactivate();
    }

    public void Deactivate()
    {
        enabled = false;
    }

    public void Reactivate()
    {
        enabled = true;
    }
}