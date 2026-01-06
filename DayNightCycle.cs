using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float dayDurationMinutes = 10f;
    [SerializeField] [Range(0f, 24f)] private float currentTime = 12f;
    
    [Header("Sun and Moon")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;
    [SerializeField] private Transform sunTransform;
    [SerializeField] private Transform moonTransform;
    
    [Header("Lighting Colors")]
    [SerializeField] private Gradient sunColor;
    [SerializeField] private Gradient moonColor;
    [SerializeField] private AnimationCurve lightIntensityCurve;
    [SerializeField] private float maxSunIntensity = 1.5f;
    [SerializeField] private float maxMoonIntensity = 0.3f;
    
    [Header("Ambient and Fog")]
    [SerializeField] private Gradient ambientColor;
    [SerializeField] private Gradient fogColor;
    [SerializeField] private AnimationCurve fogDensityCurve;
    [SerializeField] private float maxFogDensity = 0.01f;
    
    [Header("Skybox")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Gradient skyTintColor;
    [SerializeField] private AnimationCurve skyExposureCurve;
    [SerializeField] private float maxSkyExposure = 1.5f;
    
    [Header("Torch Support")]
    [SerializeField] private float minAmbientIntensity = 0.1f; // Prevents complete darkness for torches
    
    private float timeScale;
    
    void Start()
    {
        timeScale = 24f / (dayDurationMinutes * 60f);
        InitializeDefaultGradients();
        RenderSettings.fog = true;
        
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;
    }
    
    void Update()
    {
        currentTime += Time.deltaTime * timeScale;
        if (currentTime >= 24f)
            currentTime = 0f;
        
        UpdateSunAndMoon();
        UpdateLighting();
        UpdateAmbientAndFog();
        UpdateSkybox();
    }
    
    void UpdateSunAndMoon()
    {
        float sunAngle = (currentTime / 24f) * 360f - 90f;
        
        if (sunTransform != null)
            sunTransform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        
        if (moonTransform != null)
            moonTransform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
    }
    
    void UpdateLighting()
    {
        float timePercent = currentTime / 24f;
        
        // Calculate sun and moon blend based on their angle
        float sunDot = Vector3.Dot(sunTransform.forward, Vector3.down);
        float moonDot = Vector3.Dot(moonTransform.forward, Vector3.down);
        
        // Smooth blend: sun is active when above horizon, moon when sun is below
        float sunBlend = Mathf.Clamp01(sunDot);
        float moonBlend = Mathf.Clamp01(moonDot) * (1f - sunBlend);
        
        // Update sun
        if (sunLight != null)
        {
            sunLight.color = sunColor.Evaluate(timePercent);
            float targetIntensity = lightIntensityCurve.Evaluate(timePercent) * maxSunIntensity;
            sunLight.intensity = targetIntensity * sunBlend;
            sunLight.enabled = sunBlend > 0.01f; // Only disable when fully gone
        }
        
        // Update moon
        if (moonLight != null)
        {
            float moonTimePercent = (timePercent + 0.5f) % 1f;
            moonLight.color = moonColor.Evaluate(moonTimePercent);
            float targetIntensity = lightIntensityCurve.Evaluate(moonTimePercent) * maxMoonIntensity;
            moonLight.intensity = targetIntensity * moonBlend;
            moonLight.enabled = moonBlend > 0.01f;
        }
    }
    
    void UpdateAmbientAndFog()
    {
        float timePercent = currentTime / 24f;
        
        // Update ambient lighting with minimum floor for torch visibility
        Color targetAmbient = ambientColor.Evaluate(timePercent);
        float ambientIntensity = Mathf.Max(targetAmbient.grayscale, minAmbientIntensity);
        RenderSettings.ambientLight = targetAmbient * (ambientIntensity / Mathf.Max(targetAmbient.grayscale, 0.01f));
        
        // Update fog
        RenderSettings.fogColor = fogColor.Evaluate(timePercent);
        RenderSettings.fogDensity = fogDensityCurve.Evaluate(timePercent) * maxFogDensity;
    }
    
    void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;
        
        float timePercent = currentTime / 24f;
        
        // Update skybox tint color
        if (skyboxMaterial.HasProperty("_Tint"))
        {
            skyboxMaterial.SetColor("_Tint", skyTintColor.Evaluate(timePercent));
        }
        
        // Update skybox exposure
        if (skyboxMaterial.HasProperty("_Exposure"))
        {
            float exposure = skyExposureCurve.Evaluate(timePercent) * maxSkyExposure;
            skyboxMaterial.SetFloat("_Exposure", exposure);
        }
        
        // Update sun size/convergence for procedural skybox
        if (skyboxMaterial.HasProperty("_SunSize"))
        {
            skyboxMaterial.SetFloat("_SunSize", 0.04f);
        }
        
        // Rotate skybox if it has rotation property
        if (skyboxMaterial.HasProperty("_Rotation"))
        {
            skyboxMaterial.SetFloat("_Rotation", currentTime * 15f); // 15 degrees per hour
        }
        
        DynamicGI.UpdateEnvironment();
    }
    
    void InitializeDefaultGradients()
    {
        // Sun color gradient
        if (sunColor.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.95f, 0.9f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            sunColor.SetKeys(colorKeys, alphaKeys);
        }
        
        // Moon color
        if (moonColor.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.6f, 0.7f, 1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.6f, 0.7f, 1f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            moonColor.SetKeys(colorKeys, alphaKeys);
        }
        
        // Ambient color
        if (ambientColor.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.4f, 0.4f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(0.5f, 0.3f, 0.3f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            ambientColor.SetKeys(colorKeys, alphaKeys);
        }
        
        // Fog color
        if (fogColor.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(0.6f, 0.7f, 0.8f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(0.8f, 0.5f, 0.4f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            fogColor.SetKeys(colorKeys, alphaKeys);
        }
        
        // Skybox tint color
        if (skyTintColor.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f);    // Night blue
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.25f);   // Dawn orange
            colorKeys[2] = new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f);    // Day blue
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.4f, 0.2f), 0.75f);   // Dusk red
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f);    // Night blue
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            skyTintColor.SetKeys(colorKeys, alphaKeys);
        }
        
        // Light intensity curve
        if (lightIntensityCurve.keys.Length == 0)
        {
            lightIntensityCurve = new AnimationCurve();
            lightIntensityCurve.AddKey(new Keyframe(0f, 0.1f, 0f, 0f));      // Midnight
            lightIntensityCurve.AddKey(new Keyframe(0.2f, 0.5f, 2f, 2f));    // Pre-dawn
            lightIntensityCurve.AddKey(new Keyframe(0.3f, 1.2f, 0f, 0f));    // Dawn
            lightIntensityCurve.AddKey(new Keyframe(0.5f, 1.5f, 0f, 0f));    // Noon
            lightIntensityCurve.AddKey(new Keyframe(0.7f, 1.2f, 0f, 0f));    // Dusk
            lightIntensityCurve.AddKey(new Keyframe(0.8f, 0.5f, -2f, -2f));  // Post-dusk
            lightIntensityCurve.AddKey(new Keyframe(1f, 0.1f, 0f, 0f));      // Midnight
        }
        
        // Fog density curve
        if (fogDensityCurve.keys.Length == 0)
        {
            fogDensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
            fogDensityCurve.AddKey(0.25f, 1.5f);
            fogDensityCurve.AddKey(0.5f, 0.5f);
            fogDensityCurve.AddKey(0.75f, 1.5f);
        }
        
        // Sky exposure curve
        if (skyExposureCurve.keys.Length == 0)
        {
            skyExposureCurve = new AnimationCurve();
            skyExposureCurve.AddKey(new Keyframe(0f, 0.3f));     // Dark night
            skyExposureCurve.AddKey(new Keyframe(0.25f, 0.8f));  // Dawn
            skyExposureCurve.AddKey(new Keyframe(0.5f, 1.5f));   // Bright day
            skyExposureCurve.AddKey(new Keyframe(0.75f, 0.8f));  // Dusk
            skyExposureCurve.AddKey(new Keyframe(1f, 0.3f));     // Dark night
        }
    }
    
    public float GetCurrentTime() => currentTime;
    public void SetCurrentTime(float time) => currentTime = Mathf.Clamp(time, 0f, 24f);
    public bool IsDaytime() => currentTime >= 6f && currentTime < 18f;
}