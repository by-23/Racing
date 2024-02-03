using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class CollectableManager : MonoBehaviour
    {
        public UnityAction OnAllCollected;

        Collectable[] collectables;

        int amount = 0;
        int collected = 0;

        private void Awake()
        {
            collectables = FindObjectsOfType<Collectable>();

            if (collectables != null)
            {
                amount = collectables.Length;

                foreach (var collectable in collectables)
                {
                    collectable.OnCollect += OnCollect;
                }
            }
        }

        private void OnCollect()
        {
            collected++;

            if (collected == amount)
            {
                OnAllCollected?.Invoke();
            }
        }
    }
}