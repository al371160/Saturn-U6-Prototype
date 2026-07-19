#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Saturn/Build Survivor Structures — creates flat-color primitive POI prefabs
/// (surviv.io / real-world inspired) matching BunkerZero / HalfwayHitch style.
/// </summary>
public static class SurvivorStructurePrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Structures";
    private const string MatFolder = "Assets/Prefabs/Structures/Materials";

    [MenuItem("Saturn/Build Survivor Structures")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MatFolder);

        BuildCabin();
        BuildHouse();
        BuildWarehouse();
        BuildWaterTower();
        BuildFireStation();
        BuildTentCamp();
        BuildLakeHouse();
        BuildMansion();
        BuildStreetBlock();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SurvivorStructurePrefabBuilder: built 9 structure prefabs with encounter spawners.");
    }

    private static void BuildCabin()
    {
        GameObject root = NewRoot("WoodsCabin");
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0, 2.2f, 0), new Vector3(10f, 4.4f, 8f), new Color(0.45f, 0.28f, 0.16f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 5.1f, 0), new Vector3(11.5f, 1.4f, 9.5f), new Color(0.22f, 0.45f, 0.28f));
        Part(root, "Chimney", PrimitiveType.Cube, new Vector3(3.2f, 6.2f, -2f), new Vector3(1.2f, 2.4f, 1.2f), new Color(0.35f, 0.32f, 0.3f));
        Part(root, "Porch", PrimitiveType.Cube, new Vector3(0, 0.35f, 5.2f), new Vector3(8f, 0.35f, 3f), new Color(0.4f, 0.25f, 0.14f));
        Part(root, "WoodPile", PrimitiveType.Cube, new Vector3(-5.5f, 0.7f, 2f), new Vector3(2f, 1.4f, 1.5f), new Color(0.38f, 0.22f, 0.12f));
        Finish(root, "Cabin", new Color(0.35f, 0.7f, 0.4f), 4, 40f);
    }

    private static void BuildHouse()
    {
        GameObject root = NewRoot("RedHouse");
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0, 2.5f, 0), new Vector3(14f, 5f, 11f), new Color(0.72f, 0.28f, 0.22f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 5.8f, 0), new Vector3(15.5f, 1.6f, 12.5f), new Color(0.35f, 0.12f, 0.1f));
        Part(root, "Garage", PrimitiveType.Cube, new Vector3(9f, 1.8f, 0), new Vector3(6f, 3.6f, 8f), new Color(0.65f, 0.25f, 0.2f));
        Part(root, "DoorStep", PrimitiveType.Cube, new Vector3(0, 0.25f, 6.2f), new Vector3(3f, 0.4f, 1.5f), new Color(0.4f, 0.4f, 0.42f));
        Part(root, "Bush", PrimitiveType.Sphere, new Vector3(-6f, 0.8f, 6f), new Vector3(2.5f, 1.6f, 2.5f), new Color(0.2f, 0.45f, 0.18f));
        Finish(root, "House", new Color(0.9f, 0.35f, 0.3f), 5, 45f);
    }

    private static void BuildWarehouse()
    {
        GameObject root = NewRoot("MetalWarehouse");
        Part(root, "Hall", PrimitiveType.Cube, new Vector3(0, 4f, 0), new Vector3(22f, 8f, 14f), new Color(0.55f, 0.58f, 0.62f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 8.4f, 0), new Vector3(23f, 0.8f, 15f), new Color(0.35f, 0.38f, 0.42f));
        Part(root, "DoorFrame", PrimitiveType.Cube, new Vector3(0, 2.5f, 7.3f), new Vector3(6f, 5f, 0.4f), new Color(0.25f, 0.25f, 0.28f));
        Part(root, "CrateA", PrimitiveType.Cube, new Vector3(-8f, 1f, -4f), new Vector3(3f, 2f, 3f), new Color(0.55f, 0.4f, 0.2f));
        Part(root, "CrateB", PrimitiveType.Cube, new Vector3(7f, 1f, 3f), new Vector3(3f, 2f, 3f), new Color(0.5f, 0.38f, 0.18f));
        Part(root, "Barrel", PrimitiveType.Cylinder, new Vector3(5f, 1f, -5f), new Vector3(1.6f, 1f, 1.6f), new Color(0.7f, 0.25f, 0.15f));
        Finish(root, "Warehouse", new Color(0.6f, 0.65f, 0.7f), 6, 55f);
    }

    private static void BuildWaterTower()
    {
        GameObject root = NewRoot("WaterTower");
        Part(root, "LegN", PrimitiveType.Cube, new Vector3(2.2f, 4f, 2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f));
        Part(root, "LegE", PrimitiveType.Cube, new Vector3(-2.2f, 4f, 2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f));
        Part(root, "LegS", PrimitiveType.Cube, new Vector3(-2.2f, 4f, -2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f));
        Part(root, "LegW", PrimitiveType.Cube, new Vector3(2.2f, 4f, -2.2f), new Vector3(0.6f, 8f, 0.6f), new Color(0.45f, 0.3f, 0.2f));
        Part(root, "Tank", PrimitiveType.Cylinder, new Vector3(0, 10f, 0), new Vector3(7f, 3f, 7f), new Color(0.7f, 0.22f, 0.18f));
        Part(root, "Cap", PrimitiveType.Cylinder, new Vector3(0, 13.2f, 0), new Vector3(7.4f, 0.4f, 7.4f), new Color(0.55f, 0.18f, 0.15f));
        Finish(root, "Water Tower", new Color(0.85f, 0.3f, 0.25f), 3, 42f);
    }

    private static void BuildFireStation()
    {
        GameObject root = NewRoot("FireStation");
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0, 3.5f, 0), new Vector3(20f, 7f, 14f), new Color(0.75f, 0.2f, 0.15f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 7.4f, 0), new Vector3(21f, 0.9f, 15f), new Color(0.25f, 0.25f, 0.28f));
        Part(root, "BayDoor", PrimitiveType.Cube, new Vector3(0, 2.5f, 7.2f), new Vector3(8f, 5f, 0.35f), new Color(0.15f, 0.15f, 0.18f));
        Part(root, "Tower", PrimitiveType.Cube, new Vector3(-8f, 8f, -4f), new Vector3(4f, 9f, 4f), new Color(0.7f, 0.18f, 0.14f));
        Part(root, "Siren", PrimitiveType.Sphere, new Vector3(-8f, 13f, -4f), new Vector3(1.4f, 1.4f, 1.4f), new Color(0.95f, 0.85f, 0.2f));
        Finish(root, "Fire Station", new Color(1f, 0.25f, 0.2f), 5, 50f);
    }

    private static void BuildTentCamp()
    {
        GameObject root = NewRoot("TentCamp");
        Part(root, "TentA", PrimitiveType.Cube, new Vector3(-4f, 1.4f, 0), new Vector3(5f, 2.8f, 5f), new Color(0.55f, 0.5f, 0.35f));
        Part(root, "TentARoof", PrimitiveType.Cube, new Vector3(-4f, 3.2f, 0), new Vector3(5.5f, 0.8f, 5.5f), new Color(0.4f, 0.55f, 0.35f));
        Part(root, "TentB", PrimitiveType.Cube, new Vector3(4f, 1.2f, 3f), new Vector3(4.5f, 2.4f, 4.5f), new Color(0.5f, 0.45f, 0.3f));
        Part(root, "TentBRoof", PrimitiveType.Cube, new Vector3(4f, 2.8f, 3f), new Vector3(5f, 0.7f, 5f), new Color(0.35f, 0.5f, 0.32f));
        Part(root, "Campfire", PrimitiveType.Cylinder, new Vector3(0, 0.4f, -3f), new Vector3(2f, 0.4f, 2f), new Color(0.35f, 0.2f, 0.1f));
        Part(root, "Crate", PrimitiveType.Cube, new Vector3(2f, 0.6f, -2f), new Vector3(1.5f, 1.2f, 1.5f), new Color(0.45f, 0.32f, 0.18f));
        Finish(root, "Tent Camp", new Color(0.55f, 0.7f, 0.4f), 3, 35f);
    }

    private static void BuildLakeHouse()
    {
        GameObject root = NewRoot("LakeHouse");
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0, 2.4f, 0), new Vector3(12f, 4.8f, 9f), new Color(0.85f, 0.82f, 0.75f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 5.5f, 0), new Vector3(13.5f, 1.3f, 10.5f), new Color(0.2f, 0.35f, 0.5f));
        Part(root, "Deck", PrimitiveType.Cube, new Vector3(0, 0.3f, 7f), new Vector3(14f, 0.35f, 6f), new Color(0.55f, 0.4f, 0.25f));
        Part(root, "Dock", PrimitiveType.Cube, new Vector3(0, 0.25f, 13f), new Vector3(4f, 0.3f, 8f), new Color(0.5f, 0.38f, 0.22f));
        Part(root, "PilingL", PrimitiveType.Cylinder, new Vector3(-1.5f, -0.5f, 16f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.35f, 0.28f, 0.18f));
        Part(root, "PilingR", PrimitiveType.Cylinder, new Vector3(1.5f, -0.5f, 16f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.35f, 0.28f, 0.18f));
        Finish(root, "Lake House", new Color(0.35f, 0.55f, 0.85f), 4, 48f);
    }

    private static void BuildMansion()
    {
        GameObject root = NewRoot("EstateMansion");
        Part(root, "Main", PrimitiveType.Cube, new Vector3(0, 4f, 0), new Vector3(22f, 8f, 16f), new Color(0.82f, 0.78f, 0.7f));
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0, 8.6f, 0), new Vector3(23.5f, 1.5f, 17.5f), new Color(0.28f, 0.22f, 0.2f));
        Part(root, "WingL", PrimitiveType.Cube, new Vector3(-14f, 3f, 2f), new Vector3(8f, 6f, 12f), new Color(0.78f, 0.74f, 0.66f));
        Part(root, "WingR", PrimitiveType.Cube, new Vector3(14f, 3f, 2f), new Vector3(8f, 6f, 12f), new Color(0.78f, 0.74f, 0.66f));
        Part(root, "Columns", PrimitiveType.Cylinder, new Vector3(-3f, 2.5f, 9f), new Vector3(1f, 2.5f, 1f), new Color(0.9f, 0.88f, 0.82f));
        Part(root, "Columns2", PrimitiveType.Cylinder, new Vector3(3f, 2.5f, 9f), new Vector3(1f, 2.5f, 1f), new Color(0.9f, 0.88f, 0.82f));
        Part(root, "CourtyardTree", PrimitiveType.Capsule, new Vector3(0, 2.5f, -2f), new Vector3(2.5f, 3f, 2.5f), new Color(0.2f, 0.45f, 0.2f));
        Finish(root, "Mansion", new Color(0.9f, 0.75f, 0.55f), 7, 60f);
    }

    private static void BuildStreetBlock()
    {
        GameObject root = NewRoot("StreetBlock");
        Part(root, "Road", PrimitiveType.Cube, new Vector3(0, 0.08f, 0), new Vector3(40f, 0.15f, 10f), new Color(0.22f, 0.22f, 0.24f));
        Part(root, "CurbN", PrimitiveType.Cube, new Vector3(0, 0.2f, 5.3f), new Vector3(40f, 0.25f, 0.6f), new Color(0.45f, 0.45f, 0.48f));
        Part(root, "CurbS", PrimitiveType.Cube, new Vector3(0, 0.2f, -5.3f), new Vector3(40f, 0.25f, 0.6f), new Color(0.45f, 0.45f, 0.48f));
        Part(root, "Stripe", PrimitiveType.Cube, new Vector3(0, 0.16f, 0), new Vector3(38f, 0.02f, 0.35f), new Color(0.85f, 0.8f, 0.35f));
        Part(root, "LampN", PrimitiveType.Cylinder, new Vector3(-12f, 3f, 6f), new Vector3(0.35f, 3f, 0.35f), new Color(0.3f, 0.3f, 0.32f));
        Part(root, "LampS", PrimitiveType.Cylinder, new Vector3(12f, 3f, -6f), new Vector3(0.35f, 3f, 0.35f), new Color(0.3f, 0.3f, 0.32f));
        Finish(root, "Street", new Color(0.4f, 0.4f, 0.45f), 2, 30f);
    }

    private static GameObject NewRoot(string name)
    {
        GameObject root = new GameObject(name);
        return root;
    }

    private static void Part(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;

        Material mat = GetOrCreateMat(root.name + "_" + name + "_mat", color);
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
    }

    private static Material GetOrCreateMat(string matName, Color color)
    {
        string path = MatFolder + "/" + matName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            if (existing.HasProperty("_BaseColor"))
                existing.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void Finish(GameObject root, string landmarkName, Color mapColor, int spawnCount, float activationRadius)
    {
        SurvivorLandmarkMarker landmark = root.AddComponent<SurvivorLandmarkMarker>();
        landmark.displayName = landmarkName;
        landmark.mapColor = mapColor;

        SurvivorStructureEncounterSpawner spawner = root.AddComponent<SurvivorStructureEncounterSpawner>();
        spawner.spawnCount = spawnCount;
        spawner.activationRadius = activationRadius;
        spawner.oneShot = true;

        string path = PrefabFolder + "/" + root.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
#endif
