using System.Collections.Generic;
using UnityEngine;

public class SurvivorOrbitWeapon : SurvivorWeaponBehavior
{
    private Transform[] orbitNodes;
    private float orbitAngle;
    private float radius;
    private float speed;
    private float damage;
    private readonly Collider[] overlapBuffer = new Collider[16];
    private readonly Dictionary<ISurvivorDamageable, float> hitCooldowns = new Dictionary<ISurvivorDamageable, float>();
    private const float HitInterval = 0.2f;

    protected override void OnInitialize()
    {
        ApplyStats();
        BuildOrbitNodes(data.GetStats(starLevel).count);
    }

    protected override void OnStarLevelChanged()
    {
        int previousCount = orbitNodes?.Length ?? 0;
        ApplyStats();

        int desiredCount = data.GetStats(starLevel).count;
        if (desiredCount != previousCount)
            BuildOrbitNodes(desiredCount);
    }

    private void ApplyStats()
    {
        SurvivorWeaponStarStats stats = data.GetStats(starLevel);
        radius = stats.range;
        speed = stats.rate;
        damage = stats.damage;
    }

    private void BuildOrbitNodes(int count)
    {
        ClearOrbitNodes();

        orbitNodes = new Transform[Mathf.Max(1, count)];
        for (int i = 0; i < orbitNodes.Length; i++)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.name = $"OrbitBlade_{i}";
            node.transform.SetParent(transform, false);
            node.transform.localScale = Vector3.one * data.hitRadius * 2f;

            Collider col = node.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            Rigidbody rb = node.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            node.AddComponent<SurvivorOrbitHitbox>().Initialize(this);

            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = data.weaponColor;

            orbitNodes[i] = node.transform;
        }
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || !CanFire())
            return;

        if (orbitNodes == null || orbitNodes.Length == 0)
            return;

        float rateMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RateMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float effectiveRadius = radius * rangeMultiplier;

        orbitAngle += speed * rateMultiplier * Time.deltaTime;

        for (int i = 0; i < orbitNodes.Length; i++)
        {
            float angle = orbitAngle + (360f / orbitNodes.Length) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * effectiveRadius;
            orbitNodes[i].localPosition = offset + Vector3.up * 0.6f;
            DamageAtPoint(orbitNodes[i].position);
        }
    }

    private void DamageAtPoint(Vector3 worldPoint)
    {
        float now = Time.time;
        int hitCount = Physics.OverlapSphereNonAlloc(worldPoint, data.hitRadius, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            ISurvivorDamageable target = overlapBuffer[i].GetComponentInParent<ISurvivorDamageable>();
            if (target == null)
                continue;

            if (hitCooldowns.TryGetValue(target, out float nextHitTime) && now < nextHitTime)
                continue;

            hitCooldowns[target] = now + HitInterval;
            DealDamage(overlapBuffer[i].gameObject, worldPoint);
        }
    }

    public void DealDamage(GameObject targetObject, Vector3 sourcePoint)
    {
        if (targetObject == null || controller == null || !controller.IsRunning || !CanFire())
            return;

        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        Vector3 direction = targetObject.transform.position - sourcePoint;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

        PlayFireSfx();
        SurvivorCombatFX.ApplyHit(targetObject, damage * damageMultiplier, data.element, direction, data.knockbackForce);
    }

    private void ClearOrbitNodes()
    {
        if (orbitNodes == null)
            return;

        for (int i = 0; i < orbitNodes.Length; i++)
        {
            if (orbitNodes[i] != null)
                Destroy(orbitNodes[i].gameObject);
        }

        orbitNodes = null;
    }

    private void OnDestroy()
    {
        ClearOrbitNodes();
    }
}

public class SurvivorOrbitHitbox : MonoBehaviour
{
    private SurvivorOrbitWeapon weapon;

    public void Initialize(SurvivorOrbitWeapon owner)
    {
        weapon = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (weapon == null || !weapon.AllowsFiring)
            return;

        if (other.GetComponentInParent<ISurvivorDamageable>() != null)
            weapon.DealDamage(other.gameObject, transform.position);
    }
}
