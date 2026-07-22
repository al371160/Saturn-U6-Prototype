using System.Collections.Generic;
using UnityEngine;

public class SurvivorHomingWeapon : SurvivorWeaponBehavior
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

        fireTimer = Mathf.Max(0.15f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        FireVolley(stats);
    }

    private void FireVolley(SurvivorWeaponStarStats stats)
    {
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        int shots = Mathf.Max(1, stats.count);

        bool cursorAim = HasCursorAim(out Vector3 cursorDir);
        bool anySpawned = false;

        for (int i = 0; i < shots; i++)
        {
            Transform target = cursorAim ? null : FindNearestTarget();
            if (!cursorAim && target == null)
                continue;

            Vector3 initialDirection = cursorAim
                ? cursorDir
                : ResolveFlatAimDirection(target);

            anySpawned = true;
            SpawnMissile(target, initialDirection, stats.damage * damageMultiplier, stats.range * rangeMultiplier);
        }

        if (anySpawned)
            PlayFireSfx();
    }

    private void SpawnMissile(Transform target, Vector3 initialDirection, float damage, float speed)
    {
        GameObject missileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        missileObject.name = "SurvivorHomingMissile";
        missileObject.transform.position = GetProjectileSpawnPosition();
        missileObject.transform.localScale = Vector3.one * data.hitRadius * 1.5f;

        Collider col = missileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = missileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = missileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = data.weaponColor;

        missileObject.AddComponent<SurvivorHomingMissile>().Launch(target, initialDirection, speed, damage, data.element, data.knockbackForce);
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

public class SurvivorHomingMissile : MonoBehaviour
{
    private Transform target;
    private Vector3 fallbackDirection;
    private float speed;
    private float damage;
    private SurvivorElementType element;
    private float knockbackForce;
    private float lifetime = 4f;
    private float hitRadius = 0.2f;
    private const float TurnRateDegreesPerSecond = 260f;

    public void Launch(Transform homingTarget, Vector3 initialDirection, float travelSpeed, float hitDamage, SurvivorElementType hitElement, float force)
    {
        target = homingTarget;
        fallbackDirection = initialDirection.sqrMagnitude > 0.01f ? initialDirection.normalized : Vector3.forward;
        fallbackDirection.y = 0f;
        if (fallbackDirection.sqrMagnitude < 0.01f)
            fallbackDirection = Vector3.forward;
        fallbackDirection.Normalize();

        speed = travelSpeed;
        damage = hitDamage;
        element = hitElement;
        knockbackForce = force;
        hitRadius = Mathf.Max(0.15f, transform.localScale.x * 0.5f);
        transform.rotation = Quaternion.LookRotation(fallbackDirection);
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 desiredDirection = fallbackDirection;
        if (target != null)
        {
            desiredDirection = target.position - transform.position;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude > 0.01f)
                desiredDirection.Normalize();
            else
                desiredDirection = transform.forward;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(desiredDirection),
            TurnRateDegreesPerSecond * Time.deltaTime);

        float step = speed * Time.deltaTime;
        Vector3 moveDir = transform.forward;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.01f)
            moveDir = fallbackDirection;
        moveDir.Normalize();

        if (Physics.SphereCast(transform.position, hitRadius, moveDir, out RaycastHit sweepHit, step, ~0, QueryTriggerInteraction.Collide)
            && sweepHit.collider.GetComponentInParent<ISurvivorDamageable>() != null)
        {
            ApplyHit(sweepHit.collider.gameObject, moveDir);
            return;
        }

        transform.position += moveDir * step;

        if (SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, hitRadius, out Collider overlapHit))
            ApplyHit(overlapHit.gameObject, moveDir);
    }

    private void ApplyHit(GameObject hitObject, Vector3 hitDirection)
    {
        SurvivorCombatFX.ApplyHit(hitObject, damage, element, hitDirection, knockbackForce);
        Destroy(gameObject);
    }
}
