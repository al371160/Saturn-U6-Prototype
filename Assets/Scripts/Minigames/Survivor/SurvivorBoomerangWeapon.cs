using System.Collections.Generic;
using UnityEngine;

public class SurvivorBoomerangWeapon : SurvivorWeaponBehavior
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
        FireVolley(stats);
    }

    private void FireVolley(SurvivorWeaponStarStats stats)
    {
        PlayFireSfx();
        Vector3 direction = ResolveFlatAimDirection(FindNearestTarget());

        int shots = Mathf.Max(1, stats.count);
        float spreadStep = shots > 1 ? 25f : 0f;
        float startAngleOffset = -spreadStep * (shots - 1) * 0.5f;

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;

        for (int i = 0; i < shots; i++)
        {
            float angle = startAngleOffset + spreadStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * direction;
            SpawnBoomerang(dir, stats.damage * damageMultiplier, stats.range * rangeMultiplier);
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

    private void SpawnBoomerang(Vector3 direction, float damage, float speed)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorBoomerang";
        projectileObject.transform.position = GetProjectileSpawnPosition();
        projectileObject.transform.localScale = Vector3.one * data.hitRadius * 1.8f;

        Collider col = projectileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = data.weaponColor;

        projectileObject.AddComponent<SurvivorBoomerangProjectile>().Launch(transform, direction, speed, damage, data.element, data.knockbackForce);
    }
}

public class SurvivorBoomerangProjectile : MonoBehaviour
{
    private Transform owner;
    private Vector3 direction;
    private float speed;
    private float damage;
    private SurvivorElementType element;
    private float knockbackForce;
    private float outboundTimer = 0.45f;
    private bool returning;
    private float lifetime = 3f;
    private readonly HashSet<int> hitIds = new HashSet<int>();

    public void Launch(Transform ownerTransform, Vector3 travelDirection, float travelSpeed, float hitDamage, SurvivorElementType hitElement, float force)
    {
        owner = ownerTransform;
        direction = travelDirection;
        speed = travelSpeed;
        damage = hitDamage;
        element = hitElement;
        knockbackForce = force;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!returning)
        {
            transform.position += direction * (speed * Time.deltaTime);
            outboundTimer -= Time.deltaTime;
            if (outboundTimer <= 0f)
                returning = true;
        }
        else
        {
            Vector3 targetPosition = owner != null ? owner.position : transform.position;
            Vector3 toOwner = targetPosition - transform.position;

            if (toOwner.sqrMagnitude < 0.3f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += toOwner.normalized * (speed * Time.deltaTime);
        }

        TryHitDamageables();
    }

    private void TryHitDamageables()
    {
        float radius = Mathf.Max(0.2f, transform.localScale.x * 0.5f);
        if (!SurvivorWeaponBehavior.TryGetDamageableHit(transform.position, radius, out Collider hit))
            return;

        Component damageable = hit.GetComponentInParent<ISurvivorDamageable>() as Component;
        int id = damageable != null ? damageable.GetInstanceID() : hit.GetInstanceID();
        if (!hitIds.Add(id))
            return;

        SurvivorCombatFX.ApplyHit(hit.gameObject, damage, element, direction, knockbackForce);
    }
}
