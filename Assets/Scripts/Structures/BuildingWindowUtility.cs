using UnityEngine;

/// <summary>
/// Ensures enterable buildings have shatterable glass windows. Safe to call repeatedly —
/// skips if a Windows root already exists (e.g. prefab rebuilt with openings).
/// </summary>
public static class BuildingWindowUtility
{
    private static readonly Color DefaultGlass = new Color(0.55f, 0.78f, 0.95f, 0.32f);

    public static void EnsureWindows(BuildingCutawayController building)
    {
        if (building == null)
            return;

        Transform root = building.transform;
        if (FindDeepChild(root, "Windows") != null)
            return;

        BuildingInteriorZone zone = root.GetComponentInChildren<BuildingInteriorZone>(true);
        BoxCollider zoneCol = zone != null ? zone.GetComponent<BoxCollider>() : null;
        if (zoneCol == null)
            return;

        Vector3 interiorSize = zoneCol.size;
        float wallThickness = 0.35f;
        float w = interiorSize.x + wallThickness * 2f;
        float h = interiorSize.y + 0.2f;
        float d = interiorSize.z + wallThickness * 2f;

        Transform exterior = FindDeepChild(root, "Exterior");
        if (exterior == null)
            exterior = root;

        Transform windowsRoot = new GameObject("Windows").transform;
        windowsRoot.SetParent(exterior, false);

        Transform wallsFade = FindDeepChild(root, "Walls_Fadeable");
        Transform wallsSolid = FindDeepChild(root, "Walls_Solid");

        // Replace solid side/back walls with windowed segments when present.
        if (wallsFade != null)
        {
            ReplaceWallWithWindows(wallsFade, "WallBack", windowsRoot, isBackWall: true, w, h, d, wallThickness);
            ReplaceWallWithWindows(wallsFade, "WallLeft", windowsRoot, isBackWall: false, w, h, d, wallThickness);
            ReplaceWallWithWindows(wallsFade, "WallRight", windowsRoot, isBackWall: false, w, h, d, wallThickness);
        }

        // Front flank panels get a single window each when wide enough.
        if (wallsSolid != null)
        {
            AddFrontPanelWindow(wallsSolid, "FrontLeft", windowsRoot);
            AddFrontPanelWindow(wallsSolid, "FrontRight", windowsRoot);
        }
    }

    private static void ReplaceWallWithWindows(
        Transform wallsParent,
        string wallName,
        Transform windowsRoot,
        bool isBackWall,
        float buildingW,
        float buildingH,
        float buildingD,
        float wallThickness)
    {
        Transform wall = wallsParent.Find(wallName);
        if (wall == null)
            return;

        Renderer wallRenderer = wall.GetComponent<Renderer>();
        Material wallMat = wallRenderer != null ? wallRenderer.sharedMaterial : null;
        Vector3 pos = wall.localPosition;
        Vector3 scale = wall.localScale;
        Object.Destroy(wall.gameObject);

        float windowW = Mathf.Clamp(isBackWall ? buildingW * 0.22f : buildingD * 0.22f, 1.4f, 2.4f);
        float windowH = Mathf.Clamp(buildingH * 0.38f, 1.3f, 2.0f);
        float sill = Mathf.Clamp(buildingH * 0.28f, 1.0f, 1.6f);
        float halfW = buildingW * 0.5f;
        float halfD = buildingD * 0.5f;

        if (isBackWall)
        {
            // Back wall along X — two windows.
            float z = -halfD + wallThickness * 0.5f;
            BuildHorizontalWindowedWall(wallsParent, windowsRoot, wallName, wallMat, z, buildingW, buildingH, wallThickness, windowW, windowH, sill, alongX: true);
        }
        else if (wallName == "WallLeft")
        {
            float x = -halfW + wallThickness * 0.5f;
            BuildHorizontalWindowedWall(wallsParent, windowsRoot, wallName, wallMat, x, buildingD, buildingH, wallThickness, windowW, windowH, sill, alongX: false, negateAxis: true);
        }
        else
        {
            float x = halfW - wallThickness * 0.5f;
            BuildHorizontalWindowedWall(wallsParent, windowsRoot, wallName, wallMat, x, buildingD, buildingH, wallThickness, windowW, windowH, sill, alongX: false, negateAxis: false);
        }

        // Silence unused locals from earlier capture.
        _ = pos;
        _ = scale;
    }

    private static void BuildHorizontalWindowedWall(
        Transform wallsParent,
        Transform windowsRoot,
        string wallName,
        Material wallMat,
        float axisPos,
        float wallLength,
        float wallHeight,
        float wallThickness,
        float windowW,
        float windowH,
        float sill,
        bool alongX,
        bool negateAxis = false)
    {
        float lintelH = Mathf.Max(0.35f, wallHeight - (sill + windowH));
        float pier = Mathf.Max(0.4f, (wallLength - windowW * 2f) / 3f);
        float yCenter = wallHeight * 0.5f;

        // Lower band + upper band
        CreateWallSegment(wallsParent, wallName + "_Lower", wallMat, alongX, axisPos, sill * 0.5f, wallLength, sill, wallThickness, negateAxis);
        CreateWallSegment(wallsParent, wallName + "_Upper", wallMat, alongX, axisPos, sill + windowH + lintelH * 0.5f, wallLength, lintelH, wallThickness, negateAxis);

        // Three piers separating two windows
        float[] pierCenters =
        {
            -wallLength * 0.5f + pier * 0.5f,
            0f,
            wallLength * 0.5f - pier * 0.5f
        };
        for (int i = 0; i < pierCenters.Length; i++)
        {
            CreateWallSegment(
                wallsParent,
                wallName + "_Pier" + i,
                wallMat,
                alongX,
                axisPos,
                sill + windowH * 0.5f,
                pier,
                windowH,
                wallThickness,
                negateAxis,
                pierCenters[i]);
        }

        // Glass in the two openings
        float leftWindowCenter = -wallLength * 0.5f + pier + windowW * 0.5f;
        float rightWindowCenter = wallLength * 0.5f - pier - windowW * 0.5f;
        CreateGlass(windowsRoot, wallName + "_GlassL", alongX, axisPos, sill + windowH * 0.5f, windowW, windowH, wallThickness, negateAxis, leftWindowCenter);
        CreateGlass(windowsRoot, wallName + "_GlassR", alongX, axisPos, sill + windowH * 0.5f, windowW, windowH, wallThickness, negateAxis, rightWindowCenter);
    }

    private static void CreateWallSegment(
        Transform parent,
        string name,
        Material wallMat,
        bool alongX,
        float axisPos,
        float y,
        float length,
        float height,
        float thickness,
        bool negateAxis,
        float centerAlong = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);

        if (alongX)
        {
            go.transform.localPosition = new Vector3(centerAlong, y, axisPos);
            go.transform.localScale = new Vector3(length, height, thickness);
        }
        else
        {
            go.transform.localPosition = new Vector3(axisPos, y, centerAlong);
            go.transform.localScale = new Vector3(thickness, height, length);
        }

        if (wallMat != null)
            go.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    private static void CreateGlass(
        Transform windowsRoot,
        string name,
        bool alongX,
        float axisPos,
        float y,
        float windowW,
        float windowH,
        float wallThickness,
        bool negateAxis,
        float centerAlong)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(windowsRoot, false);

        float glassThickness = Mathf.Max(0.08f, wallThickness * 0.45f);
        if (alongX)
        {
            go.transform.localPosition = new Vector3(centerAlong, y, axisPos);
            go.transform.localScale = new Vector3(windowW, windowH, glassThickness);
        }
        else
        {
            go.transform.localPosition = new Vector3(axisPos, y, centerAlong);
            go.transform.localScale = new Vector3(glassThickness, windowH, windowW);
        }

        Object.Destroy(go.GetComponent<Collider>());
        BoxCollider box = go.AddComponent<BoxCollider>();
        ShatterableGlassPane pane = go.AddComponent<ShatterableGlassPane>();
        pane.Configure(18f, DefaultGlass);
        _ = box;
        _ = negateAxis;
    }

    private static void AddFrontPanelWindow(Transform wallsSolid, string panelName, Transform windowsRoot)
    {
        Transform panel = wallsSolid.Find(panelName);
        if (panel == null)
            return;

        Vector3 scale = panel.localScale;
        if (scale.x < 2.2f)
            return;

        float windowW = Mathf.Min(1.8f, scale.x * 0.45f);
        float windowH = Mathf.Min(1.7f, scale.y * 0.4f);
        float sill = Mathf.Clamp(scale.y * 0.28f, 1.0f, 1.5f);

        // Carve a hole: shrink panel into frame pieces around a glass insert.
        Renderer panelRenderer = panel.GetComponent<Renderer>();
        Material wallMat = panelRenderer != null ? panelRenderer.sharedMaterial : null;
        Vector3 pos = panel.localPosition;
        Object.Destroy(panel.gameObject);

        float thickness = scale.z;
        float wallH = scale.y;
        float wallW = scale.x;
        float lintelH = Mathf.Max(0.35f, wallH - (sill + windowH));
        float side = Mathf.Max(0.35f, (wallW - windowW) * 0.5f);

        CreateFrontSegment(wallsSolid, panelName + "_Lower", wallMat, pos + new Vector3(0f, sill * 0.5f - wallH * 0.5f, 0f), new Vector3(wallW, sill, thickness));
        CreateFrontSegment(wallsSolid, panelName + "_Upper", wallMat, pos + new Vector3(0f, sill + windowH + lintelH * 0.5f - wallH * 0.5f, 0f), new Vector3(wallW, lintelH, thickness));
        CreateFrontSegment(wallsSolid, panelName + "_L", wallMat, pos + new Vector3(-windowW * 0.5f - side * 0.5f, sill + windowH * 0.5f - wallH * 0.5f, 0f), new Vector3(side, windowH, thickness));
        CreateFrontSegment(wallsSolid, panelName + "_R", wallMat, pos + new Vector3(windowW * 0.5f + side * 0.5f, sill + windowH * 0.5f - wallH * 0.5f, 0f), new Vector3(side, windowH, thickness));

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = panelName + "_Glass";
        glass.transform.SetParent(windowsRoot, false);
        glass.transform.localPosition = pos + new Vector3(0f, sill + windowH * 0.5f - wallH * 0.5f, 0f);
        glass.transform.localScale = new Vector3(windowW, windowH, Mathf.Max(0.08f, thickness * 0.45f));
        Object.Destroy(glass.GetComponent<Collider>());
        glass.AddComponent<BoxCollider>();
        glass.AddComponent<ShatterableGlassPane>().Configure(18f, DefaultGlass);
    }

    private static void CreateFrontSegment(Transform parent, string name, Material wallMat, Vector3 localPos, Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (wallMat != null)
            go.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
            Transform nested = FindDeepChild(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
