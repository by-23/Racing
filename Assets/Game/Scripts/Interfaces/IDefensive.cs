using UnityEngine;

namespace Game.Scripts.Interfaces
{
    public interface IDefensive
    {
        public void Defense(IAttacking attacker);
    }
}