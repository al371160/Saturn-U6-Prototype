using System.Collections.Generic;
using UnityEngine;

public class SurvivorBouncingBulletWeapon : SurvivorWeaponBehavior
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

        fireTimer = Mathf.Max(0.2f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        Fire(stats);
    }

    private void Fire(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget(null);
        bool cursorAim = HasCursorAim(out Vector3 cursorDir);
        if (!cursorAim && target == null)
            return;

        PlayFireSfx();

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        int bounces = Mathf.Max(1, Mathf.RoundToInt(stats.secondaryValue));

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorBouncingBullet";
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

        SurvivorBouncingBulletProjectile projectile = projectileObject.AddComponent<SurvivorBouncingBulletProjectile>();
        if (cursorAim)
            projectile.LaunchDirected(this, cursorDir, stats.range * rangeMultiplier, stats.damage * damageMultiplier, bounces, data.element, data.knockbackForce);
        else
            projectile.Launch(this, target, stats.range * rangeMultiplier, stats.damage * damageMultiplier, bounces, data.element, data.knockbackForce);
    }

    public Transform FindNearestTarget(List<Transform> excluding)
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
            ConsiderCandidate(enemyBuffer[i].transform, excluding, ref nearest, ref nearestSqrDistance);
        for (int i = 0; i < bossBuffer.Count; i++)
            ConsiderCandidate(bossBuffer[i].transform, excluding, ref nearest, ref nearestSqrDistance);

        return nearest;
    }

    private void ConsiderCandidate(Transform candidate, List<Transform> excluding, ref Transform nearest, ref float nearestSqrDistance)
    {
        if (excluding != null && excluding.Contains(candidate))
            return;

        float sqrDistance = (candidate.position - transform.position).sqrMagnitude;
        if (sqrDistance < nearestSqrDistance)
        {
            nearestSqrDistance = sqrDistance;
            nearest = candidate;
        }
    }
}

public class SurvivorBouncingBulletProjectile : MonoBehaviour
{
    private SurvivorBouncingBulletWeapon owner;
    private Transform currentTarget;
    private Vector3 flyDirection;
    private bool directedFlight;
    private float speed;
    private float damage;
    private int bouncesLeft;
    private SurvivorElementType element;
    private float knockbackForce;
    private float lifetime = 4f;
    private float hitRadius = 0.2f;
    private readonly List<Transform> visited = new List<Transform>();

    public void Launch(SurvivorBouncingBulletWeapon weapon, Transform target, float travelSpeed, float hitDamage, int bounceCount, SurvivorElementType hitElement, float force)
    {
        owner = weapon;
        currentTarget = target;
        directedFlight = false;
        speed = travelSpeed;
        damage = hitDamage;
        bouncesLeft = bounceCount;
        element = hitElement;
        knockbackForce = force;
        hitRadius = Mathf.Max(0.15f, transform.localScale.x * 0.5f);
        if (target != null)
            visited.Add(target);
    }

    public void LaunchDirected(SurvivorBouncingBulletWeapon weapon, Vector3 direction, float travelSpeed, float hitDamage, int bounceCount, SurvivorElementType hitElement, float force)
    {
        owner = weapon;
        currentTarget = null;
        directedFlight = true;
        flyDirection = direction;
        flyDirection.y = 0f;
        if (flyDirection.sqrMagnitude < 0.01f)
            flyDirection = Vector3.forward;
        flyDirection.Normalize();
        speed = travelSpeed;
        damage = hitDamage;
        bouncesLeft = bounceCount;
        element = hitElement;
        knockbackForce = force;
        hitRadius = Mathf.Max(0.15f, transform.localScale.x * 0.5f);
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (directedFlight || currentTarget == null)
        {
            UpdateDirectedFlight();
            return;
        }

        Vector3 toTarget = currentTarget.position - transform.position;
        if (toTarget.sqrMagnitude <= 0.35f * 0.35f)
        {
            HitCurrentTarget();
            return;
        }

        Vector3 moveDir = toTarget.normalized;
        float step = speed * Time.deltaTime;
        transform.position += moveDir * step;

        if (SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, hitRadius, out Collider overlap))
        {
            Component damageableComponent = overlap.GetComponentInParent<ISurvivorDamageable>() as Component;
            Transform hitRoot = damageableComponent != null ? damageableComponent.transform : null;
            if (hitRoot != null && hitRoot != currentTarget && !visited.Contains(hitRoot))
            {
                visited.Add(hitRoot);
                SurvivorCombatFX.ApplyHit(overlap.gameObject, damage, element, moveDir, knockbackForce);
            }
        }
    }

    private void UpdateDirectedFlight()
    {
        float step = speed * Time.deltaTime;
        if (Physics.SphereCast(transform.position, hitRadius, flyDirection, out RaycastHit sweepHit, step, ~0, QueryTriggerInteraction.Collide)
            && sweepHit.collider.GetComponentInParent<ISurvivorDamageable>() != null)
        {
            ApplyDirectedHit(sweepHit.collider);
            return;
        }

        transform.position += flyDirection * step;

        if (SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, hitRadius, out Collider overlapHit))
            ApplyDirectedHit(overlapHit);
    }

    private void ApplyDirectedHit(Collider hitCollider)
    {
        Component damageableComponent = hitCollider.GetComponentInParent<ISurvivorDamageable>() as Component;
        Transform hitRoot = damageableComponent != null ? damageableComponent.transform : hitCollider.transform;

        SurvivorCombatFX.ApplyHit(hitCollider.gameObject, damage, element, flyDirection, knockbackForce);
        visited.Add(hitRoot);

        bouncesLeft--;
        if (bouncesLeft <= 0 || owner == null)
        {
            Destroy(gameObject);
            return;
        }

        Transform next = owner.FindNearestTarget(visited);
        if (next == null)
        {
            Destroy(gameObject);
            return;
        }

        directedFlight = false;
        visited.Add(next);
        currentTarget = next;
    }

    private void HitCurrentTarget()
    {
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        SurvivorCombatFX.ApplyHit(currentTarget.gameObject, damage, element, direction, knockbackForce);

        bouncesLeft--;
        if (bouncesLeft <= 0 || owner == null)
        {
            Destroy(gameObject);
            return;
        }

        Transform next = owner.FindNearestTarget(visited);
        if (next == null)
        {
            Destroy(gameObject);
            return;
        }

        visited.Add(next);
        currentTarget = next;
    }
}
