using System.Collections.Generic;
using UnityEngine;

public class SurvivorExplosiveWeapon : SurvivorWeaponBehavior
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

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorExplosiveShell";
        projectileObject.transform.position = transform.position + Vector3.up * 0.6f;
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

        projectileObject.AddComponent<SurvivorExplosiveShell>().Launch(
            direction, stats.range * rangeMultiplier, stats.damage * damageMultiplier, data.hitRadius * 3f, data.element, data.knockbackForce);
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

public class SurvivorExplosiveShell : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float blastRadius;
    private SurvivorElementType element;
    private float knockbackForce;
    private float lifetime = 3f;
    private bool exploded;

    public void Launch(Vector3 travelDirection, float travelSpeed, float hitDamage, float radius, SurvivorElementType hitElement, float force)
    {
        direction = travelDirection;
        speed = travelSpeed;
        damage = hitDamage;
        blastRadius = radius;
        element = hitElement;
        knockbackForce = force;
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Explode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ISurvivorDamageable>() != null)
            Explode();
    }

    private void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            Vector3 knockbackDir = (hitObject.transform.position - transform.position).normalized;
            SurvivorCombatFX.ApplyHit(hitObject, damage, element, knockbackDir, knockbackForce);
        }

        SurvivorCombatFX.Shake(0.8f);
        Destroy(gameObject);
    }
}
