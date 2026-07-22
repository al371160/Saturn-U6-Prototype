using UnityEngine;

/// <summary>Duration-scaled enemy roles for Survivor spawns (primitive visuals + behavior).</summary>
public enum SurvivorEnemyArchetype
{
    Chaser = 0,
    Tank = 1,
    Dasher = 2,
    Shooter = 3,
    Orbiter = 4,
    Lunger = 5,
    Skirmisher = 6,
}

public static class SurvivorEnemyTierList
{
    public static SurvivorEnemyArchetype Pick(float matchSeconds)
    {
        // Weighted rolls unlock by match duration (tier list).
        float roll = Random.value;

        if (matchSeconds < 180f)
        {
            // T1: Chaser, Tank
            return roll < 0.7f ? SurvivorEnemyArchetype.Chaser : SurvivorEnemyArchetype.Tank;
        }

        if (matchSeconds < 420f)
        {
            // T2 adds Dasher, Shooter
            if (roll < 0.35f) return SurvivorEnemyArchetype.Chaser;
            if (roll < 0.5f) return SurvivorEnemyArchetype.Tank;
            if (roll < 0.75f) return SurvivorEnemyArchetype.Dasher;
            return SurvivorEnemyArchetype.Shooter;
        }

        if (matchSeconds < 660f)
        {
            // T3 adds Orbiter, Lunger
            if (roll < 0.2f) return SurvivorEnemyArchetype.Chaser;
            if (roll < 0.3f) return SurvivorEnemyArchetype.Tank;
            if (roll < 0.45f) return SurvivorEnemyArchetype.Dasher;
            if (roll < 0.6f) return SurvivorEnemyArchetype.Shooter;
            if (roll < 0.8f) return SurvivorEnemyArchetype.Orbiter;
            return SurvivorEnemyArchetype.Lunger;
        }

        // T4: late game — skirmishers + stronger mix
        if (roll < 0.12f) return SurvivorEnemyArchetype.Chaser;
        if (roll < 0.2f) return SurvivorEnemyArchetype.Tank;
        if (roll < 0.35f) return SurvivorEnemyArchetype.Dasher;
        if (roll < 0.5f) return SurvivorEnemyArchetype.Shooter;
        if (roll < 0.65f) return SurvivorEnemyArchetype.Orbiter;
        if (roll < 0.8f) return SurvivorEnemyArchetype.Lunger;
        return SurvivorEnemyArchetype.Skirmisher;
    }

    public static Color GetColor(SurvivorEnemyArchetype archetype)
    {
        switch (archetype)
        {
            case SurvivorEnemyArchetype.Tank: return new Color(0.45f, 0.35f, 0.55f);
            case SurvivorEnemyArchetype.Dasher: return new Color(1f, 0.55f, 0.15f);
            case SurvivorEnemyArchetype.Shooter: return new Color(0.25f, 0.55f, 1f);
            case SurvivorEnemyArchetype.Orbiter: return new Color(0.95f, 0.85f, 0.2f);
            case SurvivorEnemyArchetype.Lunger: return new Color(0.85f, 0.2f, 0.2f);
            case SurvivorEnemyArchetype.Skirmisher: return new Color(0.2f, 0.9f, 0.75f);
            default: return new Color(0.9f, 0.25f, 0.25f);
        }
    }
}
