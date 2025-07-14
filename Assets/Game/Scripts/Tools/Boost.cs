using UnityEngine;
public class Boost : MonoBehaviour, IItemEffect
{
    [SerializeField] private float speedMultiplier = 3;

    public void Activate(Vehicle owner)
    {
        owner.vehicleMovement._rb.AddForce(owner.transform.forward * speedMultiplier, ForceMode.Impulse);
        Destroy(this.gameObject);
    }
    public void Deactivate()
    {
    }
    public void Reactivate()
    {
    }
}