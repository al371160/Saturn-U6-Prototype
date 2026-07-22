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
    public float spawnInterval = 0.9f;
    public float minSpawnInterval = 0.3f;
    [Tooltip("Ring distance around the player where new enemies spawn.")]
    public float spawnRadius = 22f;
    [Tooltip("Max concurrent enemies allowed at level 1.")]
    public int baseMaxEnemies = 20;
    [Tooltip("Linear growth in concurrent-enemy cap per player level.")]
    public float maxEnemiesPerLevel = 8f;
    [Tooltip("Quadratic growth in concurrent-enemy cap per player level, for the late-game surge toward hundreds of enemies.")]
    public float maxEnemiesPerLevelSquared = 0.55f;
    [Tooltip("Hard ceiling on concurrent enemies, regardless of level.")]
    public int maxEnemiesCap = 320;
    [Tooltip("Enemies farther than this from the player are despawned, so stragglers don't pile up behind you.")]
    public float enemyDespawnDistance = 60f;
    [Tooltip("Seconds between ground-snap re-checks per enemy, staggered by instance, to keep hundreds of enemies cheap.")]
    public float groundSnapInterval = 0.2f;

    [Header("Boss Waves")]
    [Tooltip("A boss spawns every this many kills. Set to 0 to disable boss waves.")]
    public int killsPerBossWave = 15;
    [Tooltip("Data-driven boss pool — each spawn picks one at random, so repeated waves see variety rather than the same single boss every time.")]
    public SurvivorBossDataSO[] bossPool;

    [Header("Terrain")]
    [Tooltip("Layer(s) considered ground, used to keep spawned enemies/bosses snapped to terrain height.")]
    public LayerMask groundMask;
    public float groundSnapRayHeight = 50f;

    [Header("Forest")]
    [Tooltip("Tree prefabs scattered across the map at combat start (Tree1/Tree2/round/flat variants).")]
    public GameObject[] forestTreePrefabs;
    public int forestTreeCount = 100;
    public float forestMapHalfExtent = 220f;
    public float forestMinSpacing = 7f;

    [Header("Battle Bus (Multiplayer round entry)")]
    [Tooltip("Off by default so single-player/Campaign testing is unaffected. Enable for the Multiplayer round-entry flow.")]
    public bool useBattleBusIntro = false;
    [Tooltip("Half-width of the square region (centered on world origin) the bus's straight path is randomized across.")]
    public float busPathHalfExtent = 900f;
    public float busFlightAltitude = 160f;
    public float busFlightDuration = 25f;
    [Tooltip("The flight line's closest approach to the map center is randomized within this distance.")]
    public float busCenterPassDistance = 80f;
}
