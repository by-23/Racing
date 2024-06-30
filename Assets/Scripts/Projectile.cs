using System;
using Ilumisoft.SkillDrive;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float followSpeed;

    [SerializeField] private float upwardsModifier = 5;

    private Rigidbody rBody;
    private Vehicle targetVehicle;
    private bool hasTarget;
    public float oscillationRange = 1f;
    public float oscillationFrequency = 1f;
    private float timeCounter = 0f;

    public float explosionForce = 10f;
    public float explosionRadius = 5f;


    public Vehicle Owner { get; set; }

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Explode();
        }
    }

    private void FixedUpdate()
    {
        if (hasTarget)
        {
            transform.rotation = Quaternion.LookRotation(targetVehicle.transform.position - transform.position);
            timeCounter += Time.deltaTime;

            float horizontalOscillation = Mathf.Sin(timeCounter * oscillationFrequency) * oscillationRange;

            Vector3 movement = (transform.forward + new Vector3(horizontalOscillation, 0f, 0f)).normalized;

            transform.Translate(movement * (followSpeed * Time.deltaTime), Space.World);
        }

        else
        {
            transform.Translate(transform.forward * (speed * Time.deltaTime), Space.World);
        }
    }

    public void SetTarget(Vehicle foundedTarget)
    {
        if (!hasTarget && foundedTarget != Owner)
        {
            targetVehicle = foundedTarget;
            hasTarget = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Vehicle vehicle) && Owner != null && vehicle != Owner)
        {
            
            if (!RoomManager.Instance.isOnline)
                Explode();
            else
                this.GetComponent<PhotonView>().RPC("ExplodeRPC", RpcTarget.All);

            Destroy(this);
        }
    }

    [PunRPC]
    void ExplodeRPC()
    {
        Explode();
    }

    void Explode()
    {
        this.enabled = false;
        this.GetComponent<Collider>().isTrigger = true;

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier);
            }
        }

        Destroy(gameObject);
    }

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
        hasTarget = false;
        targetVehicle = null;
    }
}