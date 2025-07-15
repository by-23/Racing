using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

public class Explosive : MonoBehaviour, IAttacking, IItemEffect
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
    }

    public void Activate(Vehicle owner)
    {
        if (activationType == ActivationType.Instantly)
            TryAttack();
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (activationType != ActivationType.OnTrigger) return;

        if ((!other.TryGetComponent(out Vehicle vehicle) || vehicle == _item.Owner) &&
            !other.CompareTag("Obstacle")) return;

        if (!RoomManager.Instance.IsOnline)
            TryAttack();
        else
            this.GetComponent<PhotonView>().RPC("ExplodeRPC", RpcTarget.All);
    }

    public void TryAttack()
    {
        if (activationType == ActivationType.OnTrigger)
            Destroy(_collider);
        ApplyFX();

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, layerMask);
        foreach (Collider collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            HealthController healthController = collider.GetComponentInParent<HealthController>();
            Vehicle vehicle = collider.GetComponentInParent<Vehicle>();
            if (rb != null)
            {
                ApplyForce(vehicle, rb);

                ApplyDamage(healthController, vehicle);
            }
        }

        if (activationType == ActivationType.OnTrigger)
            Destroy(gameObject);
        else
            enabled = false;
    }

    private void ApplyDamage(HealthController healthController, Vehicle vehicle)
    {
        if (healthController != null && _item.Owner != vehicle)
        {
            healthController.TakeDamage(damage);
        }
    }

    private void ApplyForce(Vehicle vehicle, Rigidbody rb)
    {
        if (activationType == ActivationType.OnTrigger || _item.Owner != vehicle)
        {
            rb.AddExplosionForce(explosionForce * 1000000, transform.position, explosionRadius,
                upwardsModifier * 1000000);
        }
    }

    private void ApplyFX()
    {
        GameObject newExplosionFX;
        if (RoomManager.Instance.IsOnline)
            newExplosionFX = PhotonNetwork.Instantiate("CFXR Explosion Smoke 2 Solo (HDR)", transform.position,
                quaternion.identity);
        else
            newExplosionFX = Instantiate(explosionFXPrefab.gameObject);

        newExplosionFX.transform.position = transform.position;
        newExplosionFX.gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        _collider.enabled = false;
        ApplyFX();
       Destroy(gameObject);
    }


    [PunRPC]
    void ExplodeRPC()
    {
        TryAttack();
    }
}