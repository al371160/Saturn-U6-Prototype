using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Preplaced near a structure. When the player enters <see cref="activationRadius"/>, spawns a
/// burst of monsters at offsets that are intentionally off-camera (outside the main camera frustum
/// and at least <see cref="minOffscreenDistance"/> from the player). Inspired by common survivor/
/// horde patterns: validate spawn candidates against camera visibility before Instantiating.
/// </summary>
public class SurvivorStructureEncounterSpawner : MonoBehaviour
{
    [Header("Trigger")]
    public float activationRadius = 48f;
    public bool oneShot = true;
    public float cooldown = 45f;

    [Header("Spawn Burst")]
    public int spawnCount = 4;
    [Tooltip("Local XZ offsets relative to this transform used as candidate spawn anchors.")]
    public Vector3[] spawnOffsets = new Vector3[]
    {
        new Vector3(18f, 0f, 12f),
        new Vector3(-16f, 0f, 14f),
        new Vector3(14f, 0f, -18f),
        new Vector3(-20f, 0f, -10f),
        new Vector3(22f, 0f, -4f),
        new Vector3(-8f, 0f, 22f),
    };

    [Header("Offscreen Rules")]
    public float minOffscreenDistance = 16f;
    public float maxSpawnDistance = 55f;
    public int maxPlacementAttempts = 12;
    public LayerMask blockedMask;

    private bool hasTriggered;
    private float cooldownTimer;
    private SurvivorMinigameController controller;
    private Camera mainCamera;

    private void Start()
    {
        controller = FindFirstObjectByType<SurvivorMinigameController>();
        if (blockedMask.value == 0)
            blockedMask = LayerMask.GetMask("Default", "Obstacle", "Ground");

        EnsureNavMeshObstacle();
    }

    /// <summary>Carve this structure's footprint out of the NavMesh so agents path around walls
    /// even if the bake didn't include post-placed prefabs.</summary>
    private void EnsureNavMeshObstacle()
    {
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = gameObject.AddComponent<NavMeshObstacle>();

        Bounds bounds = CalculateLocalBounds();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = bounds.center;
        obstacle.size = new Vector3(
            Mathf.Max(2f, bounds.size.x),
            Mathf.Max(2f, bounds.size.y),
            Mathf.Max(2f, bounds.size.z));
    }

    private Bounds CalculateLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(Vector3.up, new Vector3(8f, 4f, 8f));

        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            world.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = transform.InverseTransformPoint(world.center);
        Vector3 scale = transform.lossyScale;
        Vector3 localSize = new Vector3(
            scale.x != 0f ? Mathf.Abs(world.size.x / scale.x) : world.size.x,
            scale.y != 0f ? Mathf.Abs(world.size.y / scale.y) : world.size.y,
            scale.z != 0f ? Mathf.Abs(world.size.z / scale.z) : world.size.z);
        return new Bounds(localCenter, localSize);
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused)
            return;

        if (oneShot && hasTriggered)
            return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        SurvivorMinigamePlayer player = controller.MinigamePlayer;
        if (player == null)
            return;

        float sqr = (player.transform.position - transform.position).sqrMagnitude;
        if (sqr > activationRadius * activationRadius)
            return;

        if (TrySpawnBurst(player.transform))
        {
            hasTriggered = true;
            cooldownTimer = cooldown;
        }
    }

    private bool TrySpawnBurst(Transform player)
    {
        SurvivorMinigameSpawner spawner = controller.EnemySpawner;
        if (spawner == null)
            spawner = FindFirstObjectByType<SurvivorMinigameSpawner>();
        if (spawner == null)
            return false;

        if (mainCamera == null)
            mainCamera = Camera.main;

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryFindOffscreenPoint(player, out Vector3 point))
                continue;

            if (spawner.SpawnEnemyAt(point) != null)
                spawned++;
        }

        return spawned > 0;
    }

    private bool TryFindOffscreenPoint(Transform player, out Vector3 worldPoint)
    {
        worldPoint = transform.position;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 offset;
            if (spawnOffsets != null && spawnOffsets.Length > 0 && attempt < spawnOffsets.Length)
                offset = spawnOffsets[attempt];
            else
            {
                Vector2 ring = Random.insideUnitCircle.normalized;
                if (ring.sqrMagnitude < 0.01f)
                    ring = Vector2.right;
                float dist = Random.Range(minOffscreenDistance, maxSpawnDistance);
                offset = new Vector3(ring.x, 0f, ring.y) * dist;
            }

            Vector3 candidate = transform.position + offset;
            float playerDist = Vector3.Distance(candidate, player.position);
            if (playerDist < minOffscreenDistance || playerDist > maxSpawnDistance)
                continue;

            if (IsVisibleToCamera(candidate))
                continue;

            if (Physics.CheckSphere(candidate + Vector3.up, 0.75f, blockedMask, QueryTriggerInteraction.Ignore))
                continue;

            worldPoint = candidate;
            return true;
        }

        // Fallback: behind the player relative to the structure, still at min distance.
        Vector3 away = (transform.position - player.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -player.forward;
        away.Normalize();
        worldPoint = player.position + away * minOffscreenDistance;
        return !IsVisibleToCamera(worldPoint);
    }

    private bool IsVisibleToCamera(Vector3 worldPoint)
    {
        if (mainCamera == null)
            return false;

        Vector3 viewport = mainCamera.WorldToViewportPoint(worldPoint);
        bool inFront = viewport.z > 0f;
        bool onScreen = viewport.x > -0.05f && viewport.x < 1.05f && viewport.y > -0.05f && viewport.y < 1.05f;
        return inFront && onScreen;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        if (spawnOffsets == null)
            return;

        for (int i = 0; i < spawnOffsets.Length; i++)
            Gizmos.DrawWireSphere(transform.position + spawnOffsets[i], 1.2f);
    }
}
