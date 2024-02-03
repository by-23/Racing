using System;
using System.Collections;
using Ilumisoft.SkillDrive;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;

namespace DesignPatterns.ObjectPool
{
    [RequireComponent(typeof(PooledObject))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] private float rotateSpeed;
        [SerializeField] private float detectionRadius;
        [SerializeField] private float timeoutDelay;

        private PooledObject pooledObject;

        private Rigidbody rBody;
        private bool isFoundTarget;
        Vehicle targetVehicle;

        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
            rBody = GetComponent<Rigidbody>();
        }


        private void OnDrawGizmos()
        {
            // Проводим сферический рейкаст
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, detectionRadius, transform.forward, out hit, Mathf.Infinity))
            {
                // Можно также рисовать гизмо для визуализации попадания
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + transform.forward * hit.distance, detectionRadius);
            }
        }

        public void Deactivate()
        {
            StartCoroutine(DeactivateRoutine(timeoutDelay));
        }

        IEnumerator DeactivateRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // reset the moving Rigidbody

            rBody.velocity = new Vector3(0f, 0f, 0f);
            rBody.angularVelocity = new Vector3(0f, 0f, 0f);

            // set inactive and return to pool
            pooledObject.Release();
            gameObject.SetActive(false);
        }
    }
}