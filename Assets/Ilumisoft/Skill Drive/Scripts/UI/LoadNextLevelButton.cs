using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [AddComponentMenu("Ilumisoft/UI/Load Next Level Button")]
    [RequireComponent(typeof(Button))]
    public class LoadNextLevelButton : MonoBehaviour
    {
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
            int index = gameObject.scene.buildIndex + 1;

            if (sceneLoader != null)
            {
                GameManager.IsRetry = false;
                sceneLoader.LoadScene(1);
            }
            else
            {
                SceneManager.LoadScene(index);
            }
        }
    }
}