using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runtime fix-up for enterable buildings whose NavMeshObstacle still carves the whole footprint
/// (root carve) instead of just the walls. Older placed prefabs baked that full-box carve in, which
/// blocks the doorway and sends enemies straight into the wall instead of through it — rebuilding
/// every prefab isn't required because this runs at combat start and repairs any structure it finds
/// live in the scene.
///
/// SurvivorStructureEncounterSpawner.EnsureNavMeshObstacle() calls <see cref="Fix"/> directly for its
/// own structure at Start; <see cref="EnsureBootstrapped"/> additionally sweeps every enterable
/// structure in the scene as a safety net, in case one is ever missing that component.
/// </summary>
public class SurvivorBuildingNavAccess : MonoBehaviour
{
    /// <summary>Idempotent — call once (SurvivorMinigameController does this on combat start). Spawns
    /// the sweeper itself, so no manual scene setup is required.</summary>
    public static void EnsureBootstrapped()
    {
        if (FindFirstObjectByType<SurvivorBuildingNavAccess>() != null)
            return;

        new GameObject("SurvivorBuildingNavAccess").AddComponent<SurvivorBuildingNavAccess>();
    }

    private void Start()
    {
        BuildingCutawayController[] structures = FindObjectsByType<BuildingCutawayController>(FindObjectsSortMode.None);
        for (int i = 0; i < structures.Length; i++)
            Fix(structures[i].gameObject);
    }

    /// <summary>Disables any root-level carving obstacle on an enterable structure and makes sure
    /// every wall piece under Exterior/Walls_Fadeable + Exterior/Walls_Solid carves individually.</summary>
    public static void Fix(GameObject structureRoot)
    {
        if (structureRoot == null)
            return;

        NavMeshObstacle rootObstacle = structureRoot.GetComponent<NavMeshObstacle>();
        if (rootObstacle != null)
            rootObstacle.enabled = false;

        EnsureWallObstacles(structureRoot.transform, "Exterior/Walls_Fadeable");
        EnsureWallObstacles(structureRoot.transform, "Exterior/Walls_Solid");
    }

    private static void EnsureWallObstacles(Transform root, string path)
    {
        Transform wallsParent = root.Find(path);
        if (wallsParent == null)
            return;

        for (int i = 0; i < wallsParent.childCount; i++)
        {
            Transform wall = wallsParent.GetChild(i);
            NavMeshObstacle obstacle = wall.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
                obstacle = wall.gameObject.AddComponent<NavMeshObstacle>();

            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            // Wall pieces are primitive cubes (BoxCollider size 1 / center 0, footprint from
            // localScale) — the obstacle box works the same way, so this mirrors the collider exactly.
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;
        }
    }
}
