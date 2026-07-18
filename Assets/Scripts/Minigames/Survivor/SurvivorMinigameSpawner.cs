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
    private float despawnCheckTimer;

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
        despawnCheckTimer = 1f;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || config == null || player == null)
            return;

        despawnCheckTimer -= Time.deltaTime;
        if (despawnCheckTimer <= 0f)
        {
            despawnCheckTimer = 1f;
            DespawnStragglers();
        }

        if (controller.IsBossActive)
            return;

        if (enemyRoot.childCount >= controller.GetCurrentMaxEnemies())
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

        float groundOffset = enemyTemplate.transform.localScale.y * 0.5f;

        Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y) * config.spawnRadius;
        Vector3 spawnPosition = player.position + spawnOffset;
        spawnPosition = SurvivorGroundUtility.SnapToGround(spawnPosition, config.groundMask, config.groundSnapRayHeight, groundOffset);

        GameObject enemyObject = Instantiate(enemyTemplate, spawnPosition, Quaternion.identity, enemyRoot);
        enemyObject.SetActive(true);

        SurvivorMinigameEnemy enemy = enemyObject.GetComponent<SurvivorMinigameEnemy>();
        enemy.Initialize(
            controller,
            player,
            config.enemyHealth,
            config.enemyMoveSpeed,
            config.enemyContactDamage,
            config.enemyXPDrop,
            config.groundMask,
            config.groundSnapRayHeight,
            groundOffset,
            config.groundSnapInterval);

        if (controller.enemyEliteness > 0f && Random.value < controller.enemyEliteness)
            enemy.MakeElite();
    }

    private void DespawnStragglers()
    {
        float despawnSqrDistance = config.enemyDespawnDistance * config.enemyDespawnDistance;

        for (int i = enemyRoot.childCount - 1; i >= 0; i--)
        {
            Transform enemyTransform = enemyRoot.GetChild(i);
            if (enemyTransform.GetComponent<SurvivorBossEnemy>() != null)
                continue;

            float sqrDistance = (enemyTransform.position - player.position).sqrMagnitude;
            if (sqrDistance > despawnSqrDistance)
                Destroy(enemyTransform.gameObject);
        }
    }
}
