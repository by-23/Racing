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
            // Создаём снаряд через Photon
            newProjectile = PhotonNetwork.Instantiate("Projectile", transform.position, Quaternion.identity);
            // Вызываем RPC для установки параметров снаряда на всех клиентах
            photonView.RPC("RPC_HandleProjectile", RpcTarget.All, newProjectile.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            // Создаём снаряд стандартным способом
            newProjectile = Instantiate(projectile.gameObject);
            // Прямо устанавливаем параметры снаряда
            SetupProjectile(newProjectile);
        }
    }

    /// <summary>
    /// Общий метод для установки параметров снаряда.
    /// </summary>
    /// <param name="projectileObj">Объект снаряда.</param>
    private void SetupProjectile(GameObject projectileObj)
    {
        Projectile proj = projectileObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Owner = vehicle;
        }

        projectileObj.transform.position = muzzlePosition.position;
        projectileObj.transform.rotation = muzzlePosition.rotation;
    }

    [PunRPC]
    private void RPC_HandleProjectile(int viewID)
    {
        PhotonView projPhotonView = PhotonView.Find(viewID);
        if (projPhotonView != null)
        {
            GameObject newProjectile = projPhotonView.gameObject;
            SetupProjectile(newProjectile);
        }
    }
}