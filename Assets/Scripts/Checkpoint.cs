using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;
    public GameController gameController;
    internal bool isActivated;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView photonView = other.GetComponentInParent<PhotonView>();
            if (photonView != null && !isActivated)
            {
                // Проверка на соответствие текущего индекса контрольной точки
                if (gameController.currentCheckpointIndex == checkpointIndex)
                {
                    isActivated = true;
                    gameController.PlayerPassedCheckpoint(photonView.Owner.ActorNumber, checkpointIndex);
                }
            }
        }
    }
}