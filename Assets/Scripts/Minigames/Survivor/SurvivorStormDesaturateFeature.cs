using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP renderer feature for the storm-to-center mode's "outside the ring is black &amp; white"
/// presentation. Add this to the active ScriptableRendererData asset(s) (Project Settings ▸
/// Graphics ▸ Renderer List, e.g. PC_Renderer/Mobile_Renderer) to enable it. Every frame it reads
/// center/radius from SurvivorStormController.Instance and drives SurvivorStormDesaturatePass — the
/// pass no-ops entirely when no storm controller is active, so this feature is safe to leave enabled
/// in every scene/renderer.
/// </summary>
public class SurvivorStormDesaturateFeature : ScriptableRendererFeature
{
    public Shader shader;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    [Tooltip("How hard saturation drops when the player is in the storm (1 = full B&W).")]
    [Range(0f, 1f)]
    public float desaturateAmount = 0.85f;

    private Material material;
    private SurvivorStormDesaturatePass pass;

    public override void Create()
    {
        if (shader == null)
            shader = Shader.Find("Saturn/Survivor/StormDesaturate");

        if (material == null && shader != null)
            material = CoreUtils.CreateEngineMaterial(shader);

        pass = new SurvivorStormDesaturatePass(material)
        {
            renderPassEvent = renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null)
            return;

        SurvivorStormController stormController = SurvivorStormController.Instance;
        if (stormController == null)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        pass.Setup(stormController, desaturateAmount);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
    }
}
