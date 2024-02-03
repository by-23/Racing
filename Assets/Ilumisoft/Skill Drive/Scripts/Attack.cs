using System;
using System.Collections;
using System.Collections.Generic;
using DesignPatterns.ObjectPool;
using Ilumisoft.SkillDrive;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Tooltip("Prefab to shoot")] [SerializeField]
    private GameObject projectile;

    [Tooltip("Projectile force")] [SerializeField]
    float muzzleVelocity = 700f;

    [Tooltip("End point of gun where shots appear")] [SerializeField]
    private Transform muzzlePosition;

    [Tooltip("Time between shots / smaller = higher rate of fire")] [SerializeField]
    float cooldownWindow = 0.1f;

    [Tooltip("Reference to Object Pool")] [SerializeField]
    ObjectPool objectPool;

    private float nextTimeToShoot;

    private void Start()
    {
        objectPool = FindObjectOfType<ObjectPool>();
    }

    private void FixedUpdate()
    {
        // shoot if we have exceeded delay
        if (Input.GetButton("Fire1") && Time.time > nextTimeToShoot && objectPool != null)
        {
            // get a pooled object instead of instantiating
            var bulletObject = objectPool.GetPooledObject();
            bulletObject.Owner = this.gameObject;

            if (bulletObject == null)
                return;

            bulletObject.gameObject.SetActive(true);

            // align to gun barrel/muzzle position
            bulletObject.transform.SetPositionAndRotation(muzzlePosition.position, muzzlePosition.rotation);

            // move projectile forward
            bulletObject.Rb.AddForce(bulletObject.transform.forward * muzzleVelocity, ForceMode.Acceleration);

            // turn off after a few seconds
            Projectile projectile = bulletObject.Projectile;
            projectile?.Deactivate();

            // set cooldown delay
            nextTimeToShoot = Time.time + cooldownWindow;
        }
    }
}