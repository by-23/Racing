using System;
using Game.Scripts.Interfaces;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemsController : MonoBehaviour
{
    [SerializeField] protected Item[] items = new Item[3];
    [SerializeField] private PhotonView photonView;
    [SerializeField] protected Transform muzzlePosition;
    [SerializeField] protected Vehicle vehicle;
    [SerializeField] protected float fireRate = 1f;
    protected float nextTimeToShoot = 0f;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    internal void ActivateTool(int index)
    {
        if (items[index] == null) return;

        GameObject itemObj = null;
        if (RoomManager.Instance.IsOnline)
        {
            // Создаём снаряд через Photon
            itemObj = PhotonNetwork.Instantiate(items[index].gameObject.name, transform.position, Quaternion.identity);
            // Вызываем RPC для установки параметров снаряда на всех клиентах
            photonView.RPC("RPC_HandleProjectile", RpcTarget.All, itemObj.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            // Создаём снаряд стандартным способом
            itemObj = Instantiate(items[index].gameObject, muzzlePosition.position, muzzlePosition.rotation);
            // Прямо устанавливаем параметры снаряда
            SetupItem(itemObj);
        }
        items[index] = null;
    }

    private void SetupItem(GameObject itemObj)
    {
        if (itemObj.TryGetComponent(out Item item))
            item.Init(vehicle);
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