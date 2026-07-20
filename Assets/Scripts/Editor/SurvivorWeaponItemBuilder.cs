#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Saturn/Build Survivor Weapon Items — creates ItemSO + tinted pickup prefab for every
/// SurvivorWeaponDataSO (Power Core–style: SO + 3D pickup + inventory icon).
/// </summary>
public static class SurvivorWeaponItemBuilder
{
    private const string ItemFolder = "Assets/ScriptableObject/Items/SurvivorWeapons";
    private const string PrefabFolder = "Assets/Prefabs/Items/SurvivorWeapons";
    private const string IconFolder = "Assets/Art/Icons/Survivor";

    [MenuItem("Saturn/Build Survivor Weapon Items")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(ItemFolder);
        Directory.CreateDirectory(PrefabFolder);

        string[] guids = AssetDatabase.FindAssets("t:SurvivorWeaponDataSO", new[] { "Assets/ScriptableObject/Survivor" });
        int built = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SurvivorWeaponDataSO weapon = AssetDatabase.LoadAssetAtPath<SurvivorWeaponDataSO>(path);
            if (weapon == null)
                continue;

            EnsureWeaponIcon(weapon);
            ItemSO item = CreateOrUpdateItemSO(weapon);
            CreateOrUpdatePickupPrefab(weapon, item);
            built++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SurvivorWeaponItemBuilder: built/updated " + built + " weapon items.");
    }

    private static void EnsureWeaponIcon(SurvivorWeaponDataSO weapon)
    {
        if (weapon.icon != null)
            return;

        string typeIcon = "Icon_Projectile";
        switch (weapon.weaponType)
        {
            case SurvivorWeaponType.Aura: typeIcon = "Icon_Aura"; break;
            case SurvivorWeaponType.Orbit: typeIcon = "Icon_Orbit"; break;
            case SurvivorWeaponType.Projectile: typeIcon = "Icon_Projectile"; break;
            case SurvivorWeaponType.Boomerang: typeIcon = "Icon_Boomerang"; break;
            case SurvivorWeaponType.Chain: typeIcon = "Icon_Chain"; break;
            case SurvivorWeaponType.Hitscan: typeIcon = "Icon_Hitscan"; break;
            case SurvivorWeaponType.BouncingBullet: typeIcon = "Icon_BouncingBullet"; break;
            case SurvivorWeaponType.PoisonPool: typeIcon = "Icon_PoisonPool"; break;
            case SurvivorWeaponType.Explosive: typeIcon = "Icon_Explosive"; break;
            case SurvivorWeaponType.Homing: typeIcon = "Icon_Homing"; break;
            case SurvivorWeaponType.Drone: typeIcon = "Icon_Drone"; break;
            case SurvivorWeaponType.Melee: typeIcon = "Icon_Melee"; break;
        }

        string iconPath = IconFolder + "/" + typeIcon + ".png";
        Sprite sprite = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(iconPath);
        for (int a = 0; a < assets.Length; a++)
        {
            Sprite s = assets[a] as Sprite;
            if (s != null)
            {
                sprite = s;
                break;
            }
        }

        if (sprite != null)
        {
            weapon.icon = sprite;
            EditorUtility.SetDirty(weapon);
        }
    }

    private static ItemSO CreateOrUpdateItemSO(SurvivorWeaponDataSO weapon)
    {
        string safeName = SanitizeFileName(weapon.displayName);
        string assetPath = ItemFolder + "/" + safeName + ".asset";
        ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(assetPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, assetPath);
        }

        item.itemName = weapon.displayName;
        item.usable = false;
        item.important = true;
        item.icon = weapon.icon;
        item.description = string.IsNullOrEmpty(weapon.description)
            ? ("Survivor weapon: " + weapon.displayName)
            : weapon.description;
        item.requiredMinigame = ItemSO.MinigameType.Survivor;
        item.survivorWeapon = weapon;
        item.statToChange = ItemSO.StatToChange.none;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void CreateOrUpdatePickupPrefab(SurvivorWeaponDataSO weapon, ItemSO item)
    {
        string safeName = SanitizeFileName(weapon.displayName);
        string prefabPath = PrefabFolder + "/" + safeName + "_Pickup.prefab";

        GameObject root = new GameObject(safeName + "_Pickup");
        root.transform.position = Vector3.zero;

        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.45f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            lit = Shader.Find("Standard");
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null && lit != null)
        {
            Material mat = new Material(lit);
            mat.color = weapon.weaponColor;
            renderer.sharedMaterial = mat;
        }

        root.AddComponent<SurvivorWeaponPickup>();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Weapon";

        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            name = name.Replace(invalid[i], '_');
        return name.Trim();
    }
}
#endif
