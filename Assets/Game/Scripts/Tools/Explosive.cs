using System;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Explosive : MonoBehaviour, IAttacking
{
    private enum ActivationType
    {
        Instantly,
        OnTrigger
    }

    [SerializeField] private ParticleSystem explosionFXPrefab;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardsModifier = 5f;
    [SerializeField] protected Collider _collider;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private ActivationType activationType;
    [SerializeField] private float damage = 50;
    private Item _item;
    public Item Item => _item;

    private void OnValidate()
    {
        if (!_collider)
            _collider = GetComponent<Collider>();
    }

    private void Awake()
    {
        _item = GetComponent<Item>();
        _item.OnDeactivate += Deactivate;
        _item.OnReactivate += Reactivate;
        _item.OnInit += Init;
    }

    private void Init()
    {
        if (activationType == ActivationType.Instantly)
            Attack();
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (activationType != ActivationType.OnTrigger) return;
        if ((!other.TryGetComponent(out Vehicle vehicle) || vehicle == _item.Owner) &&
            !other.CompareTag("Obstacle")) return;

        if (!RoomManager.Instance.IsOnline)
            Attack();
        else
            this.GetComponent<PhotonView>().RPC("ExplodeRPC", RpcTarget.All);
    }

    public void Attack()
    {
        if (activationType == ActivationType.OnTrigger)
            Destroy(_collider);
        GameObject newExplosionFX;
        if (RoomManager.Instance.IsOnline)
            newExplosionFX = PhotonNetwork.Instantiate("CFXR Explosion Smoke 2 Solo (HDR)", transform.position,
                quaternion.identity);
        else
            newExplosionFX = Instantiate(explosionFXPrefab.gameObject);

        newExplosionFX.transform.position = transform.position;
        newExplosionFX.gameObject.SetActive(true);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, layerMask);
        foreach (Collider collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            HealthController healthController = collider.GetComponentInParent<HealthController>();
            Vehicle vehicle = collider.GetComponentInParent<Vehicle>();
            if (rb != null)
            {
                if (activationType == ActivationType.OnTrigger || _item.Owner != vehicle)
                {
                    rb.AddExplosionForce(explosionForce * 1000000, transform.position, explosionRadius,
                        upwardsModifier * 1000000);
                }

                if (healthController != null && _item.Owner != vehicle)
                {
                    healthController.TakeDamage(damage);
                }
            }
        }

        if (activationType == ActivationType.OnTrigger)
            Destroy(gameObject);
        else
            enabled = false;
    }

    private void Deactivate()
    {
        enabled = false;
    }

    private void Reactivate()
    {
        enabled = true;
    }

    private void OnDestroy()
    {
        _item.OnDeactivate -= Deactivate;
        _item.OnReactivate -= Reactivate;
        _item.OnInit -= Init;
    }

    [PunRPC]
    void ExplodeRPC()
    {
        Attack();
    }
}