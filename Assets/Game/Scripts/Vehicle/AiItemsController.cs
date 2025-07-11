using System;
using Game.Scripts.Interfaces;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

public class AiItemsController : ItemsController
{
    [Header("Bot Settings")] [SerializeField]
    private float detectionRange = 20f; // Дальность обнаружения цели

    [SerializeField, Range(0, 100)] private int attackChance; // Вероятность атаки (от 0 до 1)

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.TryGetComponent(out IAttacking attacker) && attacker.Item.Owner != this.vehicle)
        {
            TryDefense();
        }
    }

    private void Update()
    {
        if (GameController.Instance.isGameStarted)
            TryAttack();
    }

    private void TryDefense()
    {
        if (GameController.Instance.isGameStarted)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].GetComponent<IDefensive>() != null)
                {
                    ActivateTool(i);
                    break;
                }
            }
        }
    }

    private void TryAttack()
    {
        int randomValue = Random.Range(1, 100);
        if (randomValue <= attackChance)
        {
            Ray ray = new Ray(muzzlePosition.position, muzzlePosition.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, detectionRange))
            {
                if (hit.collider.transform.parent != null && hit.collider.transform.parent.GetComponent<Vehicle>())
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i] != null && items[i].GetComponent<IDistantAttacking>() != null)
                        {

                            if (Time.time >= nextTimeToShoot && GameController.Instance.isGameStarted)
                            {
                                ActivateTool(i);
                                nextTimeToShoot = Time.time + fireRate / 100; // Обновляем время следующего выстрела
                            }

                            nextTimeToShoot = Time.time + fireRate;
                            break;
                        }
                    }
                }
            }
        }

        // Для визуализации луча в редакторе (опционально)
        // Debug.DrawRay(muzzlePosition.position, muzzlePosition.forward * detectionRange, Color.red);
    }
}