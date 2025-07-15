using UnityEngine;

public interface IAttacking
{
    public Item Item { get; }
    public void TryAttack();
}