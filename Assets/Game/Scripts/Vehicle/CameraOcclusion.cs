using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraFollow))]
public class CameraOcclusion : MonoBehaviour
{
    [SerializeField]
    private LayerMask occlusionLayers = ~0;

    [SerializeField, Range(0.0f, 1.0f)]
    private float fadeAlpha = 0.3f;

    private Camera cam;
    private CameraFollow cameraFollow;
    private Transform target;
    
    private readonly Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();
    private readonly Dictionary<Renderer, Material[]> occludedRenderers = new Dictionary<Renderer, Material[]>();
    private readonly HashSet<Renderer> hitRenderersInFrame = new HashSet<Renderer>();
    private readonly List<Renderer> renderersToUnfadeCache = new List<Renderer>();

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cameraFollow = GetComponent<CameraFollow>();
    }

    private void LateUpdate()
    {
        target = cameraFollow.Target;
        if (target == null)
        {
            UnfadeAll();
            return;
        }

        UpdateOcclusion();
    }

    private void UpdateOcclusion()
    {
        Vector3 direction = target.position - cam.transform.position;
        float distance = direction.magnitude;

        hitRenderersInFrame.Clear();
        RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, direction, distance, occlusionLayers);

        foreach (var hit in hits)
        {
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
            
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                hitRenderersInFrame.Add(rend);
            }
        }
        
        renderersToUnfadeCache.Clear();
        foreach (var rend in occludedRenderers.Keys)
        {
            if (!hitRenderersInFrame.Contains(rend))
            {
                renderersToUnfadeCache.Add(rend);
            }
        }
        foreach(var rend in renderersToUnfadeCache)
        {
            UnfadeMaterial(rend);
        }

        foreach (var rend in hitRenderersInFrame)
        {
            if (!occludedRenderers.ContainsKey(rend))
            {
                FadeMaterial(rend);
            }
        }
    }

    private void FadeMaterial(Renderer rend)
    {
        occludedRenderers[rend] = rend.materials;

        var newMaterials = new Material[rend.materials.Length];
        for (int i = 0; i < rend.materials.Length; i++)
        {
            newMaterials[i] = GetTransparentMaterial(rend.materials[i]);
        }
        rend.materials = newMaterials;
    }

    private Material GetTransparentMaterial(Material originalMat)
    {
        if (materialCache.TryGetValue(originalMat, out Material cachedMat))
        {
            return cachedMat;
        }

        var newMat = new Material(originalMat)
        {
            name = originalMat.name + " (Transparent)"
        };

        newMat.SetFloat("_Mode", 3);
        newMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        newMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        newMat.SetInt("_ZWrite", 0);
        newMat.DisableKeyword("_ALPHATEST_ON");
        newMat.EnableKeyword("_ALPHABLEND_ON");
        newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        newMat.renderQueue = (int)RenderQueue.Transparent;
        
        var color = newMat.color;
        color.a = fadeAlpha;
        newMat.color = color;

        materialCache[originalMat] = newMat;
        return newMat;
    }
    
    private void UnfadeMaterial(Renderer rend)
    {
        if (occludedRenderers.TryGetValue(rend, out var originalMaterials))
        {
            rend.materials = originalMaterials;
            occludedRenderers.Remove(rend);
        }
    }
    
    private void UnfadeAll()
    {
        foreach (var rend in occludedRenderers.Keys.ToList())
        {
            UnfadeMaterial(rend);
        }
    }

    private void OnDestroy()
    {
        UnfadeAll();
        foreach (var material in materialCache.Values)
        {
            Destroy(material);
        }
        materialCache.Clear();
    }
} 