using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [AddComponentMenu("Ilumisoft/UI/Auto Select")]
    [RequireComponent(typeof(Selectable))]
    public class AutoSelect : MonoBehaviour
    {
        Selectable selectable;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        void Start()
        {
            if (selectable != null)
            {
                selectable.Select();
            }
        }
    }
}