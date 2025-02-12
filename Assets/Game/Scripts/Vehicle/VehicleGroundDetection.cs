using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    [System.Serializable]
    public class VehicleGroundDetection
    {
        public LayerMask GroundLayers = Physics.DefaultRaycastLayers;
        public float RaycastDist = 0.25f;
        Vehicle vehicle;

        public bool IsGrounded { get; private set; }

        public void Initialize(Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        public void CheckGround()
        {
            Ray ray = new Ray(vehicle.transform.position + vehicle.transform.up * 1f, -vehicle.transform.up);
            var raycastHit = new RaycastHit();
            IsGrounded = Physics.Raycast(ray, out raycastHit, RaycastDist, GroundLayers);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.red);
        }

        public void OnDrawGizmosSelected(Vehicle vehicle)
        {
#if UNITY_EDITOR
            var direction = -vehicle.transform.up;
            var length = RaycastDist;

            Debug.DrawRay(vehicle.transform.position, direction * length, Color.magenta);
#endif
        }
    }
}