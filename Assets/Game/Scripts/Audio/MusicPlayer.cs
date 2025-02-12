using UnityEngine;

namespace Ilumisoft.SkillDrive.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        AudioSource audioSource;

        public AudioSource AudioSource { get => this.audioSource; set => this.audioSource = value; }

        private void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }
    }
}