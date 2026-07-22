using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scatters primitive world-dressing props (SurvivorWorldPropFactory) across the map — a couple
/// ring each landmark (SurvivorLandmarkMarker), the rest land on random map anchors. Mirrors
/// SurvivorMinigameController.SpawnLootCrates' anchor-grid + shuffle pattern and SpawnForestTrees'
/// blocked-spot check so dressing doesn't spawn stacked on top of structures.
/// </summary>
public static class SurvivorWorldDressingSpawner
{
    private enum PropKind { Bush, Rock, Barrel, ShippingCrate, Hut, Car, Outhouse }

    private const float MapHalfExtent = 220f;
    private const float AreaStep = 44f;
    private const int RandomAnchorPropCount = 46;

    public static Transform Spawn(SurvivorMinigameConfig config, SurvivorMinigameController controller = null)
    {
        if (config == null)
            return null;

        Transform root = new GameObject("SurvivorWorldDressing").transform;

        SurvivorLandmarkMarker[] landmarks = Object.FindObjectsByType<SurvivorLandmarkMarker>(FindObjectsSortMode.None);
        SpawnNearLandmarks(root, landmarks, config, controller);

        Vector3[] anchors = ShuffleAreas(GetAnchors());
        SpawnAtRandomAnchors(root, anchors, config, controller);

        return root;
    }

    /// <summary>A couple of decorative blockers ringing each landmark, offset far enough out that
    /// they don't clip into the structure's own footprint.</summary>
    private static void SpawnNearLandmarks(Transform root, SurvivorLandmarkMarker[] landmarks, SurvivorMinigameConfig config, SurvivorMinigameController controller)
    {
        for (int i = 0; i < landmarks.Length; i++)
        {
            if (landmarks[i] == null)
                continue;

            int propCount = Random.Range(1, 4);
            for (int p = 0; p < propCount; p++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(10f, 18f);
                Vector3 candidate = landmarks[i].transform.position + new Vector3(ring.x, 0f, ring.y);
                PropKind kind = Random.value < 0.5f ? PropKind.Bush : PropKind.Rock;
                TryPlaceProp(root, candidate, kind, config, controller);
            }
        }
    }

    /// <summary>Random subset of map anchors (not player-relative), same anchor-grid pattern as
    /// SpawnLootCrates/SpawnGroundWeapons but on an independent grid so dressing doesn't cluster on
    /// the exact same points as loot.</summary>
    private static void SpawnAtRandomAnchors(Transform root, Vector3[] anchors, SurvivorMinigameConfig config, SurvivorMinigameController controller)
    {
        int count = Mathf.Min(RandomAnchorPropCount, anchors.Length);
        for (int i = 0; i < count; i++)
        {
            Vector2 jitter = Random.insideUnitCircle * 14f;
            Vector3 candidate = anchors[i] + new Vector3(jitter.x, 0f, jitter.y);
            TryPlaceProp(root, candidate, PickRandomKind(), config, controller);
        }
    }

    private static PropKind PickRandomKind()
    {
        float roll = Random.value;
        if (roll < 0.28f) return PropKind.Bush;
        if (roll < 0.5f) return PropKind.Rock;
        if (roll < 0.65f) return PropKind.Barrel;
        if (roll < 0.78f) return PropKind.ShippingCrate;
        if (roll < 0.88f) return PropKind.Hut;
        if (roll < 0.96f) return PropKind.Car;
        return PropKind.Outhouse;
    }

    private static void TryPlaceProp(Transform root, Vector3 candidate, PropKind kind, SurvivorMinigameConfig config, SurvivorMinigameController controller)
    {
        if (IsBlocked(candidate))
            return;

        Vector3 spawnPosition = SurvivorGroundUtility.SnapToGround(candidate, config.groundMask, config.groundSnapRayHeight, 0f);

        GameObject prop = Build(kind, spawnPosition, controller);
        if (prop != null)
            prop.transform.SetParent(root, true);
    }

    /// <summary>Same two-stage check as SpawnForestTrees: cheap gate first, then reject only on
    /// sizeable non-terrain colliders (buildings/landmarks) so small debris doesn't block placement.</summary>
    private static bool IsBlocked(Vector3 candidate)
    {
        if (!Physics.CheckSphere(candidate + Vector3.up, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        Collider[] hits = Physics.OverlapSphere(candidate + Vector3.up * 1.2f, 1.8f, ~0, QueryTriggerInteraction.Ignore);
        for (int h = 0; h < hits.Length; h++)
        {
            if (hits[h] == null || hits[h] is TerrainCollider)
                continue;

            Bounds b = hits[h].bounds;
            if (b.size.y > 1.2f && b.extents.x + b.extents.z > 2f)
                return true;
        }

        return false;
    }

    private static GameObject Build(PropKind kind, Vector3 position, SurvivorMinigameController controller)
    {
        switch (kind)
        {
            case PropKind.Bush: return SurvivorWorldPropFactory.CreateBush(position, Random.value < 0.6f);
            case PropKind.Rock: return SurvivorWorldPropFactory.CreateRock(position, Random.value < 0.35f);
            case PropKind.Barrel: return SurvivorWorldPropFactory.CreateExplodingBarrel(position);
            case PropKind.ShippingCrate: return SurvivorWorldPropFactory.CreateShippingCrate(position, controller);
            case PropKind.Hut: return SurvivorWorldPropFactory.CreateHut(position, controller);
            case PropKind.Car: return SurvivorWorldPropFactory.CreateCar(position);
            case PropKind.Outhouse: return SurvivorWorldPropFactory.CreateOuthouse(position, controller);
            default: return null;
        }
    }

    private static Vector3[] ShuffleAreas(Vector3[] source)
    {
        Vector3[] shuffled = (Vector3[])source.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }

        return shuffled;
    }

    /// <summary>Dense grid of world anchors — mirrors SurvivorMinigameController.GetPredeterminedCrateAreas,
    /// but on its own grid/step so props don't land exactly on top of crates/weapons/consumables.</summary>
    private static Vector3[] GetAnchors()
    {
        List<Vector3> areas = new List<Vector3>(128);
        for (float x = -MapHalfExtent; x <= MapHalfExtent + 0.01f; x += AreaStep)
        {
            for (float z = -MapHalfExtent; z <= MapHalfExtent + 0.01f; z += AreaStep)
                areas.Add(new Vector3(x, 0f, z));
        }

        return areas.ToArray();
    }
}
