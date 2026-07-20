using UnityEngine;

/// <summary>
/// Stylized (non-photoreal) one-shot explosion: flash, shockwave ring, sparks, smoke.
/// Built at runtime from layered ParticleSystems — no VFX Graph required.
/// </summary>
public static class SurvivorExplosionFX
{
    private const float AuthoringRadius = 3f;

    public static void Play(Vector3 position, float blastRadius, Color tint)
    {
        float scale = Mathf.Clamp(blastRadius / AuthoringRadius, 0.35f, 3.5f);

        GameObject root = new GameObject("SurvivorExplosionFX");
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        BuildFlash(root.transform, tint);
        BuildRing(root.transform, tint);
        BuildSparks(root.transform, tint);
        BuildSmoke(root.transform, tint);

        Object.Destroy(root, 2.2f);
    }

    private static void BuildFlash(Transform parent, Color tint)
    {
        ParticleSystem ps = CreateSystem(parent, "Flash");
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.12f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
        main.startColor = new Color(1f, 1f, 0.85f, 0.95f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0.35f),
                new GradientColorKey(tint, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.4f));

        SetAdditive(ps);
        ps.Play(true);
    }

    private static void BuildRing(Transform parent, Color tint)
    {
        ParticleSystem ps = CreateSystem(parent, "Ring");
        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = 0.28f;
        main.startSpeed = 6f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new Color(tint.r, tint.g, tint.b, 0.85f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startRotation3D = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;
        shape.radiusThickness = 0f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        SetAdditive(ps);
        ps.Play(true);
    }

    private static void BuildSparks(Transform parent, Color tint)
    {
        ParticleSystem ps = CreateSystem(parent, "Sparks");
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(1f, 0.85f, 0.35f, 1f);
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(18f)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.5f),
                new GradientColorKey(tint, 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        var trails = ps.trails;
        trails.enabled = true;
        trails.lifetime = 0.15f;
        trails.ratio = 0.6f;
        trails.widthOverTrail = 0.4f;

        SetAdditive(ps);
        ps.Play(true);
    }

    private static void BuildSmoke(Transform parent, Color tint)
    {
        ParticleSystem ps = CreateSystem(parent, "Smoke");
        var main = ps.main;
        main.duration = 0.8f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        Color smoke = Color.Lerp(tint, new Color(0.35f, 0.2f, 0.45f), 0.45f);
        smoke.a = 0.55f;
        main.startColor = smoke;
        main.gravityModifier = -0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(smoke, 0f), new GradientColorKey(tint * 0.5f, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));

        ps.Play(true);
    }

    private static ParticleSystem CreateSystem(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<ParticleSystem>();
    }

    private static void SetAdditive(ParticleSystem ps)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            return;

        Material mat = new Material(shader);
        if (mat.HasProperty("_Mode"))
            mat.SetFloat("_Mode", 1f); // Additive-ish when supported
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
