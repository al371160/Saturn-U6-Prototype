using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shared helper for runtime URP transparent materials (aura fields, pools, shields).</summary>
public static class SurvivorTransparentMaterial
{
    public static Material Create(Color color, float alpha = 0.3f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        Color c = color;
        c.a = Mathf.Clamp01(alpha);
        ApplyTransparent(mat, c);
        return mat;
    }

    public static void ApplyTransparent(Material mat, Color color)
    {
        if (mat == null)
            return;

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        // URP Lit surface type = Transparent
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }
}
