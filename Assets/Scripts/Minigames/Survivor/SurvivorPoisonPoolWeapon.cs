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
        Fire(stats);
    }

    private void Fire(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget();
        Vector3 direction = target != null ? (target.position - transform.position) : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;
        direction.Normalize();

        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float travelDistance = target != null ? Vector3.Distance(transform.position, target.position) : 8f;

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorPoisonFlask";
        projectileObject.transform.position = transform.position + Vector3.up * 0.6f;
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

        projectileObject.AddComponent<SurvivorPoisonFlask>().Launch(
            direction, 10f, travelDistance, stats.damage * damageMultiplier, stats.range * rangeMultiplier, Mathf.Max(1f, stats.secondaryValue));
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

/// <summary>Flies to its landing point, then leaves a lingering poison pool behind.</summary>
public class SurvivorPoisonFlask : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float travelDistance;
    private float traveled;
    private float damage;
    private float poolRadius;
    private float poolDuration;

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
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        traveled += step;

        if (traveled >= travelDistance)
            Land();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ISurvivorDamageable>() != null)
            Land();
    }

    private void Land()
    {
        GameObject poolObject = new GameObject("SurvivorPoisonPool");
        SurvivorMinigameController owner = Object.FindFirstObjectByType<SurvivorMinigameController>();
        LayerMask groundMask = owner != null && owner.config != null ? owner.config.groundMask : ~0;
        float rayHeight = owner != null && owner.config != null ? owner.config.groundSnapRayHeight : 50f;
        Vector3 grounded = SurvivorGroundUtility.SnapToGround(transform.position, groundMask, rayHeight, 0.02f);
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
    private const float TickInterval = 0.5f;
    private GameObject visual;
    private ParticleSystem particles;
    private LayerMask groundMask;
    private float groundRayHeight;
    private float groundSnapTimer;

    public void Initialize(float damagePerTick, float poolRadius, float duration, LayerMask groundLayerMask, float rayHeight)
    {
        tickDamage = damagePerTick * 0.4f;
        radius = poolRadius;
        remainingDuration = duration;
        tickTimer = TickInterval;
        groundMask = groundLayerMask;
        groundRayHeight = rayHeight;
        groundSnapTimer = 0.4f;

        visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "PoisonPoolVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = SurvivorTransparentMaterial.Create(new Color(0.35f, 0.9f, 0.25f), 0.45f);

        BuildParticles();
    }

    private void BuildParticles()
    {
        GameObject particleObject = new GameObject("PoisonPoolParticles");
        particleObject.transform.SetParent(transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(0.45f, 1f, 0.35f, 0.75f);
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = Mathf.Clamp(radius * 4f, 8f, 28f);

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.2f, radius * 0.85f);
        shape.radiusThickness = 1f;

        var color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.5f, 1f, 0.4f), 0f),
                new GradientColorKey(new Color(0.2f, 0.7f, 0.15f), 1f)
            },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
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
            transform.position = SurvivorGroundUtility.SnapToGround(transform.position, groundMask, groundRayHeight, 0.02f);
        }

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = TickInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            SurvivorCombatFX.ApplyHit(hitObject, tickDamage, SurvivorElementType.Poison, Vector3.zero, 0f);
        }
    }
}
