using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instant-hit ray weapon. A wide pellet spread with no pierce reads as a shotgun; a single
/// high-pierce, long-range shot reads as a railgun — both driven by the same data-defined
/// pellet-count/spread/pierce parameters rather than separate classes.
/// </summary>
public class SurvivorHitscanWeapon : SurvivorWeaponBehavior
{
    private const float ShotgunSpreadDegrees = 35f;

    private float fireTimer;
    private readonly List<SurvivorMinigameEnemy> enemyBuffer = new List<SurvivorMinigameEnemy>();
    private readonly List<SurvivorBossEnemy> bossBuffer = new List<SurvivorBossEnemy>();
    private readonly RaycastHit[] rayHits = new RaycastHit[16];

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

        fireTimer = Mathf.Max(0.1f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        FireVolley(stats);
    }

    private void FireVolley(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget();
        if (!HasAimSolution(target))
            return;

        PlayFireSfx();

        Vector3 baseDirection = ResolveFlatAimDirection(target);

        int pellets = Mathf.Max(1, stats.count);
        float spreadStep = pellets > 1 ? ShotgunSpreadDegrees / (pellets - 1) : 0f;
        float startAngle = -ShotgunSpreadDegrees * 0.5f;

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        int pierceCount = Mathf.Max(1, Mathf.RoundToInt(stats.secondaryValue));

        for (int i = 0; i < pellets; i++)
        {
            float angle = pellets > 1 ? startAngle + spreadStep * i : 0f;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            FireRay(direction, stats.damage * damageMultiplier, stats.range * rangeMultiplier, pierceCount);
        }

        // A single-pellet, high-pierce shot reads as a railgun "big bullet" — worth a shake.
        if (pellets == 1 && pierceCount >= 3)
            SurvivorCombatFX.Shake(0.6f);
    }

    private void FireRay(Vector3 direction, float damage, float maxDistance, int pierceCount)
    {
        Vector3 origin = GetProjectileSpawnPosition();
        int hitCount = Physics.RaycastNonAlloc(origin, direction, rayHits, maxDistance);
        System.Array.Sort(rayHits, 0, hitCount, DistanceComparer.Instance);

        int applied = 0;
        for (int i = 0; i < hitCount && applied < pierceCount; i++)
        {
            GameObject hitObject = rayHits[i].collider.gameObject;
            if (hitObject.GetComponentInParent<ISurvivorDamageable>() == null)
                continue;

            SurvivorCombatFX.ApplyHit(hitObject, damage, data.element, direction, data.knockbackForce);
            applied++;
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

    private class DistanceComparer : IComparer<RaycastHit>
    {
        public static readonly DistanceComparer Instance = new DistanceComparer();

        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}
