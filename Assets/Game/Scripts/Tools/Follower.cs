using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class Follower : MonoBehaviour, IDistantAttacking
{
    internal Vehicle targetVehicle;

    [SerializeField] private float speed;
    [SerializeField] private float addedSpeed;
    [SerializeField] private float lifetime = 15f;
    [SerializeField] private bool followImmediately;

    private bool hasTarget;
    public float oscillationRange = 1f;
    public float oscillationFrequency = 1f;
    private float timeCounter = 0f;

    private Item _item;
    private Rigidbody rb;

    private void OnEnable()
    {
        StartCoroutine(DespawnAfterLifetime());
    }

    private void Start()
    {
        _item = GetComponent<Item>();
        rb = GetComponent<Rigidbody>();
        _item.OnDeactivate += Deactivate;
        _item.OnReactivate += Reactivate;
        if (followImmediately && TryGetNearestTarget(out Vehicle nearestTarget))
            targetVehicle = nearestTarget;
    }

    private void FixedUpdate()
    {
        if (targetVehicle && (hasTarget || followImmediately))
        {
            float targetVehicleSpeed = targetVehicle.vehicleMovement.rb.linearVelocity.magnitude;
            float vehicleSpeed =
                targetVehicleSpeed < 50 ? 50 : targetVehicleSpeed;

            transform.rotation = Quaternion.LookRotation(targetVehicle.transform.position - transform.position);
            timeCounter += Time.deltaTime;

            float horizontalOscillation = Mathf.Sin(timeCounter * oscillationFrequency) * oscillationRange;

            Vector3 movement = (transform.forward + new Vector3(horizontalOscillation, 0f, 0f)).normalized;

            rb.transform.Translate(movement * ((vehicleSpeed + addedSpeed) * Time.deltaTime), Space.World);
        }

        else
        {
            rb.transform.Translate(transform.forward * (speed * Time.deltaTime), Space.World);
        }
    }

    public void SetTarget(Vehicle foundedTarget)
    {
        if (!hasTarget && foundedTarget != _item.Owner)
        {
            targetVehicle = foundedTarget;
            hasTarget = true;
        }
    }

    private Boolean TryGetNearestTarget(out Vehicle nearestTarget)
    {
        float minDistance = float.MaxValue;
        nearestTarget = null;


        // foreach (var player in PlayersSpawner.Instance.instantiatedPlayers)
        // {
        //     if (player == null || player == _item.Owner.gameObject)
        //         continue;
        //
        //     // Переводим позицию цели в локальные координаты владельца
        //     Vector3 localPos = _item.Owner.transform.InverseTransformPoint(player.transform.position);
        //
        //     // Если цель не перед игроком (например, сзади или на уровне), пропускаем её
        //     if (localPos.z <= 0)
        //         continue;
        //
        //     // Можно дополнительно ограничить угол (например, 45°)
        //     // float angle = Mathf.Atan2(Mathf.Abs(localPos.x), localPos.z) * Mathf.Rad2Deg;
        //     // if (angle > 45f)
        //     //     continue;
        //
        //     float distance = localPos.magnitude;
        //     if (distance < minDistance)
        //     {
        //         minDistance = distance;
        //         nearestTarget = player.GetComponent<Vehicle>();
        //     }
        // }

        if (nearestTarget == null)
        {
            // print("No target found");
            return false;
        }

        return true;
    }

    private void Deactivate()
    {
        enabled = false;
    }

    private void Reactivate()
    {
        enabled = true;
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

    private void OnDestroy()
    {
        _item.OnDeactivate -= Deactivate;
        _item.OnReactivate -= Reactivate;
    }
}