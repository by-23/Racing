using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Ilumisoft.SkillDrive
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] PlayableDirector loadedplayableDirector = null;

        [SerializeField] PlayableDirector playableDirector = null;

        private void Start()
        {
            loadedplayableDirector.Play();
        }

        public void LoadScene(int index)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneCoroutine2(index));
        }

        public void LoadScene(string name)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneCoroutine2(name));
        }

        IEnumerator LoadSceneCoroutine2(string name)
        {
            yield return PlayTimelineAndWait();

            SceneManager.LoadScene(name);
        }

        IEnumerator LoadSceneCoroutine2(int index)
        {
            yield return PlayTimelineAndWait();

            SceneManager.LoadScene(index);
            if (GameController.Instance.isMultiplayer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetSceneByBuildIndex(index).name,
                    LoadSceneMode.Single);
            }
        }

        IEnumerator PlayTimelineAndWait()
        {
            if (playableDirector != null)
            {
                playableDirector.gameObject.SetActive(true);
                playableDirector.Stop();
                playableDirector.time = 0;
                playableDirector.Evaluate();
                playableDirector.Play();

                yield return new WaitForSecondsRealtime((float)playableDirector.duration);
            }
        }
    }
}