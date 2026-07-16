using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Minigames/Survivor Minigame Config")]
public class SurvivorMinigameConfig : ScriptableObject
{
    [Header("Player")]
    public float playerMaxHealth = 100f;
    public float contactDamageCooldown = 0.5f;

    [Header("UI")]
    [Tooltip("Font used for the level-up / weapon-choice screen. Leave null to use TMP's project default.")]
    public TMP_FontAsset levelUpFont;

    [Header("Weapons")]
    [Tooltip("Weapon(s) the player starts with, at star 1. Leave empty to instead show startingWeaponChoices.")]
    public SurvivorWeaponDataSO[] startingWeapons;
    [Tooltip("Small curated set the player picks ONE from at the start of a run (only used when startingWeapons is empty).")]
    public SurvivorWeaponDataSO[] startingWeaponChoices;
    [Tooltip("Full pool of weapons/upgrades that can be offered on level-up.")]
    public SurvivorWeaponDataSO[] availableWeapons;
    [Tooltip("Full pool of stat buffs that can be offered on level-up.")]
    public SurvivorBuffDataSO[] availableBuffs;
    public int upgradeChoicesPerLevelUp = 3;

    [Header("Leveling")]
    public int enemyXPDrop = 5;
    public float xpGemMagnetRadius = 3.5f;
    [Tooltip("XP required to reach each level, fast-then-slowing (survivor.io-style). Element 0 = XP for level 1->2, etc.")]
    public int[] xpStepTable = new int[]
    {
        5, 10, 15, 25, 35, 45, 60, 75, 90, 110,
        130, 150, 175, 200, 230, 260, 300, 340, 380, 420
    };
    [Tooltip("Used once a level goes past the end of xpStepTable.")]
    public float xpPerLevelGrowth = 45f;

    [Header("Enemies")]
    public float enemyMoveSpeed = 2.5f;
    public float enemyHealth = 40f;
    public float enemyContactDamage = 12f;
    public float spawnInterval = 1.8f;
    public float minSpawnInterval = 0.6f;
    [Tooltip("Ring distance around the player where new enemies spawn.")]
    public float spawnRadius = 14f;
    [Tooltip("Max concurrent enemies allowed at level 1.")]
    public int baseMaxEnemies = 10;
    [Tooltip("Extra concurrent-enemy cap allowed per player level.")]
    public int maxEnemiesPerLevel = 2;
    [Tooltip("Hard ceiling on concurrent enemies, regardless of level.")]
    public int maxEnemiesCap = 30;
    [Tooltip("Enemies farther than this from the player are despawned, so stragglers don't pile up behind you.")]
    public float enemyDespawnDistance = 45f;

    [Header("Boss Waves")]
    [Tooltip("A boss spawns every this many kills. Set to 0 to disable boss waves.")]
    public int killsPerBossWave = 15;
    public float bossHealth = 600f;
    public float bossMoveSpeed = 3.2f;
    public float bossAttackRange = 3f;
    public float bossAttackDamage = 30f;
    public float bossAttackRadius = 2.5f;
    public float bossTelegraphSeconds = 0.8f;
    public float bossAttackSeconds = 0.25f;
    public float bossRecoverSeconds = 1.2f;
    [Tooltip("Bonus XP awarded for defeating a boss.")]
    public int bossXPReward = 50;

    [Header("Terrain")]
    [Tooltip("Layer(s) considered ground, used to keep spawned enemies/bosses snapped to terrain height.")]
    public LayerMask groundMask;
    public float groundSnapRayHeight = 50f;
}
