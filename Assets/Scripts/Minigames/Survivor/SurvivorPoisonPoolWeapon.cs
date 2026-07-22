using System.Collections.Generic;
using UnityEngine;

public class SurvivorPoisonPoolWeapon : SurvivorWeaponBehavior
{
    private float fireTimer;
    private readonly List<SurvivorMinigameEnemy> enemyBuffer = new List<SurvivorMinigameEnemy>();
    private readonly List<SurvivorBossEnemy> bossBuffer = new List<SurvivorBossEnemy>();

    protected override void OnInitialize()
    {
        fireTimer = 0f;
    }

    protected override void OnStarLevelChanged()
    {
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || !CanFire())
            return;

        SurvivorWeaponStarStats stats = data.GetStats(starLevel);
        float rateMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RateMultiplier : 1f;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
            return;

        fireTimer = Mathf.Max(0.3f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        PlayFireSfx();
        Fire(stats);
    }

    private void Fire(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget();
        Vector3 direction = ResolveFlatAimDirection(target);

        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;

        float travelDistance;
        if (HasCursorAim(out _) && TryGetCursorWorldPoint(out Vector3 cursorPoint))
        {
            Vector3 flat = cursorPoint - transform.position;
            flat.y = 0f;
            travelDistance = flat.magnitude;
        }
        else if (target != null)
        {
            travelDistance = Vector3.Distance(transform.position, target.position);
        }
        else
        {
            travelDistance = Mathf.Max(6f, stats.range * 0.65f);
        }

        travelDistance = Mathf.Clamp(travelDistance, 3f, Mathf.Max(8f, stats.range * rangeMultiplier));

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorPoisonFlask";
        projectileObject.transform.position = GetProjectileSpawnPosition();
        projectileObject.transform.localScale = Vector3.one * data.hitRadius * 1.4f;

        Collider col = projectileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = data.weaponColor;

        float poolRadius = Mathf.Max(1.6f, stats.range * rangeMultiplier * 0.35f);
        float poolDuration = Mathf.Max(2.5f, stats.secondaryValue);
        projectileObject.AddComponent<SurvivorPoisonFlask>().Launch(
            direction,
            10f,
            travelDistance,
            stats.damage * damageMultiplier,
            poolRadius,
            poolDuration);
    }

    private Transform FindNearestTarget()
    {
        if (controller == null || controller.enemyRoot == null)
            return null;

        enemyBuffer.Clear();
        controller.enemyRoot.GetComponentsInChildren(false, enemyBuffer);
        bossBuffer.Clear();
        controller.enemyRoot.GetComponentsInChildren(false, bossBuffer);

        Transform nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < enemyBuffer.Count; i++)
            ConsiderCandidate(enemyBuffer[i].transform, ref nearest, ref nearestSqrDistance);
        for (int i = 0; i < bossBuffer.Count; i++)
            ConsiderCandidate(bossBuffer[i].transform, ref nearest, ref nearestSqrDistance);

        return nearest;
    }

    private void ConsiderCandidate(Transform candidate, ref Transform nearest, ref float nearestSqrDistance)
    {
        float sqrDistance = (candidate.position - transform.position).sqrMagnitude;
        if (sqrDistance < nearestSqrDistance)
        {
            nearestSqrDistance = sqrDistance;
            nearest = candidate;
        }
    }
}

/// <summary>Flies to its landing point, then leaves a lingering poison pool on the ground.</summary>
public class SurvivorPoisonFlask : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float travelDistance;
    private float traveled;
    private float damage;
    private float poolRadius;
    private float poolDuration;
    private bool landed;

    public void Launch(Vector3 travelDirection, float travelSpeed, float distance, float hitDamage, float radius, float duration)
    {
        direction = travelDirection;
        speed = travelSpeed;
        travelDistance = distance;
        damage = hitDamage;
        poolRadius = radius;
        poolDuration = duration;
    }

    private void Update()
    {
        if (landed)
            return;

        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        traveled += step;

        if (SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, Mathf.Max(0.2f, transform.localScale.x * 0.5f), out Collider hit)
            && hit.GetComponentInParent<SurvivorMinigamePlayer>() == null)
        {
            Land();
            return;
        }

        if (traveled >= travelDistance)
            Land();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (landed)
            return;

        // Only early-land on solid damageables / ground-ish colliders — not the shooter.
        if (other.GetComponentInParent<SurvivorMinigamePlayer>() != null)
            return;

        if (other.GetComponentInParent<ISurvivorDamageable>() != null)
            Land();
    }

    private void Land()
    {
        if (landed)
            return;
        landed = true;

        SurvivorMinigameController owner = Object.FindFirstObjectByType<SurvivorMinigameController>();
        LayerMask groundMask = owner != null && owner.config != null ? owner.config.groundMask : ~0;
        float rayHeight = owner != null && owner.config != null ? owner.config.groundSnapRayHeight : 50f;
        Vector3 grounded = SurvivorGroundUtility.SnapToGround(transform.position, groundMask, rayHeight, 0.05f);

        GameObject poolObject = new GameObject("SurvivorPoisonPool");
        poolObject.transform.position = grounded;
        poolObject.AddComponent<SurvivorPoisonPoolZone>().Initialize(damage, poolRadius, poolDuration, groundMask, rayHeight);
        Destroy(gameObject);
    }
}

public class SurvivorPoisonPoolZone : MonoBehaviour
{
    private float tickDamage;
    private float radius;
    private float remainingDuration;
    private float tickTimer;
    private const float TickInterval = 0.45f;
    private GameObject visual;
    private ParticleSystem particles;
    private LayerMask groundMask;
    private float groundRayHeight;
    private float groundSnapTimer;

    public void Initialize(float damagePerTick, float poolRadius, float duration, LayerMask groundLayerMask, float rayHeight)
    {
        tickDamage = Mathf.Max(1f, damagePerTick * 0.45f);
        radius = Mathf.Max(1.4f, poolRadius);
        remainingDuration = Mathf.Max(2f, duration);
        tickTimer = 0.15f;
        groundMask = groundLayerMask;
        groundRayHeight = rayHeight;
        groundSnapTimer = 0.35f;

        visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "PoisonPoolVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.up * 0.03f;
        visual.transform.localScale = new Vector3(radius * 2f, 0.08f, radius * 2f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = SurvivorTransparentMaterial.Create(new Color(0.3f, 0.95f, 0.2f), 0.55f);

        BuildParticles();
    }

    private void BuildParticles()
    {
        GameObject particleObject = new GameObject("PoisonPoolParticles");
        particleObject.transform.SetParent(transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
        main.startColor = new Color(0.45f, 1f, 0.3f, 0.8f);
        main.gravityModifier = -0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = Mathf.Clamp(radius * 5f, 12f, 36f);

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.35f, radius * 0.9f);
        shape.radiusThickness = 1f;

        var color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 1f, 0.35f), 0f),
                new GradientColorKey(new Color(0.15f, 0.65f, 0.12f), 1f)
            },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        particles.Play();
    }

    private void Update()
    {
        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f)
        {
            if (particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject);
            return;
        }

        groundSnapTimer -= Time.deltaTime;
        if (groundSnapTimer <= 0f)
        {
            groundSnapTimer = 0.5f;
            transform.position = SurvivorGroundUtility.SnapToGround(transform.position, groundMask, groundRayHeight, 0.05f);
        }

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = TickInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.2f, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            ISurvivorDamageable damageable = hits[i].GetComponentInParent<ISurvivorDamageable>();
            if (damageable == null)
                continue;

            Component component = damageable as Component;
            if (component == null)
                continue;

            SurvivorCombatFX.ApplyHit(component.gameObject, tickDamage, SurvivorElementType.Poison, Vector3.zero, 0f);
        }
    }
}
