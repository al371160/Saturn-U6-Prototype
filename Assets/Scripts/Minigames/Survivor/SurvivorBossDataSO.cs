using UnityEngine;

public enum SurvivorBossAttackType
{
    Lunge,
    GroundSlam,
    ProjectileVolley,
    ChargeSweep
}

/// <summary>One entry in a boss's attack pool — mirrors how SurvivorWeaponStarStats packs a few
/// reused fields differently per archetype, rather than exploding per-attack-type classes.</summary>
[System.Serializable]
public struct SurvivorBossAttackDefinition
{
    public SurvivorBossAttackType attackType;
    [Tooltip("How long the attack telegraphs before it fires.")]
    public float telegraphDuration;
    public float damage;
    [Tooltip("Hit radius for GroundSlam/ChargeSweep, or trigger radius for Lunge.")]
    public float radius;
    [Tooltip("Travel distance for ChargeSweep, or max range for ProjectileVolley.")]
    public float range;
    [Tooltip("Only used by ProjectileVolley.")]
    public int projectileCount;
    [Tooltip("Seconds this specific attack is unavailable again after use — prevents back-to-back repeats.")]
    public float cooldownAfterUse;
    [Tooltip("Relative likelihood of being chosen among currently-available attacks.")]
    public float weight;
}

/// <summary>
/// Data-driven boss definition, mirroring the SurvivorWeaponDataSO/SurvivorBuffDataSO convention —
/// tune bosses as ScriptableObject assets rather than hardcoding behavior per boss in code.
/// SurvivorBossEnemy is the generic executor that reads this data.
/// </summary>
[CreateAssetMenu(menuName = "Minigames/Survivor Boss")]
public class SurvivorBossDataSO : ScriptableObject
{
    [Header("Identity")]
    public string bossId;
    public string displayName;
    [TextArea]
    public string description;
    public Color bossColor = new Color(0.55f, 0.15f, 0.65f);

    [Header("Core Stats")]
    public float maxHealth = 1200f;
    public float moveSpeed = 3.5f;
    public float contactDamage = 15f;
    public float scale = 2.2f;
    public float recoverDuration = 1f;

    [Header("Phases")]
    [Tooltip("Health fraction at which the boss enrages into phase 2 (new attacks unlock, existing ones speed up).")]
    [Range(0f, 1f)]
    public float enrageHealthFraction = 0.5f;
    public float enrageMoveSpeedMultiplier = 1.3f;

    [Header("Attacks")]
    public SurvivorBossAttackDefinition[] phase1Attacks;
    [Tooltip("Active once enraged — can reuse phase 1 entries plus add new ones.")]
    public SurvivorBossAttackDefinition[] phase2Attacks;

    [Header("Reward")]
    public int bossXPReward = 50;
}
