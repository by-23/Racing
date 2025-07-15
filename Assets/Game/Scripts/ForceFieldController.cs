using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ForceFieldController : MonoBehaviour
{
    [Header("Основные параметры")]
    [SerializeField] private Color color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
    [SerializeField] private float alpha = 0.5f;
    
    [Header("Эффект Френеля")]
    [SerializeField] private float fresnelPower = 2.0f;
    [SerializeField] private float fresnelScale = 1.0f;
    
    [Header("Rim Lighting")]
    [SerializeField] private float rimPower = 3.0f;
    [SerializeField] private float rimIntensity = 2.0f;
    
    [Header("Пульсация")]
    [SerializeField] private float pulseSpeed = 1.0f;
    [SerializeField] private float pulseIntensity = 0.3f;
    
    [Header("Шум и искажение")]
    [SerializeField] private float noiseScale = 1.0f;
    [SerializeField] private float noiseSpeed = 0.5f;
    [SerializeField] private float distortion = 0.02f;
    
    [Header("Анимация")]
    [SerializeField] private bool animateColor = false;
    [SerializeField] private Color secondaryColor = new Color(1.0f, 0.2f, 0.6f, 1.0f);
    [SerializeField] private float colorAnimationSpeed = 1.0f;
    
    private MeshRenderer meshRenderer;
    private Material material;
    private Color originalColor;
    
    // Названия параметров шейдера
    private const string COLOR_PROPERTY = "_Color";
    private const string ALPHA_PROPERTY = "_Alpha";
    private const string FRESNEL_POWER_PROPERTY = "_FresnelPower";
    private const string FRESNEL_SCALE_PROPERTY = "_FresnelScale";
    private const string RIM_POWER_PROPERTY = "_RimPower";
    private const string RIM_INTENSITY_PROPERTY = "_RimIntensity";
    private const string PULSE_SPEED_PROPERTY = "_PulseSpeed";
    private const string PULSE_INTENSITY_PROPERTY = "_PulseIntensity";
    private const string NOISE_SCALE_PROPERTY = "_NoiseScale";
    private const string NOISE_SPEED_PROPERTY = "_NoiseSpeed";
    private const string DISTORTION_PROPERTY = "_Distortion";
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Создаем копию материала для этого объекта
        material = new Material(meshRenderer.material);
        meshRenderer.material = material;
        
        originalColor = color;
        
        // Применяем начальные значения
        UpdateShaderProperties();
    }
    
    void Update()
    {
        // Анимация цвета
        if (animateColor)
        {
            float t = (Mathf.Sin(Time.time * colorAnimationSpeed) + 1f) * 0.5f;
            color = Color.Lerp(originalColor, secondaryColor, t);
        }
        
        // Обновляем параметры шейдера
        UpdateShaderProperties();
    }
    
    void UpdateShaderProperties()
    {
        if (material == null) return;
        
        material.SetColor(COLOR_PROPERTY, color);
        material.SetFloat(ALPHA_PROPERTY, alpha);
        material.SetFloat(FRESNEL_POWER_PROPERTY, fresnelPower);
        material.SetFloat(FRESNEL_SCALE_PROPERTY, fresnelScale);
        material.SetFloat(RIM_POWER_PROPERTY, rimPower);
        material.SetFloat(RIM_INTENSITY_PROPERTY, rimIntensity);
        material.SetFloat(PULSE_SPEED_PROPERTY, pulseSpeed);
        material.SetFloat(PULSE_INTENSITY_PROPERTY, pulseIntensity);
        material.SetFloat(NOISE_SCALE_PROPERTY, noiseScale);
        material.SetFloat(NOISE_SPEED_PROPERTY, noiseSpeed);
        material.SetFloat(DISTORTION_PROPERTY, distortion);
    }
    
    // Методы для программного управления
    public void SetColor(Color newColor)
    {
        color = newColor;
        originalColor = newColor;
    }
    
    public void SetAlpha(float newAlpha)
    {
        alpha = Mathf.Clamp01(newAlpha);
    }
    
    public void SetPulseSpeed(float newSpeed)
    {
        pulseSpeed = Mathf.Max(0f, newSpeed);
    }
    
    public void SetPulseIntensity(float newIntensity)
    {
        pulseIntensity = Mathf.Clamp(newIntensity, 0f, 2f);
    }
    
    public void ActivateForceField()
    {
        gameObject.SetActive(true);
    }
    
    public void DeactivateForceField()
    {
        gameObject.SetActive(false);
    }
    
    public void SetIntensity(float intensity)
    {
        rimIntensity = Mathf.Max(0f, intensity);
        fresnelScale = Mathf.Max(0f, intensity);
    }
    
    void OnDestroy()
    {
        // Освобождаем память от созданного материала
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }
    
    // Метод для сброса к значениям по умолчанию
    [ContextMenu("Reset to Default")]
    public void ResetToDefault()
    {
        color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        alpha = 0.5f;
        fresnelPower = 2.0f;
        fresnelScale = 1.0f;
        rimPower = 3.0f;
        rimIntensity = 2.0f;
        pulseSpeed = 1.0f;
        pulseIntensity = 0.3f;
        noiseScale = 1.0f;
        noiseSpeed = 0.5f;
        distortion = 0.02f;
        animateColor = false;
        
        UpdateShaderProperties();
    }
} 