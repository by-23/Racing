using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [AddComponentMenu("Ilumisoft/UI/Load Level Button")]
    [RequireComponent(typeof(Button))]
    public class LoadLevelButton : MonoBehaviour
    {
        [SerializeField]
        int index = 1;

        Button button = null;

        SceneLoader sceneLoader;

        private void Start()
        {
            sceneLoader = FindObjectOfType<SceneLoader>();

            button = GetComponent<Button>();

            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (sceneLoader != null)
            {
                GameManager.IsRetry = false;
                sceneLoader.LoadScene(index);
            }
            else
            {
                SceneManager.LoadScene(index);
            }
        }
    }
}