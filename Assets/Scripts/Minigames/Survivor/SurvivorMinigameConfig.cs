using UnityEngine;

[CreateAssetMenu(menuName = "Minigames/Survivor Minigame Config")]
public class SurvivorMinigameConfig : ScriptableObject
{
    [Header("Level Goal")]
    [Tooltip("Defeat this many enemies to win. Set to 0 to use survive timer instead.")]
    public int killsToWin = 15;

    [Tooltip("Used when killsToWin is 0.")]
    public float surviveSeconds = 45f;

    [Header("Player")]
    public float playerMoveSpeed = 7f;
    public float playerMaxHealth = 100f;
    public float contactDamageCooldown = 0.5f;

    [Header("Weapon")]
    public int orbitCount = 3;
    public float orbitRadius = 1.4f;
    public float orbitSpeed = 220f;
    public float orbitDamage = 25f;
    public float orbitHitRadius = 0.45f;

    [Header("Enemies")]
    public float enemyMoveSpeed = 2.5f;
    public float enemyHealth = 40f;
    public float enemyContactDamage = 12f;
    public float spawnInterval = 1.8f;
    public float minSpawnInterval = 0.6f;
    public float spawnRadius = 14f;
    public int maxEnemies = 35;

    [Header("Arena")]
    public float arenaHalfSize = 18f;
}
