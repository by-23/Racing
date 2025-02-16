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
                targetVehicleSpeed < 50 ? 50 : targetVehicleSpeed;

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
            if (player == null || player == item.Owner.gameObject)
                continue;

            // Переводим позицию цели в локальные координаты владельца
            Vector3 localPos = item.Owner.transform.InverseTransformPoint(player.transform.position);

            // Если цель не перед игроком (например, сзади или на уровне), пропускаем её
            if (localPos.z <= 0)
                continue;

            // Можно дополнительно ограничить угол (например, 45°)
            // float angle = Mathf.Atan2(Mathf.Abs(localPos.x), localPos.z) * Mathf.Rad2Deg;
            // if (angle > 45f)
            //     continue;

            float distance = localPos.magnitude;
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTarget = player.GetComponent<Vehicle>();
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