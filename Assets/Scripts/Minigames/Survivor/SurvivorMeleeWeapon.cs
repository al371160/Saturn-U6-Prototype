using System.Collections;
using UnityEngine;

/// <summary>
/// Manual (player-parented, aim-rotated) melee weapon with three swing patterns. Hitboxes are
/// simple primitives with the QuickOutline component as a placeholder visual until real art exists.
/// </summary>
public class SurvivorMeleeWeapon : SurvivorWeaponBehavior
{
    private float fireTimer;

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
        Swing(stats);
    }

    private void Swing(SurvivorWeaponStarStats stats)
    {
        PlayFireSfx();
        float damageMultiplier = controller.WeaponManager != null ? controller.WeaponManager.DamageMultiplier : 1f;
        float rangeMultiplier = controller.WeaponManager != null ? controller.WeaponManager.RangeMultiplier : 1f;
        float damage = stats.damage * damageMultiplier;
        float reach = stats.range * rangeMultiplier;
        float knockback = data.knockbackForce * Mathf.Max(1f, stats.secondaryValue);

        switch (data.meleePattern)
        {
            case SurvivorMeleePattern.Smash:
                StartCoroutine(SmashRoutine(damage, reach, knockback));
                break;
            case SurvivorMeleePattern.Thrust:
                DoThrust(damage, reach, knockback);
                break;
            case SurvivorMeleePattern.Slash:
                DoSlash(damage, reach, knockback);
                break;
        }
    }

    private IEnumerator SmashRoutine(float damage, float reach, float knockback)
    {
        Vector3 center = transform.position + transform.forward * (reach * 0.5f) + Vector3.up * 0.6f;
        GameObject hitboxVisual = CreateHitboxVisual(PrimitiveType.Sphere, center, Quaternion.identity, Vector3.one * reach);

        yield return new WaitForSeconds(0.25f); // windup, telegraphs a strong hit

        int hitCount = ApplySphereDamage(center, reach * 0.5f, damage, knockback * 2f);
        if (hitCount > 0)
            SurvivorCombatFX.Shake(0.4f);

        yield return new WaitForSeconds(0.1f);
        Destroy(hitboxVisual);
    }

    private void DoThrust(float damage, float reach, float knockback)
    {
        Vector3 halfExtents = new Vector3(0.4f, 0.6f, reach * 0.5f);
        Vector3 center = transform.position + transform.forward * (reach * 0.5f) + Vector3.up * 0.6f;
        GameObject hitboxVisual = CreateHitboxVisual(PrimitiveType.Cube, center, transform.rotation, new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, halfExtents.z * 2f));

        ApplyBoxDamage(center, halfExtents, transform.rotation, damage, knockback, transform.forward, pierce: true);
        Destroy(hitboxVisual, 0.15f);
    }

    private void DoSlash(float damage, float reach, float knockback)
    {
        Vector3 halfExtents = new Vector3(reach * 0.5f, 0.6f, reach * 0.35f);
        Vector3 center = transform.position + transform.forward * (reach * 0.3f) + Vector3.up * 0.6f;
        GameObject hitboxVisual = CreateHitboxVisual(PrimitiveType.Cube, center, transform.rotation, new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, halfExtents.z * 2f));

        ApplyBoxDamage(center, halfExtents, transform.rotation, damage, knockback, transform.forward, pierce: true);
        Destroy(hitboxVisual, 0.15f);
    }

    private int ApplySphereDamage(Vector3 center, float radius, float damage, float knockback)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        int applied = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            Vector3 direction = (hitObject.transform.position - center).normalized;
            SurvivorCombatFX.ApplyHit(hitObject, damage, data.element, direction, knockback);
            applied++;
        }
        return applied;
    }

    private void ApplyBoxDamage(Vector3 center, Vector3 halfExtents, Quaternion rotation, float damage, float knockback, Vector3 knockbackDirection, bool pierce)
    {
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            SurvivorCombatFX.ApplyHit(hitObject, damage, data.element, knockbackDirection, knockback);
            if (!pierce)
                break;
        }
    }

    private GameObject CreateHitboxVisual(PrimitiveType shape, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject hitboxObject = GameObject.CreatePrimitive(shape);
        hitboxObject.name = "MeleeHitbox";
        hitboxObject.transform.position = position;
        hitboxObject.transform.rotation = rotation;
        hitboxObject.transform.localScale = scale;

        Collider col = hitboxObject.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = hitboxObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color c = data.weaponColor;
            c.a = 0.15f;
            renderer.material.color = c;
        }

        Outline outline = hitboxObject.AddComponent<Outline>();
        outline.OutlineColor = data.weaponColor;
        outline.OutlineWidth = 4f;

        return hitboxObject;
    }
}
