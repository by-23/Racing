using System;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Attack : MonoBehaviour
{
    [SerializeField] private Item item;
    [SerializeField] private PhotonView photonView;
    [SerializeField] protected Transform muzzlePosition;
    [SerializeField] private Vehicle vehicle;
    [SerializeField] protected float fireRate = 1f; // Задержка между выстрелами в секундах

    protected float nextTimeToShoot = 0f;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void TryFire()
    {
        if (Time.time >= nextTimeToShoot && GameController.Instance.isGameStarted)
        {
            Fire();
            nextTimeToShoot = Time.time + fireRate / 100; // Обновляем время следующего выстрела
        }
    }

    private void Fire()
    {
        GameObject itemObj = null;
        if (RoomManager.Instance.IsOnline)
        {
            // Создаём снаряд через Photon
            itemObj = PhotonNetwork.Instantiate(item.gameObject.name, transform.position, Quaternion.identity);
            // Вызываем RPC для установки параметров снаряда на всех клиентах
            photonView.RPC("RPC_HandleProjectile", RpcTarget.All, itemObj.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            // Создаём снаряд стандартным способом
            itemObj = Instantiate(item.gameObject);
            // Прямо устанавливаем параметры снаряда
            SetupItem(itemObj);
        }
    }

    /// <summary>
    /// Общий метод для установки параметров снаряда.
    /// </summary>
    /// <param name="itemObj">Объект снаряда.</param>
    private void SetupItem(GameObject itemObj)
    {
        if (itemObj.TryGetComponent(out Item item))
            item.Init(vehicle);
        itemObj.transform.position = muzzlePosition.position;
        itemObj.transform.rotation = muzzlePosition.rotation;
    }

    
    [PunRPC]
    private void RPC_HandleProjectile(int viewID)
    {
        PhotonView projPhotonView = PhotonView.Find(viewID);
        if (projPhotonView != null)
        {
            GameObject newProjectile = projPhotonView.gameObject;
            SetupItem(newProjectile);
        }
    }
}