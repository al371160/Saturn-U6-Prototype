using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime dressing pass for enterable building interiors — finds Interior/Props under every
/// BuildingInteriorZone in the scene and adds a few furniture primitives plus one SurvivorLootCrate,
/// via SurvivorWorldPropFactory. Additive on top of whatever a structure's prefab already authored
/// under Interior/Props (see SurvivorStructurePrefabBuilder's hand-placed furniture), guarded by a
/// marker child so it never double-fills the same interior.
/// </summary>
public static class SurvivorInteriorFiller
{
    private const string MarkerName = "SurvivorInteriorFillerMarker";

    public static void FillAll(SurvivorMinigameController controller, SurvivorMinigameConfig config, Transform pickupTarget)
    {
        BuildingInteriorZone[] zones = Object.FindObjectsByType<BuildingInteriorZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
            Fill(zones[i], controller, config, pickupTarget);
    }

    private static void Fill(BuildingInteriorZone zone, SurvivorMinigameController controller, SurvivorMinigameConfig config, Transform pickupTarget)
    {
        if (zone == null)
            return;

        Transform structureRoot = zone.transform.parent;
        if (structureRoot == null)
            return;

        Transform props = structureRoot.Find("Interior/Props");
        if (props == null)
            return;

        if (props.Find(MarkerName) != null)
            return;

        new GameObject(MarkerName).transform.SetParent(props, false);

        AddFurniture(props);
        AddLootCrate(props, controller, config, pickupTarget);
    }

    private static void AddFurniture(Transform props)
    {
        int furnitureCount = Random.Range(2, 4);
        for (int i = 0; i < furnitureCount; i++)
        {
            Vector2 jitter = Random.insideUnitCircle * 2.5f;
            Vector3 localPosition = new Vector3(jitter.x, 0f, jitter.y);

            switch (Random.Range(0, 3))
            {
                case 0:
                    SurvivorWorldPropFactory.CreateTable(props, localPosition);
                    break;
                case 1:
                    SurvivorWorldPropFactory.CreateChair(props, localPosition);
                    break;
                default:
                    SurvivorWorldPropFactory.CreateShelf(props, localPosition);
                    break;
            }
        }
    }

    private static void AddLootCrate(Transform props, SurvivorMinigameController controller, SurvivorMinigameConfig config, Transform pickupTarget)
    {
        if (controller == null || config == null)
            return;

        SurvivorWeaponDataSO[] weapons = config.availableWeapons;

        List<SurvivorBuffDataSO> buffPool = new List<SurvivorBuffDataSO>();
        if (config.availableBuffs != null)
        {
            foreach (SurvivorBuffDataSO buff in config.availableBuffs)
            {
                if (buff != null)
                    buffPool.Add(buff);
            }
        }

        if ((weapons == null || weapons.Length == 0) && buffPool.Count == 0)
            return;

        Vector2 jitter = Random.insideUnitCircle * 1.5f;
        Vector3 worldPosition = props.position + new Vector3(jitter.x, SurvivorLootCrate.CrateWorldSize * 0.5f, jitter.y);

        SurvivorWorldPropFactory.CreateInteriorLootCrate(worldPosition, controller, pickupTarget, weapons, buffPool.ToArray());
    }
}
