using System.Collections.Generic;
using UnityEngine;

public class SurvivorChainWeapon : SurvivorWeaponBehavior
{
    private float fireTimer;
    private readonly List<SurvivorMinigameEnemy> enemyBuffer = new List<SurvivorMinigameEnemy>();
    private readonly List<SurvivorBossEnemy> bossBuffer = new List<SurvivorBossEnemy>();
    private readonly List<Transform> visited = new List<Transform>();
    private readonly List<Vector3> chainPoints = new List<Vector3>();

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

        fireTimer = Mathf.Max(0.25f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        CastChain(stats);
    }

    private void CastChain(SurvivorWeaponStarStats stats)
    {
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float jumpRadius = stats.range * rangeMultiplier;
        int maxJumps = Mathf.Max(1, stats.count);

        visited.Clear();
        chainPoints.Clear();

        Vector3 currentPoint = transform.position;
        chainPoints.Add(currentPoint);

        for (int i = 0; i < maxJumps; i++)
        {
            Transform next = FindNearestUnvisited(currentPoint, jumpRadius);
            if (next == null)
                break;

            if (next.GetComponent<ISurvivorDamageable>() != null)
            {
                Vector3 direction = next.position - currentPoint;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
                SurvivorCombatFX.ApplyHit(next.gameObject, stats.damage * damageMultiplier, data.element, direction, data.knockbackForce);
            }

            visited.Add(next);
            currentPoint = next.position;
            chainPoints.Add(currentPoint);
        }

        if (chainPoints.Count > 1)
            SpawnChainVisual();
    }

    private Transform FindNearestUnvisited(Vector3 fromPosition, float radius)
    {
        if (controller == null || controller.enemyRoot == null)
            return null;

        enemyBuffer.Clear();
        controller.enemyRoot.GetComponentsInChildren(false, enemyBuffer);
        bossBuffer.Clear();
        controller.enemyRoot.GetComponentsInChildren(false, bossBuffer);

        Transform nearest = null;
        float nearestSqrDistance = radius * radius;

        for (int i = 0; i < enemyBuffer.Count; i++)
            ConsiderCandidate(enemyBuffer[i].transform, fromPosition, ref nearest, ref nearestSqrDistance);
        for (int i = 0; i < bossBuffer.Count; i++)
            ConsiderCandidate(bossBuffer[i].transform, fromPosition, ref nearest, ref nearestSqrDistance);

        return nearest;
    }

    private void ConsiderCandidate(Transform candidate, Vector3 fromPosition, ref Transform nearest, ref float nearestSqrDistance)
    {
        if (visited.Contains(candidate))
            return;

        float sqrDistance = (candidate.position - fromPosition).sqrMagnitude;
        if (sqrDistance <= nearestSqrDistance)
        {
            nearestSqrDistance = sqrDistance;
            nearest = candidate;
        }
    }

    private void SpawnChainVisual()
    {
        GameObject lineObject = new GameObject("ChainVisual");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = chainPoints.Count;
        line.SetPositions(chainPoints.ToArray());
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = data.weaponColor;
        line.endColor = data.weaponColor;
        Destroy(lineObject, 0.15f);
    }
}
