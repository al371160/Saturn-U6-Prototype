using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runtime factory for primitive-built world dressing (exteriors + interior furniture). Mirrors
/// SurvivorStructurePrefabBuilder's primitive-composition style but builds directly at runtime (no
/// AssetDatabase) so SurvivorWorldDressingSpawner / SurvivorInteriorFiller can scatter props without
/// needing baked prefabs.
/// </summary>
public static class SurvivorWorldPropFactory
{
    private static Shader LitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        return shader != null ? shader : Shader.Find("Standard");
    }

    private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;
        Object.Destroy(part.GetComponent<Collider>());

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
            partRenderer.material = new Material(LitShader()) { color = color };

        return part;
    }

    private static NavMeshObstacle AddObstacle(GameObject root, Vector3 center, Vector3 size)
    {
        NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = center;
        obstacle.size = size;
        return obstacle;
    }

    // --- Exploding barrel ---------------------------------------------------

    public static GameObject CreateExplodingBarrel(Vector3 position, Transform parent = null)
    {
        GameObject root = new GameObject("SurvivorExplodingBarrel");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        Part(root.transform, "BarrelBody", PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0), new Vector3(1.1f, 0.7f, 1.1f), new Color(0.7f, 0.22f, 0.12f));
        Part(root.transform, "BarrelCap", PrimitiveType.Cylinder, new Vector3(0, 1.42f, 0), new Vector3(1.15f, 0.06f, 1.15f), new Color(0.32f, 0.3f, 0.28f));
        Part(root.transform, "BarrelBand", PrimitiveType.Cylinder, new Vector3(0, 0.9f, 0), new Vector3(1.16f, 0.08f, 1.16f), new Color(0.2f, 0.19f, 0.18f));

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.4f;
        collider.radius = 0.55f;
        collider.center = new Vector3(0, 0.7f, 0);

        AddObstacle(root, new Vector3(0, 0.7f, 0), new Vector3(1.2f, 1.4f, 1.2f));
        root.AddComponent<SurvivorExplodingBarrel>();
        return root;
    }

    // --- Static blockers ------------------------------------------------------

    public static GameObject CreateBush(Vector3 position, bool destroyable, Transform parent = null)
    {
        GameObject root = new GameObject("SurvivorWorldProp_Bush");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        Color leafColor = new Color(0.2f + Random.Range(-0.03f, 0.03f), 0.42f + Random.Range(-0.05f, 0.05f), 0.18f);
        Part(root.transform, "LeafA", PrimitiveType.Sphere, new Vector3(0, 0.6f, 0), new Vector3(1.6f, 1.1f, 1.6f), leafColor);
        Part(root.transform, "LeafB", PrimitiveType.Sphere, new Vector3(0.5f, 0.4f, 0.3f), new Vector3(1.1f, 0.8f, 1.1f), leafColor);
        Part(root.transform, "LeafC", PrimitiveType.Sphere, new Vector3(-0.45f, 0.35f, -0.25f), new Vector3(1f, 0.75f, 1f), leafColor);

        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 0.55f, 0);
        collider.radius = 1f;

        AddObstacle(root, new Vector3(0, 0.55f, 0), new Vector3(1.9f, 1.1f, 1.9f));

        if (destroyable)
            root.AddComponent<SurvivorDestructibleProp>().Initialize(18f);

        return root;
    }

    public static GameObject CreateRock(Vector3 position, bool destroyable, Transform parent = null)
    {
        GameObject root = new GameObject("SurvivorWorldProp_Rock");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        Color rockColor = new Color(0.42f, 0.4f, 0.38f) + new Color(Random.Range(-0.04f, 0.04f), 0f, 0f);
        Part(root.transform, "RockMain", PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 1.1f, 1.5f), rockColor);
        Part(root.transform, "RockSmall", PrimitiveType.Cube, new Vector3(0.7f, 0.3f, 0.4f), new Vector3(0.9f, 0.6f, 0.8f), rockColor);
        root.transform.Rotate(0f, Random.Range(0f, 360f), 0f);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.5f, 0);
        collider.size = new Vector3(2.2f, 1.2f, 2f);

        AddObstacle(root, new Vector3(0, 0.5f, 0), new Vector3(2.3f, 1.2f, 2.1f));

        if (destroyable)
            root.AddComponent<SurvivorDestructibleProp>().Initialize(60f);

        return root;
    }

    public static GameObject CreateOuthouse(Vector3 position, SurvivorMinigameController controller = null, Transform parent = null)
    {
        return CreateEnterableBox(
            "SurvivorWorldProp_Outhouse",
            position,
            size: new Vector3(2.4f, 2.6f, 2.4f),
            doorWidth: 1.2f,
            wallColor: new Color(0.45f, 0.35f, 0.22f),
            roofColor: new Color(0.3f, 0.25f, 0.18f),
            controller,
            parent);
    }

    public static GameObject CreateShippingCrate(Vector3 position, SurvivorMinigameController controller = null, Transform parent = null)
    {
        Color crateColor = Random.value < 0.5f ? new Color(0.2f, 0.35f, 0.6f) : new Color(0.6f, 0.22f, 0.18f);
        return CreateEnterableBox(
            "SurvivorWorldProp_ShippingCrate",
            position,
            size: new Vector3(4.2f, 2.4f, 2.4f),
            doorWidth: 1.8f,
            wallColor: crateColor,
            roofColor: crateColor * 0.85f,
            controller,
            parent);
    }

    public static GameObject CreateHut(Vector3 position, SurvivorMinigameController controller = null, Transform parent = null)
    {
        return CreateEnterableBox(
            "SurvivorWorldProp_Hut",
            position,
            size: new Vector3(3.6f, 2.8f, 3.6f),
            doorWidth: 1.5f,
            wallColor: new Color(0.5f, 0.42f, 0.3f),
            roofColor: new Color(0.35f, 0.28f, 0.16f),
            controller,
            parent);
    }

    /// <summary>
    /// Small enterable shell (walls + doorway + roof + interior zone) with a placeholder lootbox inside.
    /// Naming matches SurvivorStructurePrefabBuilder so BuildingCutawayController / NavAccess work.
    /// </summary>
    private static GameObject CreateEnterableBox(
        string name,
        Vector3 position,
        Vector3 size,
        float doorWidth,
        Color wallColor,
        Color roofColor,
        SurvivorMinigameController controller,
        Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.Rotate(0f, Random.Range(0f, 360f), 0f);

        Transform exterior = new GameObject("Exterior").transform;
        exterior.SetParent(root.transform, false);
        Transform wallsFade = new GameObject("Walls_Fadeable").transform;
        wallsFade.SetParent(exterior, false);
        Transform wallsSolid = new GameObject("Walls_Solid").transform;
        wallsSolid.SetParent(exterior, false);
        Transform roofFade = new GameObject("Roof_Fadeable").transform;
        roofFade.SetParent(exterior, false);
        Transform floorT = new GameObject("Floor").transform;
        floorT.SetParent(exterior, false);
        Transform interior = new GameObject("Interior").transform;
        interior.SetParent(root.transform, false);
        Transform props = new GameObject("Props").transform;
        props.SetParent(interior, false);

        float w = size.x;
        float h = size.y;
        float d = size.z;
        float halfW = w * 0.5f;
        float halfD = d * 0.5f;
        float wallY = h * 0.5f;
        float thickness = 0.28f;
        float sideWidth = Mathf.Max(0.35f, (w - doorWidth) * 0.5f);

        // Floor
        PartKeepCollider(floorT, "FloorMesh", PrimitiveType.Cube, new Vector3(0, 0.08f, 0), new Vector3(w, 0.16f, d), new Color(0.32f, 0.28f, 0.22f));

        // Back + sides (fadeable)
        AddWallWithCarve(wallsFade, "WallBack", new Vector3(0, wallY, -halfD + thickness * 0.5f), new Vector3(w, h, thickness), wallColor);
        AddWallWithCarve(wallsFade, "WallLeft", new Vector3(-halfW + thickness * 0.5f, wallY, 0), new Vector3(thickness, h, d), wallColor);
        AddWallWithCarve(wallsFade, "WallRight", new Vector3(halfW - thickness * 0.5f, wallY, 0), new Vector3(thickness, h, d), wallColor);

        // Front split around doorway
        AddWallWithCarve(wallsSolid, "FrontLeft",
            new Vector3(-halfW + sideWidth * 0.5f, wallY, halfD - thickness * 0.5f),
            new Vector3(sideWidth, h, thickness), wallColor);
        AddWallWithCarve(wallsSolid, "FrontRight",
            new Vector3(halfW - sideWidth * 0.5f, wallY, halfD - thickness * 0.5f),
            new Vector3(sideWidth, h, thickness), wallColor);
        AddWallWithCarve(wallsFade, "FrontLintel",
            new Vector3(0, h - 0.35f, halfD - thickness * 0.5f),
            new Vector3(doorWidth, 0.7f, thickness), wallColor);

        Part(roofFade, "RoofMesh", PrimitiveType.Cube, new Vector3(0, h + 0.18f, 0), new Vector3(w + 0.6f, 0.36f, d + 0.6f), roofColor);

        GameObject zoneGo = new GameObject("BuildingInteriorZone");
        zoneGo.transform.SetParent(root.transform, false);
        zoneGo.transform.localPosition = new Vector3(0, wallY, 0);
        BoxCollider zoneCol = zoneGo.AddComponent<BoxCollider>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector3(w - thickness * 2f, h - 0.2f, d - thickness * 2f);
        BuildingInteriorZone zone = zoneGo.AddComponent<BuildingInteriorZone>();

        BuildingCutawayController cutaway = root.AddComponent<BuildingCutawayController>();
        cutaway.AutoCollectFadeRenderers();
        zone.cutaway = cutaway;

        // Placeholder lootbox toward the back of the interior.
        if (controller != null)
        {
            Transform pickupTarget = controller.MinigamePlayer != null ? controller.MinigamePlayer.transform : null;
            SurvivorWeaponDataSO[] weapons = controller.config != null ? controller.config.availableWeapons : null;
            SurvivorBuffDataSO[] buffs = controller.config != null ? controller.config.availableBuffs : null;
            Vector3 lootPos = root.transform.TransformPoint(new Vector3(0f, SurvivorLootCrate.CrateWorldSize * 0.5f, -halfD * 0.35f));
            GameObject loot = CreateInteriorLootCrate(lootPos, controller, pickupTarget, weapons, buffs);
            loot.transform.SetParent(props, true);
            loot.name = "PlaceholderLootbox";
        }
        else
        {
            // Visual placeholder if controller isn't ready yet.
            Part(props, "PlaceholderLootbox", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, -halfD * 0.35f),
                Vector3.one * 1.1f,
                new Color(0.55f, 0.38f, 0.18f));
        }

        return root;
    }

    private static void AddWallWithCarve(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject wall = PartKeepCollider(parent, name, PrimitiveType.Cube, localPos, localScale, color);
        NavMeshObstacle obstacle = wall.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;
    }

    private static GameObject PartKeepCollider(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
            partRenderer.material = new Material(LitShader()) { color = color };

        return part;
    }

    public static GameObject CreateCar(Vector3 position, Transform parent = null)
    {
        GameObject root = new GameObject("SurvivorWorldProp_Car");
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.Rotate(0f, Random.Range(0f, 360f), 0f);

        Color bodyColor = new Color(Random.Range(0.3f, 0.8f), Random.Range(0.3f, 0.8f), Random.Range(0.3f, 0.8f));
        Part(root.transform, "Chassis", PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(1.9f, 0.7f, 4.2f), bodyColor);
        Part(root.transform, "Cabin", PrimitiveType.Cube, new Vector3(0, 1.05f, -0.3f), new Vector3(1.7f, 0.55f, 2.2f), bodyColor * 0.9f);
        Part(root.transform, "WheelF", PrimitiveType.Cylinder, new Vector3(0, 0.3f, 1.5f), new Vector3(0.7f, 0.15f, 0.7f), new Color(0.08f, 0.08f, 0.08f));
        Part(root.transform, "WheelR", PrimitiveType.Cylinder, new Vector3(0, 0.3f, -1.5f), new Vector3(0.7f, 0.15f, 0.7f), new Color(0.08f, 0.08f, 0.08f));

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.6f, 0);
        collider.size = new Vector3(1.9f, 1.2f, 4.2f);

        AddObstacle(root, new Vector3(0, 0.6f, 0), new Vector3(2f, 1.2f, 4.3f));
        return root;
    }

    // --- Interior furniture (no NavMeshObstacle — interiors aren't on the baked NavMesh) --------

    public static GameObject CreateTable(Transform parent, Vector3 localPosition)
    {
        GameObject root = new GameObject("InteriorTable");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;

        Part(root.transform, "Top", PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(2.2f, 0.12f, 1.2f), new Color(0.42f, 0.28f, 0.16f));
        Part(root.transform, "Leg", PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.25f, 0.7f, 0.25f), new Color(0.32f, 0.22f, 0.13f));

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.5f, 0);
        collider.size = new Vector3(2.2f, 1f, 1.2f);
        return root;
    }

    public static GameObject CreateChair(Transform parent, Vector3 localPosition)
    {
        GameObject root = new GameObject("InteriorChair");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Part(root.transform, "Seat", PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(0.6f, 0.1f, 0.6f), new Color(0.35f, 0.24f, 0.15f));
        Part(root.transform, "Back", PrimitiveType.Cube, new Vector3(0, 0.75f, -0.28f), new Vector3(0.6f, 0.6f, 0.1f), new Color(0.35f, 0.24f, 0.15f));

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.45f, 0);
        collider.size = new Vector3(0.6f, 0.9f, 0.6f);
        return root;
    }

    public static GameObject CreateShelf(Transform parent, Vector3 localPosition)
    {
        GameObject root = new GameObject("InteriorShelf");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;

        Part(root.transform, "Frame", PrimitiveType.Cube, new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.4f), new Color(0.4f, 0.3f, 0.2f));
        Part(root.transform, "ShelfA", PrimitiveType.Cube, new Vector3(0, 1.5f, 0.05f), new Vector3(1.55f, 0.08f, 0.45f), new Color(0.3f, 0.22f, 0.14f));
        Part(root.transform, "ShelfB", PrimitiveType.Cube, new Vector3(0, 0.6f, 0.05f), new Vector3(1.55f, 0.08f, 0.45f), new Color(0.3f, 0.22f, 0.14f));

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 1f, 0);
        collider.size = new Vector3(1.6f, 2f, 0.4f);
        return root;
    }

    /// <summary>Interior loot crate — reuses SurvivorLootCrate so it drops weapons/buffs exactly
    /// like the exterior crates SurvivorMinigameController.SpawnLootCrates places.</summary>
    public static GameObject CreateInteriorLootCrate(
        Vector3 position,
        SurvivorMinigameController controller,
        Transform pickupTarget,
        SurvivorWeaponDataSO[] weapons,
        SurvivorBuffDataSO[] buffs)
    {
        GameObject root = new GameObject("SurvivorLootCrate");
        root.transform.position = position;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = Vector3.one * SurvivorLootCrate.CrateWorldSize;

        SurvivorLootCrate crate = root.AddComponent<SurvivorLootCrate>();
        crate.Initialize(controller, pickupTarget, weapons, buffs, 50f, 0.65f);
        return root;
    }
}
