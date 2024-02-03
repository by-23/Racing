using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [RequireComponent(typeof(Image))]
    public class BlurEffect : MonoBehaviour
    {
        Image image;

        [SerializeField]
        float strength = 5;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void Start()
        {
            var materialInstance = Instantiate(image.material);

            image.material = materialInstance;
        }

        public void FadeIn(float duration)
        {
            StartCoroutine(FadeInCoroutine(duration));
        }

        public void FadeOut(float duration)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }

        IEnumerator FadeInCoroutine(float duration)
        {
            float elapsed = 0.0f;

            ApplyStrength(0);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float value = Mathf.Lerp(0, strength, elapsed / duration);

                ApplyStrength(value);

                yield return null;
            }

            yield return null;
        }

        IEnumerator FadeOutCoroutine(float duration)
        {
            float elapsed = 0.0f;

            ApplyStrength(strength);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float value = Mathf.Lerp(strength, 0, elapsed / duration);

                ApplyStrength(value);

                yield return null;
            }

            yield return null;
        }

        void ApplyStrength(float value)
        {
            image.material.SetFloat("_Radius", value);
        }
    }
}