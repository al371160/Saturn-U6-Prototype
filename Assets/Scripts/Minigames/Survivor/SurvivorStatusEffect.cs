using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added dynamically to an enemy/boss GameObject on the first elemental hit. Tracks damage-over-time
/// stacks (Fire/Poison/Bleed) and slow effects (Ice freeze, Lightning shock) independently so a target
/// can be, say, burning and frozen at the same time. Reapplying the same element refreshes its duration
/// rather than stacking a second instance.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorStatusEffect : MonoBehaviour
{
    private class DotStack
    {
        public SurvivorElementType element;
        public float remainingDuration;
        public float tickInterval;
        public float tickTimer;
        public float tickDamage;
    }

    private ISurvivorDamageable damageable;
    private ISurvivorStatusTarget statusTarget;
    private readonly List<DotStack> dots = new List<DotStack>();
    private float freezeTimer;
    private float shockSlowTimer;
    private float stunTimer;

    private void Awake()
    {
        damageable = GetComponent<ISurvivorDamageable>();
        statusTarget = GetComponent<ISurvivorStatusTarget>();
    }

    public void ApplyElement(SurvivorElementType element, float sourceDamage)
    {
        SurvivorElementType existingElement = GetActiveElement();
        SurvivorComboReaction reaction = SurvivorElementalCombos.GetReaction(existingElement, element);

        if (reaction != SurvivorComboReaction.None)
        {
            TriggerCombo(reaction, existingElement, sourceDamage);
            return;
        }

        switch (element)
        {
            case SurvivorElementType.Fire:
                AddOrRefreshDot(SurvivorElementType.Fire, sourceDamage * 0.3f, 0.5f, 3f);
                break;
            case SurvivorElementType.Poison:
                AddOrRefreshDot(SurvivorElementType.Poison, sourceDamage * 0.2f, 0.5f, 4f);
                break;
            case SurvivorElementType.Bleed:
                AddOrRefreshDot(SurvivorElementType.Bleed, sourceDamage * 0.25f, 0.5f, 2.5f);
                break;
            case SurvivorElementType.Ice:
                freezeTimer = Mathf.Max(freezeTimer, 1.5f);
                break;
            case SurvivorElementType.Lightning:
                shockSlowTimer = Mathf.Max(shockSlowTimer, 1f);
                break;
        }
    }

    /// <summary>Which element is currently "active" on this target, for combo lookup purposes.
    /// Freeze/shock (binary timers) take priority since they're the more commonly-combo'd elements
    /// (Ice+Lightning); otherwise the first active DOT stack is used.</summary>
    private SurvivorElementType GetActiveElement()
    {
        if (freezeTimer > 0f)
            return SurvivorElementType.Ice;
        if (shockSlowTimer > 0f)
            return SurvivorElementType.Lightning;
        if (dots.Count > 0)
            return dots[0].element;

        return SurvivorElementType.None;
    }

    /// <summary>
    /// Noita-style reaction: consumes the existing element's stacks/timers (reactions cost their
    /// ingredients, which is what makes them read as combos rather than free bonus damage) and
    /// deals bonus damage instead of applying the incoming element normally.
    /// </summary>
    private void TriggerCombo(SurvivorComboReaction reaction, SurvivorElementType existingElement, float sourceDamage)
    {
        float bonusDamage = 0f;

        switch (reaction)
        {
            case SurvivorComboReaction.BurstDamage:
                bonusDamage = sourceDamage * 1.5f;
                RemoveDot(existingElement);
                break;
            case SurvivorComboReaction.ShatterStun:
                bonusDamage = sourceDamage * 1.2f;
                freezeTimer = 0f;
                shockSlowTimer = 0f;
                stunTimer = 1.2f;
                break;
            case SurvivorComboReaction.TrueDamageAmp:
                bonusDamage = sourceDamage * 0.5f;
                RemoveDot(SurvivorElementType.Bleed);
                break;
        }

        damageable?.TakeDamage(bonusDamage);
        SurvivorComboFX.PlayReaction(transform.position + Vector3.up * 1.2f, reaction, bonusDamage);
    }

    private void RemoveDot(SurvivorElementType element)
    {
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            if (dots[i].element == element)
                dots.RemoveAt(i);
        }
    }

    private void AddOrRefreshDot(SurvivorElementType element, float tickDamage, float tickInterval, float duration)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i].element != element)
                continue;

            dots[i].remainingDuration = duration;
            dots[i].tickDamage = tickDamage;
            return;
        }

        dots.Add(new DotStack
        {
            element = element,
            remainingDuration = duration,
            tickInterval = tickInterval,
            tickTimer = tickInterval,
            tickDamage = tickDamage
        });
    }

    private void Update()
    {
        TickDots();
        TickSlows();
    }

    private void TickDots()
    {
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            DotStack dot = dots[i];
            dot.remainingDuration -= Time.deltaTime;

            if (dot.remainingDuration <= 0f)
            {
                dots.RemoveAt(i);
                continue;
            }

            dot.tickTimer -= Time.deltaTime;
            if (dot.tickTimer <= 0f)
            {
                dot.tickTimer = dot.tickInterval;
                damageable?.TakeDamage(dot.tickDamage);
            }
        }
    }

    private void TickSlows()
    {
        if (freezeTimer > 0f)
            freezeTimer -= Time.deltaTime;

        if (shockSlowTimer > 0f)
            shockSlowTimer -= Time.deltaTime;

        if (stunTimer > 0f)
            stunTimer -= Time.deltaTime;

        if (statusTarget == null)
            return;

        float slowMultiplier = 1f;
        if (freezeTimer > 0f)
            slowMultiplier *= 0.05f;
        if (shockSlowTimer > 0f)
            slowMultiplier *= 0.3f;
        if (stunTimer > 0f)
            slowMultiplier *= 0f;

        statusTarget.SetSlowMultiplier(slowMultiplier);
    }
}
