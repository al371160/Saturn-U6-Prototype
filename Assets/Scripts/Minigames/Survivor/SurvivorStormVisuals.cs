using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Presentation layer for the closing storm ring: a tall translucent wall that tracks
/// SurvivorStormController.CenterPosition/CurrentRadius every frame, plus an ash ParticleSystem
/// that only emits while the player stands outside the safe zone. Built procedurally at runtime
/// (no prefab dependency), matching SurvivorExplosionFX/SurvivorStormObjective's authoring style.
/// Created automatically by SurvivorStormController.EnsureVisuals() if the scene doesn't already
/// have one.
/// </summary>
public class SurvivorStormVisuals : MonoBehaviour
{
    [Header("Wall")]
    public float wallHeight = 400f;
    [Tooltip("Multiplies the wall's diameter slightly past the storm's exact radius so the boundary reads clearly without clipping the player.")]
    public float wallRadiusPadding = 1.02f;
    public Color wallColor = new Color(0f, 0f, 0f, 0.45f);

    [Header("Ash")]
    public Color ashColor = new Color(0.16f, 0.14f, 0.13f, 0.85f);
    public float ashAreaSize = 14f;
    public float ashSpawnHeight = 6f;

    private const float WallUndergroundDepth = 50f;

    private SurvivorStormController stormController;
    private Transform playerTransform;
    private Transform wallTransform;
    private ParticleSystem ashParticles;

    public void Initialize(SurvivorStormController controller)
    {
        stormController = controller;
        BuildWallIfMissing();
        BuildAshParticlesIfMissing();
    }

    private void Update()
    {
        if (stormController == null)
            return;

        if (playerTransform == null)
        {
            SurvivorMinigamePlayer player = stormController.player != null ? stormController.player : FindFirstObjectByType<SurvivorMinigamePlayer>();
            if (player != null)
                playerTransform = player.transform;
        }

        UpdateWall();
        UpdateAshParticles();
    }

    private void UpdateWall()
    {
        if (wallTransform == null)
            return;

        Vector3 center = stormController.CenterPosition;
        // Tube mesh has unit radius and spans Y [-1, 1], so XZ scale = radius and Y scale = half height.
        float radius = Mathf.Max(0.5f, stormController.CurrentRadius * wallRadiusPadding);
        wallTransform.position = new Vector3(center.x, wallHeight * 0.5f - WallUndergroundDepth, center.z);
        wallTransform.localScale = new Vector3(radius, wallHeight * 0.5f, radius);
    }

    private void UpdateAshParticles()
    {
        if (ashParticles == null || playerTransform == null)
            return;

        if (ashParticles.transform.parent != playerTransform)
        {
            ashParticles.transform.SetParent(playerTransform, false);
            ashParticles.transform.localPosition = Vector3.zero;
        }

        bool inStorm = IsOutsideSafeRadius(playerTransform.position);
        if (inStorm && !ashParticles.isEmitting)
            ashParticles.Play(true);
        else if (!inStorm && ashParticles.isEmitting)
            ashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private bool IsOutsideSafeRadius(Vector3 worldPosition)
    {
        Vector3 flatPosition = worldPosition;
        flatPosition.y = 0f;
        Vector3 flatCenter = stormController.CenterPosition;
        flatCenter.y = 0f;
        return Vector3.Distance(flatPosition, flatCenter) > stormController.CurrentRadius;
    }

    private void BuildWallIfMissing()
    {
        if (wallTransform != null)
            return;

        // Hollow tube (not a solid filled cylinder) so the safe zone stays visible.
        GameObject wallObject = new GameObject("SurvivorStormWall");
        wallObject.transform.SetParent(transform, false);
        MeshFilter filter = wallObject.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildTubeMesh(64, 0.985f);
        MeshRenderer wallRenderer = wallObject.AddComponent<MeshRenderer>();
        wallRenderer.shadowCastingMode = ShadowCastingMode.Off;
        wallRenderer.receiveShadows = false;

        Material wallMaterial = SurvivorTransparentMaterial.Create(wallColor, wallColor.a);
        if (wallMaterial.HasProperty("_Cull"))
            wallMaterial.SetFloat("_Cull", (float)CullMode.Off);
        wallRenderer.material = wallMaterial;

        wallTransform = wallObject.transform;
    }

    /// <summary>Unit-radius tube mesh in local space; scale XZ to storm diameter, Y to height.</summary>
    private static Mesh BuildTubeMesh(int segments, float innerRadius)
    {
        segments = Mathf.Max(8, segments);
        innerRadius = Mathf.Clamp(innerRadius, 0.5f, 0.999f);

        Vector3[] vertices = new Vector3[segments * 4];
        int[] triangles = new int[segments * 12];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < segments; i++)
        {
            float t0 = (i / (float)segments) * Mathf.PI * 2f;
            float t1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 o0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0));
            Vector3 o1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1));
            Vector3 i0 = o0 * innerRadius;
            Vector3 i1 = o1 * innerRadius;

            int v = i * 4;
            vertices[v] = o0 + Vector3.up;
            vertices[v + 1] = o1 + Vector3.up;
            vertices[v + 2] = o1 - Vector3.up;
            vertices[v + 3] = o0 - Vector3.up;

            // Reuse verts for inner wall by offsetting into a second ring via scale — keep single outer shell
            // for performance; double-sided via Cull Off on the material.
            uvs[v] = new Vector2(i / (float)segments, 1f);
            uvs[v + 1] = new Vector2((i + 1) / (float)segments, 1f);
            uvs[v + 2] = new Vector2((i + 1) / (float)segments, 0f);
            uvs[v + 3] = new Vector2(i / (float)segments, 0f);

            int t = i * 12;
            // Outer face
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
            // Inner-facing (same verts, reversed winding — works with Cull Off)
            triangles[t + 6] = v;
            triangles[t + 7] = v + 2;
            triangles[t + 8] = v + 1;
            triangles[t + 9] = v;
            triangles[t + 10] = v + 3;
            triangles[t + 11] = v + 2;

            // Silence unused inner locals in case of older compiler settings
            _ = i0;
            _ = i1;
        }

        Mesh mesh = new Mesh { name = "SurvivorStormTube" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildAshParticlesIfMissing()
    {
        if (ashParticles != null)
            return;

        GameObject ashObject = new GameObject("SurvivorStormAsh");
        ashObject.SetActive(false);
        ashParticles = ashObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ashParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor = ashColor;
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        ParticleSystem.EmissionModule emission = ashParticles.emission;
        emission.rateOverTime = 60f;

        ParticleSystem.ShapeModule shape = ashParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(ashAreaSize, 0.5f, ashAreaSize);
        shape.position = new Vector3(0f, ashSpawnHeight, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ashParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ashParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(ashColor, 0f), new GradientColorKey(ashColor, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(ashColor.a, 0.15f),
                new GradientAlphaKey(ashColor.a, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ApplyAshRenderer(ashParticles);

        ashObject.SetActive(true);
        ashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void ApplyAshRenderer(ParticleSystem ps)
    {
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            return;

        Material material = new Material(shader);
        SurvivorTransparentMaterial.ApplyTransparent(material, Color.white);
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
