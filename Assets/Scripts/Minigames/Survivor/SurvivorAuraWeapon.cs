using UnityEngine;

public class SurvivorAuraWeapon : SurvivorWeaponBehavior
{
    private float tickTimer;
    private GameObject visual;
    private readonly Collider[] overlapBuffer = new Collider[32];

    protected override void OnInitialize()
    {
        tickTimer = 0f;
        BuildVisual();
    }

    protected override void OnStarLevelChanged()
    {
    }

    private void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "AuraVisual";
        visual.transform.SetParent(transform, false);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color c = data.weaponColor;
            c.a = 0.25f;
            renderer.material.color = c;
        }
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused)
            return;

        SurvivorWeaponStarStats stats = data.GetStats(starLevel);
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float radius = stats.range * rangeMultiplier;

        if (visual != null)
            visual.transform.localScale = Vector3.one * radius * 2f;

        float rateMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RateMultiplier : 1f;
        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.1f, rateMultiplier > 0f ? stats.rate / rateMultiplier : stats.rate);
        DealDamage(stats, radius);
    }

    private void DealDamage(SurvivorWeaponStarStats stats, float radius)
    {
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObject = overlapBuffer[i].gameObject;
            if (hitObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            Vector3 direction = hitObject.transform.position - transform.position;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            SurvivorCombatFX.ApplyHit(hitObject, stats.damage * damageMultiplier, data.element, direction, data.knockbackForce);
        }
    }

    private void OnDestroy()
    {
        if (visual != null)
            Destroy(visual);
    }
}
