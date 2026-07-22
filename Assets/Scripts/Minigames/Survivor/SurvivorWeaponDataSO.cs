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

public enum SurvivorWeaponRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
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
    [Tooltip("Inherent power tier — affects drop weight and starting star range on ground/crate pickups.")]
    public SurvivorWeaponRarity rarity = SurvivorWeaponRarity.Common;

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

/// <summary>Weighted loot rolls and rarity presentation colors for Survivor weapon drops.</summary>
public static class SurvivorLootRarity
{
    private static readonly float[] RarityWeights =
    {
        50f, // Common
        25f, // Uncommon
        15f, // Rare
        7f,  // Epic
        3f   // Legendary
    };

    public static Color GetColor(SurvivorWeaponRarity rarity)
    {
        switch (rarity)
        {
            case SurvivorWeaponRarity.Uncommon: return new Color(0.35f, 0.85f, 0.40f);
            case SurvivorWeaponRarity.Rare: return new Color(0.30f, 0.55f, 1f);
            case SurvivorWeaponRarity.Epic: return new Color(0.75f, 0.35f, 0.95f);
            case SurvivorWeaponRarity.Legendary: return new Color(1f, 0.75f, 0.2f);
            default: return new Color(0.72f, 0.72f, 0.75f);
        }
    }

    public static SurvivorWeaponDataSO PickWeightedWeapon(SurvivorWeaponDataSO[] pool)
    {
        if (pool == null || pool.Length == 0)
            return null;

        SurvivorWeaponRarity tier = RollRarityTierPresent(pool);
        SurvivorWeaponDataSO pick = PickFromTier(pool, tier);
        if (pick != null)
            return pick;

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null)
                return pool[i];
        }

        return null;
    }

    public static int RollStartStar(SurvivorWeaponDataSO weapon)
    {
        if (weapon == null)
            return 1;

        int maxStar = Mathf.Max(1, weapon.MaxStar);
        int min;
        int max;
        switch (weapon.rarity)
        {
            case SurvivorWeaponRarity.Uncommon:
                min = 1; max = 3; break;
            case SurvivorWeaponRarity.Rare:
                min = 2; max = 4; break;
            case SurvivorWeaponRarity.Epic:
                min = 3; max = 5; break;
            case SurvivorWeaponRarity.Legendary:
                min = 4; max = maxStar; break;
            default:
                min = 1; max = 2; break;
        }

        max = Mathf.Clamp(max, 1, maxStar);
        min = Mathf.Clamp(min, 1, max);
        return Random.Range(min, max + 1);
    }

    private static SurvivorWeaponRarity RollRarityTierPresent(SurvivorWeaponDataSO[] pool)
    {
        bool[] present = new bool[5];
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == null)
                continue;
            present[(int)pool[i].rarity] = true;
        }

        float total = 0f;
        for (int i = 0; i < RarityWeights.Length; i++)
        {
            if (present[i])
                total += RarityWeights[i];
        }

        if (total <= 0f)
            return SurvivorWeaponRarity.Common;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < RarityWeights.Length; i++)
        {
            if (!present[i])
                continue;
            cumulative += RarityWeights[i];
            if (roll <= cumulative)
                return (SurvivorWeaponRarity)i;
        }

        return SurvivorWeaponRarity.Common;
    }

    private static SurvivorWeaponDataSO PickFromTier(SurvivorWeaponDataSO[] pool, SurvivorWeaponRarity tier)
    {
        int count = 0;
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null && pool[i].rarity == tier)
                count++;
        }

        if (count == 0)
            return null;

        int pick = Random.Range(0, count);
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == null || pool[i].rarity != tier)
                continue;
            if (pick == 0)
                return pool[i];
            pick--;
        }

        return null;
    }
}
