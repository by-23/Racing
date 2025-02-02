using System;
using Ilumisoft.SkillDrive;
using Photon.Pun;
using UnityEngine;

public class BotAttack : Attack
{
    [Header("Bot Settings")] [SerializeField]
    private float detectionRange = 20f; // Дальность обнаружения цели

    [SerializeField, Range(1, 10)] private float attackChance = 0.5f; // Вероятность атаки (от 0 до 1)

    private void Update()
    {
        DetectAndAttack();
    }

    private void DetectAndAttack()
    {
        // Предполагается, что muzzlePosition определён в базовом классе (например, как protected)
        Ray ray = new Ray(muzzlePosition.position, muzzlePosition.forward);
        if (Time.time >= nextTimeToShoot && Physics.Raycast(ray, out RaycastHit hit, detectionRange))
        {
            if (hit.collider.CompareTag("PlayerMesh"))
            {
                int randomValue = UnityEngine.Random.Range(1, 10);
                if (randomValue <= attackChance)
                {
                    TryFire();
                    nextTimeToShoot = Time.time + fireRate;
                }
            }
        }

        // Для визуализации луча в редакторе (опционально)
        Debug.DrawRay(muzzlePosition.position, muzzlePosition.forward * detectionRange, Color.red);
    }
}