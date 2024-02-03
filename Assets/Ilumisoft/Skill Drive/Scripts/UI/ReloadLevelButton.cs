using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [AddComponentMenu("Ilumisoft/UI/Reload Level Button")]
    [RequireComponent(typeof(Button))]
    public class ReloadLevelButton : MonoBehaviour
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
            if (sceneLoader != null)
            {
                GameManager.IsRetry = true;
                sceneLoader.LoadScene(gameObject.scene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }
    }
}