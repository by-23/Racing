using UnityEngine;

namespace Ilumisoft.SkillDrive.Game
{
    public class DisableOnRetry : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.IsRetry)
            {
                gameObject.SetActive(false);
            }
        }
    }
}