using Ilumisoft.SkillDrive.Game;
using UnityEngine;

namespace Ilumisoft.SkillDrive.UI
{
    public class LevelNumberDisplay : MonoBehaviour
    {
        [SerializeField]
        LevelData levelData = null;

        [SerializeField]
        TMPro.TextMeshProUGUI text = null;


        void Start()
        {
            int levelNumber = levelData.GetLevelNumber(gameObject.scene);

            text.text = $"Level {levelNumber}";
        }
    }
}