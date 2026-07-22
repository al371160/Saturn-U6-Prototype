using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Lightweight Blit-based fallback for the storm desaturation effect, for use if
/// SurvivorStormDesaturateFeature isn't (or can't be) added to the active URP Renderer Data asset.
/// Attach to the main camera. Note: OnRenderImage is only invoked for cameras rendering through the
/// legacy Built-in Render Pipeline — Unity's Scriptable Render Pipelines (URP/HDRP) never call it, so
/// under this project's URP setup this component intentionally never fires (SurvivorStormDesaturateFeature
/// is the effective path); it exists purely as a defensive fallback if the pipeline is ever switched
/// back to Built-in. Uses Assets/Shaders/SurvivorStormDesaturateLegacyBlit.shader, a Built-in-pipeline
/// counterpart to the URP renderer feature's shader.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SurvivorStormDesaturateCameraFallback : MonoBehaviour
{
    public Shader shader;
    [Tooltip("World-space distance over which the desaturation fades in at the storm boundary.")]
    public float edgeSoftness = 25f;
    [Range(0f, 1f)]
    public float desaturateAmount = 1f;

    private static readonly int InvViewProjectionId = Shader.PropertyToID("_StormInvViewProjection");
    private static readonly int StormCenterId = Shader.PropertyToID("_StormCenter");
    private static readonly int StormRadiusId = Shader.PropertyToID("_StormRadius");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int DesaturateAmountId = Shader.PropertyToID("_DesaturateAmount");

    private Material material;
    private Camera targetCamera;

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SurvivorStormController stormController = SurvivorStormController.Instance;
        if (stormController == null || !EnsureMaterial())
        {
            Graphics.Blit(source, destination);
            return;
        }

        Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(targetCamera.projectionMatrix, false);
        Matrix4x4 viewProjection = gpuProjection * targetCamera.worldToCameraMatrix;
        material.SetMatrix(InvViewProjectionId, viewProjection.inverse);

        Vector3 center = stormController.CenterPosition;
        material.SetVector(StormCenterId, new Vector4(center.x, center.y, center.z, 0f));
        material.SetFloat(StormRadiusId, stormController.CurrentRadius);
        material.SetFloat(EdgeSoftnessId, Mathf.Max(0.01f, edgeSoftness));
        material.SetFloat(DesaturateAmountId, Mathf.Clamp01(desaturateAmount));

        Graphics.Blit(source, destination, material);
    }

    private bool EnsureMaterial()
    {
        if (material != null)
            return true;

        if (shader == null)
            shader = Shader.Find("Saturn/Survivor/StormDesaturateLegacyBlit");
        if (shader == null)
            return false;

        material = CoreUtils.CreateEngineMaterial(shader);
        return material != null;
    }

    private void OnDisable()
    {
        CoreUtils.Destroy(material);
        material = null;
    }
}
