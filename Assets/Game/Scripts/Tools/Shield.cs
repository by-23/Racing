using UnityEngine;

public class Shield : MonoBehaviour
{
    protected void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Follower projective))
        {
            Destroy(other.gameObject);
        }
    }
}