using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns companions that hover near the player and independently fire their own projectiles
/// at nearby enemies on their own timer, rather than the weapon itself firing.
/// </summary>
public class SurvivorDroneWeapon : SurvivorWeaponBehavior
{
    private readonly List<SurvivorDroneCompanion> drones = new List<SurvivorDroneCompanion>();

    protected override void OnInitialize()
    {
        RebuildDrones();
    }

    protected override void OnStarLevelChanged()
    {
        RebuildDrones();
    }

    private void RebuildDrones()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] != null)
                Destroy(drones[i].gameObject);
        }
        drones.Clear();

        SurvivorWeaponStarStats stats = data.GetStats(starLevel);
        int droneCount = Mathf.Max(1, stats.count);

        for (int i = 0; i < droneCount; i++)
        {
            GameObject droneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droneObject.name = $"SurvivorDrone_{i}";
            droneObject.transform.SetParent(transform, false);
            droneObject.transform.localScale = Vector3.one * 0.5f;

            Collider col = droneObject.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            Rigidbody rb = droneObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Renderer renderer = droneObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = data.weaponColor;

            SurvivorDroneCompanion companion = droneObject.AddComponent<SurvivorDroneCompanion>();
            companion.Initialize(this, controller, i, droneCount);
            drones.Add(companion);
        }
    }

    public SurvivorWeaponStarStats CurrentStats => data.GetStats(starLevel);
    public SurvivorWeaponDataSO WeaponData => data;

    private void OnDestroy()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] != null)
                Destroy(drones[i].gameObject);
        }
    }
}

public class SurvivorDroneCompanion : MonoBehaviour
{
    private SurvivorDroneWeapon owner;
    private SurvivorMinigameController controller;
    private float orbitAngleOffset;
    private float fireTimer;
    private readonly List<SurvivorMinigameEnemy> enemyBuffer = new List<SurvivorMinigameEnemy>();
    private readonly List<SurvivorBossEnemy> bossBuffer = new List<SurvivorBossEnemy>();

    private const float HoverRadius = 2.2f;
    private const float HoverHeight = 1.4f;
    private const float OrbitSpeed = 40f;

    public void Initialize(SurvivorDroneWeapon droneWeapon, SurvivorMinigameController owningController, int index, int total)
    {
        owner = droneWeapon;
        controller = owningController;
        orbitAngleOffset = total > 0 ? (360f / total) * index : 0f;
        fireTimer = Random.Range(0f, 1f);
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || owner == null)
            return;

        float angle = (Time.time * OrbitSpeed + orbitAngleOffset) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * HoverRadius + Vector3.up * HoverHeight;
        transform.localPosition = offset;

        SurvivorWeaponStarStats stats = owner.CurrentStats;
        float rateMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RateMultiplier : 1f;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
            return;

        if (!owner.AllowsFiring)
            return;

        fireTimer = Mathf.Max(0.3f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        FireAtNearestEnemy(stats);
    }

    private void FireAtNearestEnemy(SurvivorWeaponStarStats stats)
    {
        Transform target = FindNearestTarget();
        if (target == null)
            return;

        SurvivorAudio.PlayWeaponFire(owner.WeaponData);

        Vector3 direction = (target.position - transform.position).normalized;
        SurvivorWeaponDataSO weaponData = owner.WeaponData;

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorDroneBolt";
        projectileObject.transform.position = transform.position;
        projectileObject.transform.localScale = Vector3.one * weaponData.hitRadius;

        Collider col = projectileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = weaponData.weaponColor;

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;

        projectileObject.AddComponent<SurvivorProjectile>().Launch(
            direction, stats.range * rangeMultiplier, stats.damage * damageMultiplier);
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
