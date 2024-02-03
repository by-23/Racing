using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ilumisoft.SkillDrive.Game
{
    public class InputActionEvent : MonoBehaviour
    {
        public InputAction inputAction;

        public UnityEvent OnPerformed = new UnityEvent();

        private void Awake()
        {
            inputAction.started += (arg) =>
            {
                OnPerformed.Invoke();
            };
        }

        private void OnEnable()
        {
            StartCoroutine(InvokeNextFrame(() =>
            {
                inputAction.Enable();
            }));
        }

        private void OnDisable()
        {
            inputAction.Disable();
        }

        IEnumerator InvokeNextFrame(UnityAction action)
        {
            yield return new WaitForSeconds(1);

            action?.Invoke();
        }
    }
}