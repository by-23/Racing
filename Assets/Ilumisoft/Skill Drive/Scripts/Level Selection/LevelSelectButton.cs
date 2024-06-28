using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.LevelSelection
{
    public class LevelSelectButton : MonoBehaviour, ISelectHandler
    {
        [SerializeField] TMPro.TextMeshProUGUI text;

        LevelSelectionManager levelSelectManager;

        int levelNumber = 0;

        Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            this.levelSelectManager.LoadLevel(levelNumber);
        }

        public void Initialize(LevelSelectionManager levelSelectManager, int levelNumber)
        {
            this.levelSelectManager = levelSelectManager;
            this.levelNumber = levelNumber;
            text.text = $"LEVEL {levelNumber}";
        }

        public void OnSelect(BaseEventData eventData)
        {
            levelSelectManager.GoToLevel(levelNumber);
        }
    }
}