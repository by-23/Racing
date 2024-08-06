using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;
    public GameController gameController;
    private Dictionary<int, bool> playerCheckpointStatus = new Dictionary<int, bool>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView photonView = other.GetComponentInParent<PhotonView>();
            if (photonView != null)
            {
                int playerId = photonView.Owner.ActorNumber;

                if (!playerCheckpointStatus.ContainsKey(playerId) || !playerCheckpointStatus[playerId])
                {
                    playerCheckpointStatus[playerId] = true;
                    gameController.PlayerPassedCheckpoint(playerId, checkpointIndex);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView photonView = other.GetComponentInParent<PhotonView>();
            if (photonView != null)
            {
                int playerId = photonView.Owner.ActorNumber;
                playerCheckpointStatus[playerId] = false;
            }
        }
    }
}