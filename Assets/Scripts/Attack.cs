using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Projectile projectile;

    [SerializeField] private Transform muzzlePosition;

    [SerializeField] float cooldownWindow = 0.1f;

    [SerializeField] private Vehicle vehicle;

    [SerializeField] private float despawnDelay = 20;

    private float nextTimeToShoot;

    private void Update()
    {
        if (Input.GetButton("Fire1") && Time.time > nextTimeToShoot)
        {
            GameObject newProjectile = PhotonNetwork.Instantiate("Projectile", transform.position, quaternion.identity);
            newProjectile.GetComponent<Projectile>().Owner = vehicle;
            var newProjectileTransform = newProjectile.transform;
            newProjectileTransform.position = muzzlePosition.position;
            newProjectileTransform.rotation = muzzlePosition.rotation;
            nextTimeToShoot = Time.time + cooldownWindow;
        }
    }
}