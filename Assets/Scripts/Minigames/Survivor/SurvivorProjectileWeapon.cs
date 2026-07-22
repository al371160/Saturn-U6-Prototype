using System.Collections.Generic;
using UnityEngine;

public class SurvivorProjectileWeapon : SurvivorWeaponBehavior
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

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
            return;

        SurvivorWeaponStarStats stats = data.GetStats(starLevel);
        float rateMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RateMultiplier : 1f;
        fireTimer = Mathf.Max(0.05f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        FireVolley(stats);
    }

    private void FireVolley(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget();
        if (!HasAimSolution(target))
            return;

        PlayFireSfx();

        Vector3 baseDirection = ResolveFlatAimDirection(target);

        int shots = Mathf.Max(1, stats.count);
        float spreadStep = shots > 1 ? 12f : 0f;
        float startAngleOffset = -spreadStep * (shots - 1) * 0.5f;

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;

        for (int i = 0; i < shots; i++)
        {
            float angle = startAngleOffset + spreadStep * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            SpawnProjectile(direction, stats.damage * damageMultiplier, stats.range * rangeMultiplier);
        }
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

    private void SpawnProjectile(Vector3 direction, float damage, float speed)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorProjectile";
        projectileObject.transform.position = GetProjectileSpawnPosition();
        projectileObject.transform.localScale = Vector3.one * data.hitRadius * 1.6f;

        Collider col = projectileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = data.weaponColor;

        projectileObject.AddComponent<SurvivorProjectile>().Launch(direction, speed, damage, data.element, data.knockbackForce);
    }
}

public class SurvivorProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private SurvivorElementType element;
    private float knockbackForce;
    private float lifetime = 2.5f;
    private float hitRadius = 0.2f;

    public void Launch(Vector3 travelDirection, float travelSpeed, float hitDamage, SurvivorElementType hitElement = SurvivorElementType.None, float force = 0f)
    {
        direction = travelDirection;
        speed = travelSpeed;
        damage = hitDamage;
        element = hitElement;
        knockbackForce = force;
        hitRadius = Mathf.Max(0.15f, transform.localScale.x * 0.5f);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 start = transform.position;

        // Sweep so fast shots don't tunnel through crate colliders.
        if (Physics.SphereCast(start, hitRadius, direction, out RaycastHit sweepHit, step, ~0, QueryTriggerInteraction.Collide)
            && sweepHit.collider.GetComponentInParent<ISurvivorDamageable>() != null)
        {
            ApplyHit(sweepHit.collider.gameObject);
            return;
        }

        transform.position = start + direction * step;

        if (SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, hitRadius, out Collider overlapHit))
        {
            ApplyHit(overlapHit.gameObject);
            return;
        }

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void ApplyHit(GameObject hitObject)
    {
        SurvivorCombatFX.ApplyHit(hitObject, damage, element, direction, knockbackForce);
        Destroy(gameObject);
    }
}
