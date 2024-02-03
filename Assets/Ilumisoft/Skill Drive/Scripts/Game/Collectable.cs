using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        public UnityAction OnCollect = null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (particlesPrefab != null)
                {
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
                }

                OnCollect?.Invoke();

                gameObject.SetActive(false);
            }
        }
    }
}