using UnityEngine;
using UnityEngine.Audio;

namespace Ilumisoft.SkillDrive.Audio
{
    public class MusicManager : MonoBehaviour
    {
        [SerializeField]
        AudioMixerGroup audioMixerGroup;

        [SerializeField]
        AudioClip backgroundMusic = null;

        [SerializeField]
        bool playOnAwake = true;

        MusicPlayer musicPlayer = null;

        private void Awake()
        {
            musicPlayer = FindOrCreateMusicPlayer();
        }

        /// <summary>
        /// Returns an existing instance of Music Player or creates one (persistent)
        /// </summary>
        /// <returns></returns>
        MusicPlayer FindOrCreateMusicPlayer()
        {
            musicPlayer = FindObjectOfType<MusicPlayer>();

            if (musicPlayer == null)
            {
                musicPlayer = new GameObject("Music Player").AddComponent<MusicPlayer>();

                DontDestroyOnLoad(musicPlayer);
            }

            return musicPlayer;
        }

        void Start()
        {
            var audioSource = musicPlayer.AudioSource;

            if(audioSource.isPlaying == false || audioSource.clip != backgroundMusic)
            {
                if (playOnAwake)
                {
                    audioSource.clip = backgroundMusic;
                    audioSource.outputAudioMixerGroup = audioMixerGroup;
                    audioSource.Play();
                }
            }
        }
    }
}