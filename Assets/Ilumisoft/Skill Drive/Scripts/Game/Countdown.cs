using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class Countdown : MonoBehaviour
    {
        [SerializeField]
        TMPro.TextMeshProUGUI text = null;

        public UnityAction OnTimeOut = null;

        bool isRunning = false;

        float timeLeft = 0.0f;

        public void SetTimeLimit(float timeLimit)
        {
            this.timeLeft = timeLimit;

            SetText(timeLimit);
        }

        public void StartCountdown()
        {
            isRunning = true;
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        void Update()
        {
            if (isRunning)
            {
                timeLeft -= Time.deltaTime;

                timeLeft = Mathf.Max(0.0f, timeLeft);

                SetText(timeLeft);

                if(timeLeft <= 0.0f)
                {
                    isRunning = false;

                    OnTimeOut?.Invoke();
                }
            }
        }

        void SetText(float time)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);

            text.text = timeSpan.ToString("mm\\:ss\\:ff");
        }
    }
}