using UnityEngine;

public class SurvivorMinigameSpawner : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private SurvivorMinigameConfig config;
    private Transform player;
    private Transform enemyRoot;
    private GameObject enemyTemplate;
    private float spawnTimer;
    private float currentSpawnInterval;

    public void Initialize(
        SurvivorMinigameController owner,
        SurvivorMinigameConfig levelConfig,
        Transform playerTransform,
        Transform enemyParent,
        GameObject enemyPrefab)
    {
        controller = owner;
        config = levelConfig;
        player = playerTransform;
        enemyRoot = enemyParent;
        enemyTemplate = enemyPrefab;
        currentSpawnInterval = config.spawnInterval;
        spawnTimer = 0.5f;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || config == null || player == null)
            return;

        if (enemyRoot.childCount >= config.maxEnemies)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        SpawnEnemy();
        spawnTimer = currentSpawnInterval;
        currentSpawnInterval = Mathf.Max(config.minSpawnInterval, currentSpawnInterval * 0.985f);
    }

    private void SpawnEnemy()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        if (randomCircle.sqrMagnitude < 0.01f)
            randomCircle = Vector2.right;

        Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y) * config.spawnRadius;
        Vector3 spawnPosition = player.position + spawnOffset;
        spawnPosition = controller.ClampToArena(spawnPosition);

        GameObject enemyObject = Instantiate(enemyTemplate, spawnPosition, Quaternion.identity, enemyRoot);
        enemyObject.SetActive(true);

        SurvivorMinigameEnemy enemy = enemyObject.GetComponent<SurvivorMinigameEnemy>();
        enemy.Initialize(
            controller,
            player,
            config.enemyHealth,
            config.enemyMoveSpeed,
            config.enemyContactDamage);
    }
}
