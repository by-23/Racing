using Ilumisoft.SkillDrive;
using NTC.Pool;
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
            Projectile newProjectile = NightPool.Spawn(projectile);
            newProjectile.Owner = vehicle;
            var newProjectileTransform = newProjectile.transform;
            newProjectileTransform.position = muzzlePosition.position;
            newProjectileTransform.rotation = muzzlePosition.rotation;
            // NightPool.Despawn(newProjectile, despawnDelay);
            nextTimeToShoot = Time.time + cooldownWindow;
        }
    }
}