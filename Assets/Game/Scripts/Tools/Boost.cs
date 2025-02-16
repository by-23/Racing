using System;
using UnityEngine;

public class Boost : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 3;
    private Item item;

    private void Awake()
    {
        item = GetComponent<Item>();
        item.OnInit += ApplyBoost;
        
    }


    internal void ApplyBoost()
    {
        item.Owner.vehicleMovement.rb.AddForce(item.Owner.transform.forward * speedMultiplier, ForceMode.Impulse);
        Destroy(this.gameObject);
    }
    private void OnDestroy()
    {
        item.OnInit -= ApplyBoost;
    }
}