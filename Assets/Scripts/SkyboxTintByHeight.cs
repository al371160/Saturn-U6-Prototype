using UnityEngine;

public class SkyboxTintByHeight : MonoBehaviour
{
    public Transform player;
    public Material sharedSkyboxMaterial;

    [Header("Height Zones")]
    public float groundMaxHeight = 20f;
    public float iceMaxHeight = 60f;

    [Header("Skybox Tints")]
    public Color groundSkyTint = new Color(1f, 0.95f, 0.8f);
    public Color iceSkyTint = Color.white;
    public Color sunsetSkyTint = new Color(0.5f, 0.3f, 1f);

    [Header("Fog Colors")]
    public Color groundFogColor = new Color(0.85f, 0.8f, 0.75f);
    public Color iceFogColor = new Color(0.9f, 0.95f, 1f);
    public Color sunsetFogColor = new Color(0.6f, 0.4f, 0.7f);

    [Header("Oxygen Danger Tint")]
    [Tooltip("Sky tint blended in when oxygen is critically low.")]
    public Color oxygenDangerSkyTint = new Color(0.6f, 0.05f, 0.05f);
    public Color oxygenDangerFogColor = new Color(0.5f, 0.1f, 0.1f);
    [Tooltip("Oxygen fraction at which the danger tint starts fading in.")]
    public float oxygenDangerThreshold = 0.4f;

    [Header("Transition")]
    public float fadeSpeed = 2f;

    private Color currentSkyTint;
    private Color currentFogColor;

    void Start()
    {
        RenderSettings.skybox = sharedSkyboxMaterial;
        currentSkyTint = sharedSkyboxMaterial.GetColor("_Tint");
        currentFogColor = RenderSettings.fogColor;
    }

    void Update()
    {
        float height = player.position.y;

        Color targetSkyTint, targetFogColor;

        if (height <= groundMaxHeight)
        {
            targetSkyTint = groundSkyTint;
            targetFogColor = groundFogColor;
        }
        else if (height <= iceMaxHeight)
        {
            float t = Mathf.InverseLerp(groundMaxHeight, iceMaxHeight, height);
            targetSkyTint = Color.Lerp(groundSkyTint, iceSkyTint, t);
            targetFogColor = Color.Lerp(groundFogColor, iceFogColor, t);
        }
        else
        {
            float t = Mathf.InverseLerp(iceMaxHeight, iceMaxHeight + 30f, height);
            targetSkyTint = Color.Lerp(iceSkyTint, sunsetSkyTint, t);
            targetFogColor = Color.Lerp(iceFogColor, sunsetFogColor, t);
        }

        // Blend in oxygen danger tint when oxygen is low
        if (OxygenSystem.Instance != null)
        {
            float oxyFraction = OxygenSystem.Instance.OxygenFraction;
            if (oxyFraction < oxygenDangerThreshold)
            {
                float dangerT = 1f - (oxyFraction / oxygenDangerThreshold);
                targetSkyTint = Color.Lerp(targetSkyTint, oxygenDangerSkyTint, dangerT);
                targetFogColor = Color.Lerp(targetFogColor, oxygenDangerFogColor, dangerT);
            }
        }

        currentSkyTint = Color.Lerp(currentSkyTint, targetSkyTint, Time.deltaTime * fadeSpeed);
        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime * fadeSpeed);

        sharedSkyboxMaterial.SetColor("_Tint", currentSkyTint);
        RenderSettings.fogColor = currentFogColor;
    }
}
