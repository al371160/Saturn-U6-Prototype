using UnityEngine;

public enum SurvivorWeaponType
{
    Orbit,
    Projectile,
    Aura,
    Boomerang,
    Chain,
    Hitscan,
    BouncingBullet,
    PoisonPool,
    Explosive,
    Homing,
    Drone,
    Melee
}

public enum SurvivorMeleePattern
{
    Smash,
    Thrust,
    Slash
}

/// <summary>One diep.io-style branch a weapon can evolve into once it reaches requiredStar — several
/// options can be offered at once (even below max star), each a meaningfully different playstyle
/// rather than just bigger numbers.</summary>
[System.Serializable]
public struct SurvivorWeaponEvolutionOption
{
    public SurvivorWeaponDataSO targetWeapon;
    [Tooltip("Minimum star level at which this branch becomes available (can be below MaxStar).")]
    public int requiredStar;
    public string branchName;
    [TextArea]
    public string branchDescription;
}

[System.Serializable]
public struct SurvivorWeaponStarStats
{
    public float damage;
    [Tooltip("Orbit: rotation speed deg/sec. Most other types: seconds between volleys/ticks.")]
    public float rate;
    [Tooltip("Radius, travel speed, or jump distance, depending on weapon type.")]
    public float range;
    [Tooltip("Orbiters/pellets/shots/jumps/drones per volley, depending on weapon type.")]
    public int count;
    [Tooltip("Meaning depends on weapon type: pierce count (Hitscan), bounce count (BouncingBullet), " +
        "extra knockback multiplier (melee/explosive), or unused.")]
    public float secondaryValue;
}

[CreateAssetMenu(menuName = "Minigames/Survivor Weapon")]
public class SurvivorWeaponDataSO : ScriptableObject
{
    [Header("Identity")]
    public string weaponId;
    public string displayName;
    [TextArea]
    public string description;
    public SurvivorWeaponType weaponType;
    [Tooltip("Only used when weaponType is Melee.")]
    public SurvivorMeleePattern meleePattern;
    public Color weaponColor = Color.white;
    public Sprite icon;

    [Header("Shared")]
    public float hitRadius = 0.45f;
    [Tooltip("Manual weapons (melee) are parented to the player and swing with aim direction. " +
        "Auto weapons (everything else) only track the player's position, not rotation.")]
    public bool isManualWeapon = false;
    public SurvivorElementType element = SurvivorElementType.None;
    [Tooltip("Physical knockback force applied to anything this weapon hits. 0 = no knockback.")]
    public float knockbackForce = 0f;

    [Header("Star Progression (element 0 = star 1)")]
    public SurvivorWeaponStarStats[] starStats = new SurvivorWeaponStarStats[6];

    [Header("Evolution Branches")]
    [Tooltip("Diep.io-style branching evolutions — each becomes available once the weapon reaches " +
        "requiredStar, offered as an extra level-up choice alongside the normal +1 star.")]
    public SurvivorWeaponEvolutionOption[] evolutionOptions;

    public int MaxStar => Mathf.Max(1, starStats.Length);

    public SurvivorWeaponStarStats GetStats(int star)
    {
        int index = Mathf.Clamp(star - 1, 0, starStats.Length - 1);
        return starStats[index];
    }

    /// <summary>Survivor.io-style numeric callout for the level-up card, generated from real stats.</summary>
    public string GetLevelDescription(int star)
    {
        SurvivorWeaponStarStats stats = GetStats(star);
        string countLabel = CountLabelFor(weaponType);

        string text = $"Lv.{star} — {stats.damage:0} DMG";
        if (countLabel != null && stats.count > 0)
            text += $", {stats.count} {countLabel}";
        text += $", {stats.range:0.#} {RangeLabelFor(weaponType)}";
        text += RateIsMultiplicative(weaponType) ? $", {stats.rate:0} spin speed" : $", {stats.rate:0.0}s cooldown";

        if (star > 1)
        {
            float damageDelta = stats.damage - GetStats(star - 1).damage;
            if (damageDelta > 0.01f)
                text += $" (+{damageDelta:0} DMG)";
        }

        return text;
    }

    private static string CountLabelFor(SurvivorWeaponType type)
    {
        switch (type)
        {
            case SurvivorWeaponType.Orbit: return "orbiters";
            case SurvivorWeaponType.Projectile: return "shots";
            case SurvivorWeaponType.Hitscan: return "pellets";
            case SurvivorWeaponType.Boomerang: return "throws";
            case SurvivorWeaponType.BouncingBullet: return "bounces";
            case SurvivorWeaponType.Chain: return "jumps";
            case SurvivorWeaponType.Drone: return "drones";
            default: return null;
        }
    }

    private static string RangeLabelFor(SurvivorWeaponType type)
    {
        switch (type)
        {
            case SurvivorWeaponType.Orbit: return "radius";
            case SurvivorWeaponType.Aura: return "AoE radius";
            case SurvivorWeaponType.PoisonPool: return "pool radius";
            case SurvivorWeaponType.Explosive: return "blast radius";
            case SurvivorWeaponType.Chain: return "jump range";
            default: return "range";
        }
    }

    private static bool RateIsMultiplicative(SurvivorWeaponType type)
    {
        return type == SurvivorWeaponType.Orbit;
    }
}
