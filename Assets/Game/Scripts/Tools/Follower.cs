using System;
using System.Collections;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Follower : MonoBehaviour
{
    internal Vehicle targetVehicle;

    [SerializeField] private float speed;
    [SerializeField] private float followSpeed;
    [SerializeField] private float lifetime = 15f;

    private bool hasTarget;
    public float oscillationRange = 1f;
    public float oscillationFrequency = 1f;
    private float timeCounter = 0f;

    private Item item;


    private Rigidbody rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        item = GetComponent<Item>();
    }

    private void OnEnable()
    {
        StartCoroutine(DespawnAfterLifetime());
    }

    private void FixedUpdate()
    {
        if (hasTarget)
        {
            transform.rotation = Quaternion.LookRotation(targetVehicle.transform.position - transform.position);
            timeCounter += Time.deltaTime;

            float horizontalOscillation = Mathf.Sin(timeCounter * oscillationFrequency) * oscillationRange;

            Vector3 movement = (transform.forward + new Vector3(horizontalOscillation, 0f, 0f)).normalized;

            rb.transform.Translate(movement * (followSpeed * Time.deltaTime), Space.World);
        }

        else
        {
            rb.transform.Translate(transform.forward * (speed * Time.deltaTime), Space.World);
        }
    }

    public void SetTarget(Vehicle foundedTarget)
    {
        if (!hasTarget && foundedTarget != item.Owner)
        {
            targetVehicle = foundedTarget;
            hasTarget = true;
        }
    }


    private IEnumerator DespawnAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (RoomManager.Instance.IsOnline)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnDespawn()
    {
        hasTarget = false;
        targetVehicle = null;
    }
}