using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class Follower : MonoBehaviour
{
    internal Vehicle targetVehicle;

    [SerializeField] private float speed;
    [SerializeField] private float followSpeed;
    [SerializeField] private float lifetime = 15f;
    [SerializeField] private bool followImmediately;

    private bool hasTarget;
    public float oscillationRange = 1f;
    public float oscillationFrequency = 1f;
    private float timeCounter = 0f;

    private Item item;
    private Rigidbody rb;

    private void OnEnable()
    {
        StartCoroutine(DespawnAfterLifetime());
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        item = GetComponent<Item>();
        if (followImmediately)
            targetVehicle = GetNearestTarget();
    }

    private void FixedUpdate()
    {
        if (targetVehicle && hasTarget || followImmediately)
        {
            float targetVehicleSpeed = targetVehicle.vehicleMovement.rb.velocity.magnitude;
            float vehicleSpeed =
                targetVehicleSpeed < 30 ? targetVehicleSpeed : 30;

            transform.rotation = Quaternion.LookRotation(targetVehicle.transform.position - transform.position);
            timeCounter += Time.deltaTime;

            float horizontalOscillation = Mathf.Sin(timeCounter * oscillationFrequency) * oscillationRange;

            Vector3 movement = (transform.forward + new Vector3(horizontalOscillation, 0f, 0f)).normalized;

            rb.transform.Translate(movement * ((vehicleSpeed + followSpeed) * Time.deltaTime), Space.World);
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

    private Vehicle GetNearestTarget()
    {
        float minDistance = float.MaxValue;
        Vehicle nearestTarget = null;

        foreach (var player in PlayersSpawner.Instance.instantiatedPlayers)
        {
            var currentVehicle = player.GetComponent<Vehicle>();
            if (player == null || player == item.Owner.gameObject) continue;

            float distance = Vector3.Distance(player.transform.position, transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTarget = currentVehicle;
            }
        }

        return nearestTarget;
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