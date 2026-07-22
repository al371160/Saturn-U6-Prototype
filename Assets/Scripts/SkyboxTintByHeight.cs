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

        currentSkyTint = Color.Lerp(currentSkyTint, targetSkyTint, Time.deltaTime * fadeSpeed);
        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime * fadeSpeed);

        sharedSkyboxMaterial.SetColor("_Tint", currentSkyTint);
        RenderSettings.fogColor = currentFogColor;
    }
}
