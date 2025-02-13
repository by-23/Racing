using Ilumisoft.SkillDrive;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private ParticleSystem explosionFXPrefab;
    [SerializeField] private float upwardsModifier = 5f;
    [SerializeField] protected Collider _collider;
    [SerializeField] private LayerMask _layerMask;
    private Item item;


    private void OnValidate()
    {
        if (!_collider)
        {
            _collider = GetComponent<Collider>();
        }
    }

    private void Awake()
    {
        item = GetComponent<Item>();
    }


    protected void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Vehicle vehicle) && vehicle != item.Owner)
        {
            if (!RoomManager.Instance.IsOnline)
            {
                Explode(vehicle);
            }
            else
                this.GetComponent<PhotonView>().RPC("ExplodeRPC", RpcTarget.All, vehicle);
        }
    }

    private void Explode(Vehicle vehicle)
    {
        Destroy(_collider);
        GameObject newExplosionFX;
        if (RoomManager.Instance.IsOnline)
            newExplosionFX = PhotonNetwork.Instantiate("CFXR Explosion Smoke 2 Solo (HDR)", transform.position,
                quaternion.identity);
        else
            newExplosionFX = Instantiate(explosionFXPrefab.gameObject);
        newExplosionFX.transform.position = transform.position;
        newExplosionFX.gameObject.SetActive(true);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, _layerMask);

        foreach (Collider collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce * 1000000, transform.position, explosionRadius,
                    upwardsModifier * 1000000);
            }
        }

        vehicle.healthController.TakeDamage(50);
        Destroy(gameObject);
    }

    [PunRPC]
    void ExplodeRPC(Vehicle vehicle)
    {
        Explode(vehicle);
    }
}