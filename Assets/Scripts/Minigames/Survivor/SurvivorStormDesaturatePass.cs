using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Full-screen desaturation when the player is in the storm (outside the safe radius).
/// </summary>
public class SurvivorStormDesaturatePass : ScriptableRenderPass
{
    private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
    private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
    private static readonly int PlayerInStormId = Shader.PropertyToID("_PlayerInStorm");
    private static readonly int DesaturateAmountId = Shader.PropertyToID("_DesaturateAmount");

    private static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

    private readonly Material material;
    private float playerInStorm;
    private float desaturateAmount;
    private bool active;

    public SurvivorStormDesaturatePass(Material passMaterial)
    {
        material = passMaterial;
        profilingSampler = new ProfilingSampler("SurvivorStormDesaturate");
    }

    public void Setup(SurvivorStormController stormController, float desaturateStrength)
    {
        active = false;
        playerInStorm = 0f;
        desaturateAmount = Mathf.Clamp01(desaturateStrength);

        if (stormController == null)
            return;

        SurvivorMinigamePlayer player = stormController.player;
        if (player == null)
            player = Object.FindFirstObjectByType<SurvivorMinigamePlayer>();
        if (player == null)
            return;

        Vector3 flatPlayer = player.transform.position;
        flatPlayer.y = 0f;
        Vector3 flatCenter = stormController.CenterPosition;
        flatCenter.y = 0f;
        float distance = Vector3.Distance(flatPlayer, flatCenter);
        float radius = Mathf.Max(0.01f, stormController.CurrentRadius);

        // Soft fade over the first ~8m outside the safe radius.
        float soft = 8f;
        playerInStorm = distance <= radius ? 0f : Mathf.Clamp01((distance - radius) / soft);
        active = playerInStorm > 0.001f;
    }

    private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
    {
        Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
    }

    private void ExecuteDesaturatePass(RasterCommandBuffer cmd, RTHandle colorCopy)
    {
        SharedPropertyBlock.Clear();
        if (colorCopy != null)
            SharedPropertyBlock.SetTexture(BlitTextureId, colorCopy);
        SharedPropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1, 1, 0, 0));
        SharedPropertyBlock.SetFloat(PlayerInStormId, playerInStorm);
        SharedPropertyBlock.SetFloat(DesaturateAmountId, desaturateAmount);

        cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, SharedPropertyBlock);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!active || material == null)
            return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
            return;

        if (!resourceData.cameraColor.IsValid())
            return;

        TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        colorCopyDesc.name = "_SurvivorStormColorCopy";
        colorCopyDesc.clearBuffer = false;

        TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);

        using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Survivor Storm Color Copy", out CopyPassData copyPassData, profilingSampler))
        {
            copyPassData.source = resourceData.activeColorTexture;
            builder.UseTexture(copyPassData.source, AccessFlags.Read);
            builder.SetRenderAttachment(colorCopy, 0, AccessFlags.Write);
            builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyColorPass(context.cmd, data.source));
        }

        using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("Survivor Storm Desaturate", out MainPassData passData, profilingSampler))
        {
            passData.pass = this;
            passData.colorCopy = colorCopy;
            builder.UseTexture(passData.colorCopy, AccessFlags.Read);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => data.pass.ExecuteDesaturatePass(context.cmd, data.colorCopy));
        }
    }

    private class CopyPassData
    {
        internal TextureHandle source;
    }

    private class MainPassData
    {
        internal SurvivorStormDesaturatePass pass;
        internal TextureHandle colorCopy;
    }
}
