using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    public class VehicleAI : MonoBehaviour
    {
        [SerializeField] private Vehicle vehicle;
        [SerializeField] private float steeringSensitivity = 1f;
        [SerializeField] private float accelerationFactor = 1f;
        [SerializeField] private float obstacleDetectionDistance = 5f;
        [SerializeField] private float avoidanceStrength = 1f;

        private GameController gameController;

        private void Start()
        {
            gameController = GameController.Instance;
            vehicle = GetComponent<Vehicle>();
        }

        private void Update()
        {
            if (vehicle.CanMove && gameController.checkpoints.Count > 0)
            {
                if (DetectObstacle(out Vector3 avoidanceDirection))
                {
                    AvoidObstacle(avoidanceDirection);
                }
                else
                {
                    DriveTowardsCheckpoint();
                }
            }
        }

        private bool DetectObstacle(out Vector3 avoidanceDirection)
        {
            avoidanceDirection = Vector3.zero;
            RaycastHit hit;
            Vector3 forward = transform.forward;
            
            if (Physics.Raycast(transform.position, forward, out hit, obstacleDetectionDistance))
            {
                if (hit.collider.CompareTag("Obstacle"))
                {
                    avoidanceDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;
                    return true;
                }
            }
            return false;
        }

        private void AvoidObstacle(Vector3 avoidanceDirection)
        {
            float angleToAvoidance = Vector3.SignedAngle(transform.forward, avoidanceDirection, Vector3.up);
            float steeringPower = angleToAvoidance * avoidanceStrength * vehicle.FinalStats.SteeringPower;
            float rotationTorque = steeringPower - vehicle.Rigidbody.angularVelocity.y;
            
            vehicle.Rigidbody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.Acceleration);
            vehicle.ApplyAcceleration(accelerationFactor * 0.5f); // Замедляемся при объезде
        }

        private void DriveTowardsCheckpoint()
        {
            Transform targetCheckpoint = gameController.checkpoints[gameController.currentCheckpointIndex].transform;
            Vector3 directionToCheckpoint = targetCheckpoint.position - transform.position;
            directionToCheckpoint.y = 0;

            float angleToCheckpoint = Vector3.SignedAngle(transform.forward, directionToCheckpoint.normalized, Vector3.up);
            float steeringPower = angleToCheckpoint * steeringSensitivity * vehicle.FinalStats.SteeringPower;
            float rotationTorque = steeringPower - vehicle.Rigidbody.angularVelocity.y;

            vehicle.Rigidbody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.Acceleration);
            vehicle.ApplyAcceleration(accelerationFactor);
        }
    }
}
