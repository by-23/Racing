using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Projectile projectile;
    [SerializeField] private Transform muzzlePosition;
    [SerializeField] private Vehicle vehicle;
    [SerializeField] private float despawnDelay = 20;
    [SerializeField] private float fireRate = 1f; // Задержка между выстрелами в секундах

    private float nextTimeToShoot = 0f;

    public void TryFire()
    {
        if (Time.time >= nextTimeToShoot)
        {
            Fire();
            nextTimeToShoot = Time.time + fireRate; // Обновляем время следующего выстрела
        }
    }

    private void Fire()
    {
        GameObject newProjectile = null;
        if (RoomManager.Instance.isOnline)
            newProjectile = PhotonNetwork.Instantiate("Projectile", transform.position, quaternion.identity);
        else
            newProjectile = Instantiate(projectile.gameObject);

        newProjectile.GetComponent<Projectile>().Owner = vehicle;
        var newProjectileTransform = newProjectile.transform;
        newProjectileTransform.position = muzzlePosition.position;
        newProjectileTransform.rotation = muzzlePosition.rotation;
    }
}