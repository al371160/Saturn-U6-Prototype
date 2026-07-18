using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Single shared pathway every weapon routes damage through, so any weapon can "mix and match" with
/// any elemental effect: applies the raw damage, spawns a floating damage number, applies an optional
/// elemental status effect, applies optional physical knockback, and can trigger camera screen shake.
/// </summary>
public static class SurvivorCombatFX
{
    // Every weapon gets at least a token push regardless of its own knockbackForce, scaled by
    // damage dealt, so hits always read as physical even for zero-knockback weapons.
    private const float MinimumKnockbackPerDamage = 0.06f;

    private static CinemachineImpulseSource impulseSource;

    public static void ApplyHit(GameObject targetObject, float damage, SurvivorElementType element, Vector3 knockbackDirection, float knockbackForce)
    {
        if (targetObject == null)
            return;

        ISurvivorDamageable damageable = targetObject.GetComponent<ISurvivorDamageable>();
        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        ShowDamage(targetObject.transform.position + Vector3.up * 1.2f, damage, ColorForElement(element));

        if (element != SurvivorElementType.None)
        {
            SurvivorStatusEffect status = targetObject.GetComponent<SurvivorStatusEffect>();
            if (status == null)
                status = targetObject.AddComponent<SurvivorStatusEffect>();

            status.ApplyElement(element, damage);
        }

        float effectiveKnockbackForce = Mathf.Max(knockbackForce, damage * MinimumKnockbackPerDamage);
        if (effectiveKnockbackForce > 0f)
        {
            ISurvivorStatusTarget target = targetObject.GetComponent<ISurvivorStatusTarget>();
            target?.ApplyKnockback(knockbackDirection, effectiveKnockbackForce);
        }
    }

    public static void ShowDamage(Vector3 position, float amount, Color color)
    {
        SurvivorDamagePopup.Create(position, amount, color);
    }

    public static void Shake(float intensity)
    {
        EnsureImpulseSource();
        impulseSource.GenerateImpulseWithForce(intensity);
    }

    public static Color ColorForElement(SurvivorElementType element)
    {
        switch (element)
        {
            case SurvivorElementType.Fire:
                return new Color(1f, 0.45f, 0.15f);
            case SurvivorElementType.Ice:
                return new Color(0.55f, 0.85f, 1f);
            case SurvivorElementType.Lightning:
                return new Color(1f, 0.95f, 0.3f);
            case SurvivorElementType.Bleed:
                return new Color(0.85f, 0.15f, 0.2f);
            case SurvivorElementType.Poison:
                return new Color(0.5f, 0.9f, 0.3f);
            default:
                return Color.white;
        }
    }

    private static void EnsureImpulseSource()
    {
        if (impulseSource != null)
            return;

        GameObject sourceObject = new GameObject("SurvivorScreenShakeSource");
        Object.DontDestroyOnLoad(sourceObject);
        impulseSource = sourceObject.AddComponent<CinemachineImpulseSource>();
    }
}
