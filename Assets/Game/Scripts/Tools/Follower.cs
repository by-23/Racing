using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class Follower : MonoBehaviour, IDistantAttacking, IItemEffect
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
        if (followImmediately && TryGetNearestTarget(out Vehicle nearestTarget))
            targetVehicle = nearestTarget;
    }

    public void Activate(Vehicle owner)
    {
        transform.position = owner.muzzlePosition.position;
    }

    private void FixedUpdate()
    {
        if (targetVehicle && (hasTarget || followImmediately))
        {
            float targetVehicleSpeed = targetVehicle.vehicleMovement._rb.linearVelocity.magnitude;
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

        if (nearestTarget == null)
        {
            return false;
        }

        return true;
    }

    public void Deactivate()
    {
        enabled = false;
    }

    public void Reactivate()
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
}