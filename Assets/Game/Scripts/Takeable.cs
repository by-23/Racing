using System;
using NTC.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

public class Takeable : MonoBehaviour
{
    [SerializeField] private Item[] itemVariants;
    [field: SerializeField]
    public Item item { get; private set; }
    private Collider collider;
    private Rigidbody rb;
    private Renderer render;

    private void Awake()
    {
        if (item == null)
            item = itemVariants[Random.Range(0, itemVariants.Length)];
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        render = GetComponent<Renderer>();

    }
    private void Start()
    {


        if (item != null)
        {
            SetColor(item.color);
        }
    }

    private void SetColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    public void DestroyItem()
    {
        if (item == null)
            return;
        if (collider != null)
            collider.enabled = false;
        if (rb != null)
            rb.isKinematic = true;
        if (render != null)
            render.enabled = false;
        NightPool.Despawn(gameObject, .1f);
    }
}