using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    [RequireComponent(typeof(CollectableManager), typeof(AudioSource))]
    public class GameManager : MonoBehaviour
    {
        public static bool IsRetry = false;

        [SerializeField]
        LevelData levelData = null;

        [SerializeField, Header("Game")]
        Countdown countdown = null;

        [SerializeField, Tooltip("Delay before the countdown starts")]
        float startDelay = 0.0f;

        [SerializeField, Tooltip("Time limit of the countdown")]
        float timeLimit = 10.0f;

        [SerializeField, Header("SFX")]
        AudioClip successSFX = null;

        [SerializeField]
        AudioClip failSFX = null;


        [SerializeField]
        UnityEvent onComplete = null;

        [SerializeField]
        UnityEvent onFail = null;

        AudioSource audioSource;

        CollectableManager collectableManager = null;

        // Reference to the player
        GameObject player;

        bool isGameOver = false;

        public UnityEvent OnComplete { get => this.onComplete; set => this.onComplete = value; }
        public UnityEvent OnFail { get => this.onFail; set => this.onFail = value; }

        private void Awake()
        {
            player = GameObject.FindWithTag("Player");

            collectableManager = GetComponent<CollectableManager>();
            audioSource = GetComponent<AudioSource>();

            collectableManager.OnAllCollected += OnAllCollected;
            countdown.OnTimeOut += OnTimeOut;
        }

        private IEnumerator Start()
        {
            countdown.SetTimeLimit(timeLimit);

            yield return new WaitForSeconds(startDelay);

            countdown.StartCountdown();

            isGameOver = false;
        }

        /// <summary>
        /// Callback invoked when the player collected all cubes
        /// </summary>
        private void OnAllCollected()
        {
            EndLevel(won: true);
        }

        /// <summary>
        /// Callback invoked when the player run out of time
        /// </summary>
        private void OnTimeOut()
        {
            EndLevel(won: false);
        }

        /// <summary>
        /// Ends the current level
        /// </summary>
        /// <param name="won"></param>
        public void EndLevel(bool won)
        {
            if (!isGameOver)
            {
                // Make sure this is only called once
                isGameOver = true;

                var audioClip = won ? successSFX : failSFX;

                if (audioSource != null && audioClip != null)
                {
                    audioSource.PlayOneShot(audioClip);
                }

                var vehicle = player.GetComponent<Vehicle>();

                vehicle.CanMove = false;
                vehicle.Rigidbody.drag = 2.0f;

                if(won)
                {
                    UnlockNextLevel();

                    OnComplete?.Invoke(); 
                }
                else
                {
                    OnFail?.Invoke();
                }
            }
        }

        void UnlockNextLevel()
        {
            // Get the level number of the current scene
            int currentLevelNumber = levelData.GetLevelNumber(gameObject.scene);

            // Unlock the next level
            LevelLockManager.UnlockLevel(currentLevelNumber + 1);
        }
    }
}