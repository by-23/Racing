using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;
    public GameController gameController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView photonView = other.GetComponentInParent<PhotonView>();
            if (photonView != null)
            {
                gameController.PlayerPassedCheckpoint(checkpointIndex);
            }
        }
    }
}