using System.Linq;
using AYellowpaper;
using UnityEditor;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IItemEffect, MonoBehaviour>[] effects;
    [SerializeField] private float beingDeactivatedDuration = 5f;

    public Color color;

    internal Vehicle Owner { get; private set; }

    private void Init()
    {
        if (effects == null || effects.Length < 1)
        {
            effects = GetComponents<IItemEffect>()
                .Select(effect => new InterfaceReference<IItemEffect, MonoBehaviour>(effect)).ToArray();
        }
    }

    internal void Activate(Vehicle owner)
    {
        Owner = owner;
        foreach (var effect in effects)
            effect.Value.Activate(owner);
    }

    internal void Deactivate()
    {
        foreach (var effect in effects)
            effect.Value.Deactivate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Init();
        EditorUtility.SetDirty(this);
    }
#endif
}

public interface IItemEffect
{
    public void Activate(Vehicle owner);
    public void Deactivate();
}