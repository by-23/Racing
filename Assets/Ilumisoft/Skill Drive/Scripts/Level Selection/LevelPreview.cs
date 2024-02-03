using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.LevelSelection
{
    public class LevelPreview : MonoBehaviour
    {
        [SerializeField]
        GameObject virtualCamera;

        [SerializeField]
        GameObject zoomInCam;

        [SerializeField]
        Image lockImage;

        [SerializeField]
        GameObject completedText;

        public void Select()
        {
            virtualCamera.gameObject.SetActive(true);
        }

        public void Deselect()
        {
            virtualCamera.gameObject.SetActive(false);
        }

        public void ShowLock(bool show)
        {
            lockImage.gameObject.SetActive(show);
        }

        public void ShowCompletedText(bool show)
        {
            completedText.SetActive(show);
        }

        public void ZoomIn()
        {
            zoomInCam.SetActive(true);
        }
    }
}