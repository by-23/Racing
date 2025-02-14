using UnityEngine;

public class Takeable : MonoBehaviour
{
    private Item item;
    [SerializeField] private ItemVariant[] itemVariants;

    private void Start()
    {
        if (itemVariants != null && itemVariants.Length > 0)
        {
            int randomIndex = Random.Range(0, itemVariants.Length);
            item = itemVariants[randomIndex].item;
            SetColor(itemVariants[randomIndex].color);
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
}

[System.Serializable]
public class ItemVariant
{
    public Item item;
    public Color color;
}