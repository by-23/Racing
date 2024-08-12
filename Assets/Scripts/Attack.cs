using System;
using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Projectile projectile;
    [SerializeField] private PhotonView photonView;
    [SerializeField] private Transform muzzlePosition;
    [SerializeField] private Vehicle vehicle;
    [SerializeField] private float fireRate = 1f; // Задержка между выстрелами в секундах

    private float nextTimeToShoot = 0f;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

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
        {
            newProjectile = PhotonNetwork.Instantiate("Projectile", transform.position, Quaternion.identity);
            // Call the RPC method to handle the rest of the logic
            photonView.RPC("HandleProjectile", RpcTarget.All, newProjectile.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            newProjectile = Instantiate(projectile.gameObject);
            HandleProjectile(newProjectile.GetComponent<PhotonView>().ViewID);
        }
    }

    [PunRPC]
    private void HandleProjectile(int viewID)
    {
        GameObject newProjectile = PhotonView.Find(viewID).gameObject;
        newProjectile.GetComponent<Projectile>().Owner = vehicle;
        var newProjectileTransform = newProjectile.transform;
        newProjectileTransform.position = muzzlePosition.position;
        newProjectileTransform.rotation = muzzlePosition.rotation;
    }
}