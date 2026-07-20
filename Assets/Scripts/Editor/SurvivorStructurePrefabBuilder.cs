#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Saturn/Build Survivor Structures — primitive POI prefabs with optional enterable
/// interiors and local roof/wall cutaway fade (Saturn/BuildingCutaway).
/// </summary>
public static class SurvivorStructurePrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Structures";
    private const string MatFolder = "Assets/Prefabs/Structures/Materials";
    private const string CutawayShaderName = "Saturn/BuildingCutaway";

    [MenuItem("Saturn/Build Survivor Structures")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MatFolder);

        // Enterable first-pass remodel
        BuildEnterableCabin();
        BuildEnterableHouse();
        BuildEnterableWarehouse();
        BuildEnterableLakeHouse();
        BuildEnterableFireStation();
        BuildEnterableMansion();

        // Non-enterable / open structures
        BuildWaterTower();
        BuildTentCamp();
        BuildStreetBlock();

        // Next wave
        BuildEnterableBarn();
        BuildEnterablePoliceStation();
        BuildEnterableBank();
        BuildOuthouse();
        BuildShippingContainerYard();
        BuildGreenhouse();
        BuildHuntingPerch();
        BuildEnterableSaloon();
        BuildPavilion();
        BuildBridgeSpan();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SurvivorStructurePrefabBuilder: rebuilt structure prefabs (enterable + cutaway + new wave).");
    }

    // --- Enterable shells -------------------------------------------------

    private static void BuildEnterableCabin()
    {
        GameObject root = BeginEnterable("WoodsCabin", new Vector3(10f, 4.4f, 8f), new Color(0.45f, 0.28f, 0.16f), new Color(0.22f, 0.45f, 0.28f), doorWidth: 2.4f);
        PartUnder(Find(root, "Interior/Props"), "Table", PrimitiveType.Cube, new Vector3(0, 0.7f, -1f), new Vector3(2.5f, 0.8f, 1.2f), new Color(0.4f, 0.25f, 0.14f), false);
        PartUnder(Find(root, "Interior/Props"), "Bed", PrimitiveType.Cube, new Vector3(-2.5f, 0.45f, 1.5f), new Vector3(2f, 0.5f, 3f), new Color(0.5f, 0.35f, 0.3f), false);
        Part(root, "Chimney", PrimitiveType.Cube, new Vector3(3.2f, 5.8f, -2f), new Vector3(1.2f, 2.2f, 1.2f), new Color(0.35f, 0.32f, 0.3f), false);
        FinishEnterable(root, "Cabin", new Color(0.35f, 0.7f, 0.4f), 4, 40f);
    }

    private static void BuildEnterableHouse()
    {
        GameObject root = BeginEnterable("RedHouse", new Vector3(14f, 5f, 11f), new Color(0.72f, 0.28f, 0.22f), new Color(0.35f, 0.12f, 0.1f), doorWidth: 2.8f);
        PartUnder(Find(root, "Interior/Props"), "Table", PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(3f, 0.9f, 1.5f), new Color(0.45f, 0.3f, 0.18f), false);
        PartUnder(Find(root, "Interior/Props"), "Fridge", PrimitiveType.Cube, new Vector3(4f, 1.2f, -3f), new Vector3(1.4f, 2.4f, 1.2f), new Color(0.75f, 0.78f, 0.8f), false);
        Part(root, "Bush", PrimitiveType.Sphere, new Vector3(-6f, 0.8f, 6f), new Vector3(2.5f, 1.6f, 2.5f), new Color(0.2f, 0.45f, 0.18f), false);
        FinishEnterable(root, "House", new Color(0.9f, 0.35f, 0.3f), 5, 45f);
    }

    private static void BuildEnterableWarehouse()
    {
        GameObject root = BeginEnterable("MetalWarehouse", new Vector3(22f, 8f, 14f), new Color(0.55f, 0.58f, 0.62f), new Color(0.35f, 0.38f, 0.42f), doorWidth: 5f, wallThickness: 0.45f);
        PartUnder(Find(root, "Interior/Props"), "CrateA", PrimitiveType.Cube, new Vector3(-6f, 1f, -3f), new Vector3(3f, 2f, 3f), new Color(0.55f, 0.4f, 0.2f), false);
        PartUnder(Find(root, "Interior/Props"), "CrateB", PrimitiveType.Cube, new Vector3(5f, 1f, 2f), new Vector3(3f, 2f, 3f), new Color(0.5f, 0.38f, 0.18f), false);
        PartUnder(Find(root, "Interior/Props"), "Barrel", PrimitiveType.Cylinder, new Vector3(3f, 1f, -4f), new Vector3(1.6f, 1f, 1.6f), new Color(0.7f, 0.25f, 0.15f), false);
        FinishEnterable(root, "Warehouse", new Color(0.6f, 0.65f, 0.7f), 6, 55f);
    }

    private static void BuildEnterableLakeHouse()
    {
        GameObject root = BeginEnterable("LakeHouse", new Vector3(12f, 4.8f, 9f), new Color(0.85f, 0.82f, 0.75f), new Color(0.2f, 0.35f, 0.5f), doorWidth: 2.6f);
        Part(root, "Deck", PrimitiveType.Cube, new Vector3(0, 0.25f, 7.5f), new Vector3(14f, 0.3f, 6f), new Color(0.55f, 0.4f, 0.25f), false);
        Part(root, "Dock", PrimitiveType.Cube, new Vector3(0, 0.2f, 14f), new Vector3(4f, 0.25f, 8f), new Color(0.5f, 0.38f, 0.22f), false);
        PartUnder(Find(root, "Interior/Props"), "Couch", PrimitiveType.Cube, new Vector3(0, 0.55f, -1.5f), new Vector3(3.5f, 0.8f, 1.4f), new Color(0.35f, 0.4f, 0.5f), false);
        FinishEnterable(root, "Lake House", new Color(0.35f, 0.55f, 0.85f), 4, 48f);
    }

    private static void BuildEnterableFireStation()
    {
        GameObject root = BeginEnterable("FireStation", new Vector3(20f, 7f, 14f), new Color(0.75f, 0.2f, 0.15f), new Color(0.25f, 0.25f, 0.28f), doorWidth: 6f, wallThickness: 0.4f);
        Part(root, "Tower", PrimitiveType.Cube, new Vector3(-8f, 8f, -4f), new Vector3(4f, 9f, 4f), new Color(0.7f, 0.18f, 0.14f), false);
        PartUnder(Find(root, "Interior/Props"), "Lockers", PrimitiveType.Cube, new Vector3(6f, 1.5f, -4f), new Vector3(4f, 3f, 1f), new Color(0.3f, 0.32f, 0.35f), false);
        FinishEnterable(root, "Fire Station", new Color(1f, 0.25f, 0.2f), 5, 50f);
    }

    private static void BuildEnterableMansion()
    {
        GameObject root = BeginEnterable("EstateMansion", new Vector3(22f, 8f, 16f), new Color(0.82f, 0.78f, 0.7f), new Color(0.28f, 0.22f, 0.2f), doorWidth: 3.5f, wallThickness: 0.4f);
        Part(root, "WingL", PrimitiveType.Cube, new Vector3(-14f, 3f, 2f), new Vector3(8f, 6f, 12f), new Color(0.78f, 0.74f, 0.66f), false);
        Part(root, "WingR", PrimitiveType.Cube, new Vector3(14f, 3f, 2f), new Vector3(8f, 6f, 12f), new Color(0.78f, 0.74f, 0.66f), false);
        PartUnder(Find(root, "Interior/Props"), "DiningTable", PrimitiveType.Cube, new Vector3(0, 0.85f, 0), new Vector3(5f, 1f, 2.2f), new Color(0.4f, 0.28f, 0.16f), false);
        PartUnder(Find(root, "Interior/Props"), "CourtyardTree", PrimitiveType.Capsule, new Vector3(0, 2.2f, -4f), new Vector3(2f, 2.5f, 2f), new Color(0.2f, 0.45f, 0.2f), false);
        FinishEnterable(root, "Mansion", new Color(0.9f, 0.75f, 0.55f), 7, 60f);
    }

    private static void BuildEnterableBarn()
    {
        GameObject root = BeginEnterable("Barn", new Vector3(24f, 9f, 16f), new Color(0.55f, 0.22f, 0.16f), new Color(0.25f, 0.4f, 0.28f), doorWidth: 5.5f, wallThickness: 0.45f);
        PartUnder(Find(root, "Interior/Props"), "Hay", PrimitiveType.Cube, new Vector3(-6f, 1.2f, 3f), new Vector3(4f, 2.4f, 3f), new Color(0.75f, 0.65f, 0.3f), false);
        PartUnder(Find(root, "Interior/Props"), "Stall", PrimitiveType.Cube, new Vector3(6f, 1.5f, -3f), new Vector3(5f, 3f, 0.3f), new Color(0.4f, 0.28f, 0.16f), false);
        FinishEnterable(root, "Barn", new Color(0.7f, 0.35f, 0.25f), 6, 55f);
    }

    private static void BuildEnterablePoliceStation()
    {
        GameObject root = BeginEnterable("PoliceStation", new Vector3(26f, 7f, 18f), new Color(0.35f, 0.4f, 0.5f), new Color(0.2f, 0.22f, 0.28f), doorWidth: 3.2f, wallThickness: 0.4f);
        PartUnder(Find(root, "Interior/Props"), "Desk", PrimitiveType.Cube, new Vector3(0, 0.8f, 2f), new Vector3(4f, 1f, 1.5f), new Color(0.3f, 0.3f, 0.32f), false);
        PartUnder(Find(root, "Interior/Props"), "CellBars", PrimitiveType.Cube, new Vector3(-7f, 1.8f, -4f), new Vector3(0.2f, 3.5f, 6f), new Color(0.55f, 0.55f, 0.6f), false);
        FinishEnterable(root, "Police Station", new Color(0.4f, 0.5f, 0.7f), 6, 55f);
    }

    private static void BuildEnterableBank()
    {
        GameObject root = BeginEnterable("Bank", new Vector3(18f, 6.5f, 14f), new Color(0.7f, 0.68f, 0.62f), new Color(0.35f, 0.3f, 0.25f), doorWidth: 3f, wallThickness: 0.4f);
        PartUnder(Find(root, "Interior/Props"), "Counter", PrimitiveType.Cube, new Vector3(0, 1f, 1f), new Vector3(8f, 1.2f, 1.2f), new Color(0.45f, 0.35f, 0.25f), false);
        PartUnder(Find(root, "Interior/Props"), "Vault", PrimitiveType.Cube, new Vector3(0, 1.8f, -4.5f), new Vector3(5f, 3.5f, 2f), new Color(0.35f, 0.38f, 0.4f), false);
        FinishEnterable(root, "Bank", new Color(0.85f, 0.75f, 0.4f), 5, 50f);
    }

    private static void BuildEnterableSaloon()
    {
        GameObject root = BeginEnterable("Saloon", new Vector3(16f, 6f, 12f), new Color(0.55f, 0.35f, 0.22f), new Color(0.3f, 0.18f, 0.12f), doorWidth: 3f);
        PartUnder(Find(root, "Interior/Props"), "Bar", PrimitiveType.Cube, new Vector3(0, 1f, -3f), new Vector3(8f, 1.2f, 1.5f), new Color(0.4f, 0.25f, 0.15f), false);
        PartUnder(Find(root, "Interior/Props"), "Piano", PrimitiveType.Cube, new Vector3(-4f, 0.9f, 2f), new Vector3(2.5f, 1.4f, 1.2f), new Color(0.15f, 0.12f, 0.1f), false);
        FinishEnterable(root, "Saloon", new Color(0.75f, 0.45f, 0.25f), 4, 45f);
    }

    // --- Open / tiny structures -------------------------------------------

    private static void BuildWaterTower()
    {
        GameObject root = NewRoot("WaterTower");
        Part(root, "LegN", PrimitiveType.Cube, new Vector3(2.2f, 4f, 2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f), false);
        Part(root, "LegE", PrimitiveType.Cube, new Vector3(-2.2f, 4f, 2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f), false);
        Part(root, "LegS", PrimitiveType.Cube, new Vector3(-2.2f, 4f, -2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f), false);
        Part(root, "LegW", PrimitiveType.Cube, new Vector3(2.2f, 4f, -2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f), false);
        Part(root, "Tank", PrimitiveType.Cylinder, new Vector3(0, 10f, 0), new Vector3(7f, 3f, 7f), new Color(0.7f, 0.22f, 0.18f), false);
        Finish(root, "Water Tower", new Color(0.85f, 0.3f, 0.25f), 3, 42f);
    }

    private static void BuildTentCamp()
    {
        GameObject root = NewRoot("TentCamp");
        Part(root, "TentA", PrimitiveType.Cube, new Vector3(-4f, 1.4f, 0), new Vector3(5f, 2.8f, 5f), new Color(0.55f, 0.5f, 0.35f), false);
        Part(root, "TentARoof", PrimitiveType.Cube, new Vector3(-4f, 3.2f, 0), new Vector3(5.5f, 0.8f, 5.5f), new Color(0.4f, 0.55f, 0.35f), false);
        Part(root, "TentB", PrimitiveType.Cube, new Vector3(4f, 1.2f, 3f), new Vector3(4.5f, 2.4f, 4.5f), new Color(0.5f, 0.45f, 0.3f), false);
        Part(root, "Campfire", PrimitiveType.Cylinder, new Vector3(0, 0.4f, -3f), new Vector3(2f, 0.4f, 2f), new Color(0.35f, 0.2f, 0.1f), false);
        Finish(root, "Tent Camp", new Color(0.55f, 0.7f, 0.4f), 3, 35f);
    }

    private static void BuildStreetBlock()
    {
        GameObject root = NewRoot("StreetBlock");
        Part(root, "Road", PrimitiveType.Cube, new Vector3(0, 0.08f, 0), new Vector3(40f, 0.15f, 10f), new Color(0.22f, 0.22f, 0.24f), false);
        Part(root, "CurbN", PrimitiveType.Cube, new Vector3(0, 0.2f, 5.3f), new Vector3(40f, 0.25f, 0.6f), new Color(0.45f, 0.45f, 0.48f), false);
        Part(root, "CurbS", PrimitiveType.Cube, new Vector3(0, 0.2f, -5.3f), new Vector3(40f, 0.25f, 0.6f), new Color(0.45f, 0.45f, 0.48f), false);
        Part(root, "Stripe", PrimitiveType.Cube, new Vector3(0, 0.16f, 0), new Vector3(38f, 0.02f, 0.35f), new Color(0.85f, 0.8f, 0.35f), false);
        Finish(root, "Street", new Color(0.4f, 0.4f, 0.45f), 2, 30f);
    }

    private static void BuildOuthouse()
    {
        GameObject root = NewRoot("Outhouse");
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0, 1.4f, 0), new Vector3(2.2f, 2.8f, 2.2f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 3f, 0), new Vector3(2.6f, 0.4f, 2.6f), new Color(0.3f, 0.25f, 0.18f), false);
        Finish(root, "Outhouse", new Color(0.55f, 0.4f, 0.25f), 1, 22f);
    }

    private static void BuildShippingContainerYard()
    {
        GameObject root = NewRoot("ShippingContainerYard");
        Part(root, "ContainerBlue", PrimitiveType.Cube, new Vector3(-4f, 1.5f, 0), new Vector3(6f, 3f, 2.5f), new Color(0.2f, 0.35f, 0.65f), false);
        Part(root, "ContainerRed", PrimitiveType.Cube, new Vector3(4f, 1.5f, 2f), new Vector3(6f, 3f, 2.5f), new Color(0.65f, 0.2f, 0.18f), false);
        Part(root, "ContainerGold", PrimitiveType.Cube, new Vector3(0, 1.5f, -4f), new Vector3(6f, 3f, 2.5f), new Color(0.75f, 0.6f, 0.2f), false);
        Finish(root, "Containers", new Color(0.3f, 0.45f, 0.7f), 3, 35f);
    }

    private static void BuildGreenhouse()
    {
        GameObject root = BeginEnterable("Greenhouse", new Vector3(12f, 4f, 8f), new Color(0.55f, 0.75f, 0.55f, 0.55f), new Color(0.4f, 0.65f, 0.45f, 0.45f), doorWidth: 2.2f, wallThickness: 0.2f);
        PartUnder(Find(root, "Interior/Props"), "PlanterA", PrimitiveType.Cube, new Vector3(-3f, 0.4f, 0), new Vector3(2f, 0.6f, 4f), new Color(0.35f, 0.25f, 0.15f), false);
        PartUnder(Find(root, "Interior/Props"), "PlanterB", PrimitiveType.Cube, new Vector3(3f, 0.4f, 0), new Vector3(2f, 0.6f, 4f), new Color(0.35f, 0.25f, 0.15f), false);
        FinishEnterable(root, "Greenhouse", new Color(0.45f, 0.85f, 0.45f), 3, 40f);
    }

    private static void BuildHuntingPerch()
    {
        GameObject root = NewRoot("HuntingPerch");
        Part(root, "Post", PrimitiveType.Cube, new Vector3(0, 3f, 0), new Vector3(0.6f, 6f, 0.6f), new Color(0.4f, 0.3f, 0.2f), false);
        Part(root, "Platform", PrimitiveType.Cube, new Vector3(0, 6.2f, 0), new Vector3(4f, 0.3f, 4f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "RailN", PrimitiveType.Cube, new Vector3(0, 6.9f, 1.8f), new Vector3(4f, 0.8f, 0.15f), new Color(0.4f, 0.3f, 0.2f), false);
        Part(root, "Ladder", PrimitiveType.Cube, new Vector3(1.5f, 3f, 0), new Vector3(0.3f, 6f, 0.15f), new Color(0.35f, 0.28f, 0.18f), false);
        Finish(root, "Hunting Perch", new Color(0.5f, 0.4f, 0.25f), 2, 30f);
    }

    private static void BuildPavilion()
    {
        GameObject root = NewRoot("Pavilion");
        Part(root, "Floor", PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(10f, 0.25f, 10f), new Color(0.55f, 0.45f, 0.3f), false);
        Part(root, "PostA", PrimitiveType.Cube, new Vector3(4f, 2.2f, 4f), new Vector3(0.4f, 4.2f, 0.4f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "PostB", PrimitiveType.Cube, new Vector3(-4f, 2.2f, 4f), new Vector3(0.4f, 4.2f, 0.4f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "PostC", PrimitiveType.Cube, new Vector3(4f, 2.2f, -4f), new Vector3(0.4f, 4.2f, 0.4f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "PostD", PrimitiveType.Cube, new Vector3(-4f, 2.2f, -4f), new Vector3(0.4f, 4.2f, 0.4f), new Color(0.45f, 0.35f, 0.22f), false);
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 4.5f, 0), new Vector3(11f, 0.35f, 11f), new Color(0.5f, 0.25f, 0.2f), false);
        Finish(root, "Pavilion", new Color(0.7f, 0.4f, 0.35f), 2, 28f);
    }

    private static void BuildBridgeSpan()
    {
        GameObject root = NewRoot("BridgeSpan");
        Part(root, "Deck", PrimitiveType.Cube, new Vector3(0, 1.5f, 0), new Vector3(8f, 0.35f, 28f), new Color(0.4f, 0.32f, 0.22f), false);
        Part(root, "RailL", PrimitiveType.Cube, new Vector3(-3.8f, 2.2f, 0), new Vector3(0.25f, 1f, 28f), new Color(0.35f, 0.28f, 0.18f), false);
        Part(root, "RailR", PrimitiveType.Cube, new Vector3(3.8f, 2.2f, 0), new Vector3(0.25f, 1f, 28f), new Color(0.35f, 0.28f, 0.18f), false);
        Part(root, "SupportA", PrimitiveType.Cube, new Vector3(0, 0.6f, -8f), new Vector3(1f, 1.2f, 1f), new Color(0.3f, 0.3f, 0.32f), false);
        Part(root, "SupportB", PrimitiveType.Cube, new Vector3(0, 0.6f, 8f), new Vector3(1f, 1.2f, 1f), new Color(0.3f, 0.3f, 0.32f), false);
        Finish(root, "Bridge", new Color(0.45f, 0.4f, 0.35f), 2, 32f);
    }

    // --- Enterable shell builder ------------------------------------------

    private static GameObject BeginEnterable(string name, Vector3 size, Color wallColor, Color roofColor, float doorWidth = 2.5f, float wallThickness = 0.35f)
    {
        GameObject root = NewRoot(name);
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
        Transform doorway = new GameObject("Doorway").transform;
        doorway.SetParent(exterior, false);

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

        // Floor
        PartUnder(floorT, "FloorMesh", PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(w, 0.2f, d), new Color(0.35f, 0.3f, 0.25f), false);

        // Back + sides (fadeable)
        PartUnder(wallsFade, "WallBack", PrimitiveType.Cube, new Vector3(0, wallY, -halfD + wallThickness * 0.5f), new Vector3(w, h, wallThickness), wallColor, true);
        PartUnder(wallsFade, "WallLeft", PrimitiveType.Cube, new Vector3(-halfW + wallThickness * 0.5f, wallY, 0), new Vector3(wallThickness, h, d), wallColor, true);
        PartUnder(wallsFade, "WallRight", PrimitiveType.Cube, new Vector3(halfW - wallThickness * 0.5f, wallY, 0), new Vector3(wallThickness, h, d), wallColor, true);

        // Front wall split around doorway (solid lower frame pieces optional; upper lintel fades)
        float sideWidth = Mathf.Max(0.5f, (w - doorWidth) * 0.5f);
        PartUnder(wallsSolid, "FrontLeft", PrimitiveType.Cube,
            new Vector3(-halfW + sideWidth * 0.5f, wallY, halfD - wallThickness * 0.5f),
            new Vector3(sideWidth, h, wallThickness), wallColor, false);
        PartUnder(wallsSolid, "FrontRight", PrimitiveType.Cube,
            new Vector3(halfW - sideWidth * 0.5f, wallY, halfD - wallThickness * 0.5f),
            new Vector3(sideWidth, h, wallThickness), wallColor, false);
        PartUnder(wallsFade, "FrontLintel", PrimitiveType.Cube,
            new Vector3(0, h - 0.4f, halfD - wallThickness * 0.5f),
            new Vector3(doorWidth, 0.8f, wallThickness), wallColor, true);

        // Roof (fadeable)
        PartUnder(roofFade, "RoofMesh", PrimitiveType.Cube, new Vector3(0, h + 0.25f, 0), new Vector3(w + 1.2f, 0.5f, d + 1.2f), roofColor, true);

        // Interior trigger zone
        GameObject zoneGo = new GameObject("BuildingInteriorZone");
        zoneGo.transform.SetParent(root.transform, false);
        zoneGo.transform.localPosition = new Vector3(0, wallY, 0);
        BoxCollider zoneCol = zoneGo.AddComponent<BoxCollider>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector3(w - wallThickness * 2f, h - 0.2f, d - wallThickness * 2f);
        zoneGo.AddComponent<BuildingInteriorZone>();

        BuildingCutawayController cutaway = root.AddComponent<BuildingCutawayController>();
        cutaway.AutoCollectFadeRenderers();
        zoneGo.GetComponent<BuildingInteriorZone>().cutaway = cutaway;

        return root;
    }

    private static void FinishEnterable(GameObject root, string landmarkName, Color mapColor, int spawnCount, float activationRadius)
    {
        BuildingCutawayController cutaway = root.GetComponent<BuildingCutawayController>();
        if (cutaway != null)
            cutaway.AutoCollectFadeRenderers();
        Finish(root, landmarkName, mapColor, spawnCount, activationRadius);
    }

    // --- Shared helpers ---------------------------------------------------

    private static GameObject NewRoot(string name) => new GameObject(name);

    private static Transform Find(GameObject root, string path)
    {
        Transform t = root.transform.Find(path);
        if (t == null)
            Debug.LogWarning("Missing path " + path + " on " + root.name);
        return t;
    }

    private static void Part(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, bool cutaway)
    {
        PartUnder(root.transform, name, type, localPos, scale, color, cutaway);
    }

    private static void PartUnder(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, bool cutaway)
    {
        if (parent == null)
            return;

        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;

        Material mat = cutaway
            ? GetOrCreateCutawayMat(parent.root.name + "_" + name + "_cutaway", color)
            : GetOrCreateMat(parent.root.name + "_" + name + "_mat", color);

        part.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static Material GetOrCreateMat(string matName, Color color)
    {
        string path = MatFolder + "/" + matName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyColor(existing, color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        ApplyColor(mat, color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material GetOrCreateCutawayMat(string matName, Color color)
    {
        string path = MatFolder + "/" + matName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader cutaway = Shader.Find(CutawayShaderName);
        if (cutaway == null)
            return GetOrCreateMat(matName.Replace("_cutaway", "_mat"), color);

        if (existing != null)
        {
            if (existing.shader != cutaway)
                existing.shader = cutaway;
            ApplyColor(existing, color);
            existing.SetFloat("_Fade", 0f);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material mat = new Material(cutaway);
        ApplyColor(mat, color);
        mat.SetFloat("_Fade", 0f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void ApplyColor(Material mat, Color color)
    {
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
    }

    private static void Finish(GameObject root, string landmarkName, Color mapColor, int spawnCount, float activationRadius)
    {
        SurvivorLandmarkMarker landmark = root.GetComponent<SurvivorLandmarkMarker>();
        if (landmark == null)
            landmark = root.AddComponent<SurvivorLandmarkMarker>();
        landmark.displayName = landmarkName;
        landmark.mapColor = mapColor;

        SurvivorStructureEncounterSpawner spawner = root.GetComponent<SurvivorStructureEncounterSpawner>();
        if (spawner == null)
            spawner = root.AddComponent<SurvivorStructureEncounterSpawner>();
        spawner.spawnCount = spawnCount;
        spawner.activationRadius = activationRadius;
        spawner.oneShot = true;

        // Carve footprint so enemies path around walls when NavMeshAgent is used.
        UnityEngine.AI.NavMeshObstacle obstacle = root.GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle == null)
            obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        Collider[] cols = root.GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
                b.Encapsulate(cols[i].bounds);
            obstacle.center = root.transform.InverseTransformPoint(b.center);
            Vector3 size = root.transform.InverseTransformVector(b.size);
            obstacle.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
        else
        {
            obstacle.center = new Vector3(0f, 2f, 0f);
            obstacle.size = new Vector3(10f, 4f, 10f);
        }

        string path = PrefabFolder + "/" + root.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
#endif
