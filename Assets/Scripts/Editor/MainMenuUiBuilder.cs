#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot builder for Main Menu multiplayer panel stack.
/// Menu: Tools / Saturn / Build Main Menu Multiplayer UI
/// </summary>
public static class MainMenuUiBuilder
{
    private const string ScenePath = "Assets/Scenes/Main Menu.unity";
    private const string CatalogPath = "Assets/Resources/MatchPrepCatalog.asset";
    private const string ConfigPath = "Assets/ScriptableObject/Survivor/Level 1 Survivor Config.asset";
    private const string FontPath = "Assets/Fonts/bedstead/bedstead-bold SDF.asset";

    [MenuItem("Tools/Saturn/Build Main Menu Multiplayer UI")]
    public static void Build()
    {
        string result = BuildInternal();
        Debug.Log("[MainMenuUiBuilder] " + result);
    }

    public static string BuildInternal()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        MatchPrepCatalog catalog = AssetDatabase.LoadAssetAtPath<MatchPrepCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MatchPrepCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        PopulateCatalog(catalog);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot == null)
            return "ERROR: UI canvas missing";

        DestroyChild(uiRoot.transform, "MultiplayerPanel");
        DestroyChild(uiRoot.transform, "MatchPrepPanel");
        DestroyChild(uiRoot.transform, "DeployPanel");
        DestroyChild(uiRoot.transform, "MenuControllers");

        GameObject mainPanel = uiRoot.transform.Find("Main Menu")?.gameObject;
        if (mainPanel == null)
            return "ERROR: Main Menu panel missing";

        GameObject fadeOut = uiRoot.transform.Find("FadeOut")?.gameObject;
        CanvasGroup fadeGroup = fadeOut != null ? fadeOut.GetComponent<CanvasGroup>() : null;

        Transform existingStart = mainPanel.transform.Find("Button");
        if (existingStart != null)
        {
            RectTransform startRt = existingStart.GetComponent<RectTransform>();
            startRt.anchoredPosition = new Vector2(0f, -40f);
            startRt.sizeDelta = new Vector2(320f, 64f);
            TMP_Text startLabel = existingStart.GetComponentInChildren<TMP_Text>();
            if (startLabel != null)
            {
                startLabel.text = "Start Demo";
                if (font != null)
                    startLabel.font = font;
            }
        }

        DestroyChild(mainPanel.transform, "MultiplayerButton");
        DestroyChild(mainPanel.transform, "QuitButton");

        Button multiplayerBtn = CreateButton(mainPanel.transform, "MultiplayerButton", "Multiplayer",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(320f, 64f), font);
        Button quitBtn = CreateButton(mainPanel.transform, "QuitButton", "Quit",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(320f, 64f), font);

        TMP_Text title = mainPanel.transform.Find("Saturn text")?.GetComponent<TMP_Text>();
        if (title != null && font != null)
            title.font = font;

        GameObject mpPanel = CreateUIObject("MultiplayerPanel", uiRoot.transform);
        Stretch(mpPanel);
        AddPanelImage(mpPanel, SurvivorUiStyle.DarkPanel);
        mpPanel.SetActive(false);

        AddLabel(mpPanel, "Title", "MULTIPLAYER", 54f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300, -120), new Vector2(300, -40), font);
        TMP_Text mpStatus = AddLabel(mpPanel, "Status", "Choose a mode", 22f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400, -170), new Vector2(400, -130), font, muted: true);

        Button playSolo = CreateButton(mpPanel.transform, "PlaySoloButton", "Play Solo",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, 80f), new Vector2(260f, 56f), font);
        Button playDuo = CreateButton(mpPanel.transform, "PlayDuoButton", "Play Duo",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, 80f), new Vector2(260f, 56f), font);
        Button playSquad = CreateButton(mpPanel.transform, "PlaySquadButton", "Play Squad",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(260f, 56f), font);
        Button joinTeam = CreateButton(mpPanel.transform, "JoinTeamButton", "Join Team",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -70f), new Vector2(260f, 48f), font);
        Button createTeam = CreateButton(mpPanel.transform, "CreateTeamButton", "Create Team",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, -70f), new Vector2(260f, 48f), font);
        Button howToPlay = CreateButton(mpPanel.transform, "HowToPlayButton", "How to Play",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(260f, 48f), font);
        Button localHost = CreateButton(mpPanel.transform, "LocalHostButton", "Local Host",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -200f), new Vector2(260f, 48f), font);
        Button localJoin = CreateButton(mpPanel.transform, "LocalJoinButton", "Local Join",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, -200f), new Vector2(260f, 48f), font);
        Button backMp = CreateButton(mpPanel.transform, "BackButton", "Back",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(220f, 52f), font);

        GameObject prepPanel = CreateUIObject("MatchPrepPanel", uiRoot.transform);
        Stretch(prepPanel);
        AddPanelImage(prepPanel, SurvivorUiStyle.DarkPanel);
        prepPanel.SetActive(false);

        AddLabel(prepPanel, "Title", "MATCH PREP", 48f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300, -90), new Vector2(300, -30), font);

        TMP_Text charLabel = AddLabel(prepPanel, "CharacterLabel", "Character: Operative", 24f, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -160), new Vector2(420, -120), font);
        TMP_Text skinLabel = AddLabel(prepPanel, "SkinLabel", "Skin: Default", 24f, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -210), new Vector2(420, -170), font);

        Button prevChar = CreateButton(prepPanel.transform, "PrevCharacterButton", "<",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -140f), new Vector2(48f, 40f), font);
        Button nextChar = CreateButton(prepPanel.transform, "NextCharacterButton", ">",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, -140f), new Vector2(48f, 40f), font);
        Button prevSkin = CreateButton(prepPanel.transform, "PrevSkinButton", "<",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -190f), new Vector2(48f, 40f), font);
        Button nextSkin = CreateButton(prepPanel.transform, "NextSkinButton", ">",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, -190f), new Vector2(48f, 40f), font);

        TMP_Text weaponLabel = AddLabel(prepPanel, "SelectedWeaponLabel", "Weapon: (none)", 22f, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(40, -260), new Vector2(-20, -220), font);
        TMP_Text buffLabel = AddLabel(prepPanel, "SelectedBuffsLabel", "Buffs: (none)", 22f, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(20, -260), new Vector2(-40, -220), font);

        CreateScrollList(prepPanel.transform, "WeaponScroll",
            new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(40, 120), new Vector2(-20, -280), out Transform weaponRoot);
        CreateScrollList(prepPanel.transform, "BuffScroll",
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(20, 120), new Vector2(-40, -280), out Transform buffRoot);

        Button readyBtn = CreateButton(prepPanel.transform, "ReadyButton", "Ready",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 60f), new Vector2(220f, 56f), font);
        Button cancelBtn = CreateButton(prepPanel.transform, "CancelButton", "Cancel",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(160f, 60f), new Vector2(220f, 56f), font);

        GameObject deployPanel = CreateUIObject("DeployPanel", uiRoot.transform);
        Stretch(deployPanel);
        AddPanelImage(deployPanel, new Color(0.02f, 0.03f, 0.05f, 0.98f));
        deployPanel.SetActive(false);
        TMP_Text deployLabel = AddLabel(deployPanel, "Status", "Deploying...", 64f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400, -40), new Vector2(400, 40), font);

        if (fadeOut != null)
            fadeOut.transform.SetAsLastSibling();

        GameObject controllersGo = CreateUIObject("MenuControllers", uiRoot.transform);
        MainMenuController mainMenu = controllersGo.AddComponent<MainMenuController>();
        MultiplayerMenuController mpMenu = controllersGo.AddComponent<MultiplayerMenuController>();
        MatchPrepController matchPrep = controllersGo.AddComponent<MatchPrepController>();
        DeployScreenController deploy = controllersGo.AddComponent<DeployScreenController>();

        SetRef(mainMenu, "mainPanel", mainPanel);
        SetRef(mainMenu, "multiplayerPanel", mpPanel);
        SetRef(mainMenu, "matchPrepPanel", prepPanel);
        SetRef(mainMenu, "deployPanel", deployPanel);
        SetRef(mainMenu, "multiplayerButton", multiplayerBtn);
        SetRef(mainMenu, "quitButton", quitBtn);

        mpMenu.mainMenu = mainMenu;
        mpMenu.statusText = mpStatus;
        SetRef(mpMenu, "playSoloButton", playSolo);
        SetRef(mpMenu, "playDuoButton", playDuo);
        SetRef(mpMenu, "playSquadButton", playSquad);
        SetRef(mpMenu, "joinTeamButton", joinTeam);
        SetRef(mpMenu, "createTeamButton", createTeam);
        SetRef(mpMenu, "howToPlayButton", howToPlay);
        SetRef(mpMenu, "localHostButton", localHost);
        SetRef(mpMenu, "localJoinButton", localJoin);
        SetRef(mpMenu, "backButton", backMp);

        SetRef(matchPrep, "mainMenu", mainMenu);
        SetRef(matchPrep, "readyButton", readyBtn);
        SetRef(matchPrep, "cancelButton", cancelBtn);
        SetRef(matchPrep, "prevCharacterButton", prevChar);
        SetRef(matchPrep, "nextCharacterButton", nextChar);
        SetRef(matchPrep, "prevSkinButton", prevSkin);
        SetRef(matchPrep, "nextSkinButton", nextSkin);
        SetRef(matchPrep, "characterLabel", charLabel);
        SetRef(matchPrep, "skinLabel", skinLabel);
        SetRef(matchPrep, "catalog", catalog);
        SetRef(matchPrep, "weaponListRoot", weaponRoot);
        SetRef(matchPrep, "buffListRoot", buffRoot);
        SetRef(matchPrep, "selectedWeaponLabel", weaponLabel);
        SetRef(matchPrep, "selectedBuffsLabel", buffLabel);
        SetRef(matchPrep, "uiFont", font);

        SerializedObject prepSo = new SerializedObject(matchPrep);
        SerializedProperty wProp = prepSo.FindProperty("weaponOptions");
        wProp.arraySize = catalog.weaponOptions.Length;
        for (int i = 0; i < catalog.weaponOptions.Length; i++)
            wProp.GetArrayElementAtIndex(i).objectReferenceValue = catalog.weaponOptions[i];
        SerializedProperty bProp = prepSo.FindProperty("buffOptions");
        bProp.arraySize = catalog.buffOptions.Length;
        for (int i = 0; i < catalog.buffOptions.Length; i++)
            bProp.GetArrayElementAtIndex(i).objectReferenceValue = catalog.buffOptions[i];
        prepSo.ApplyModifiedPropertiesWithoutUndo();

        SetRef(deploy, "statusLabel", deployLabel);
        SetRef(deploy, "fadeCanvasGroup", fadeGroup);

        if (existingStart != null)
        {
            SceneLoader sl = existingStart.GetComponent<SceneLoader>();
            if (sl != null)
            {
                if (string.IsNullOrEmpty(sl.sceneToLoad))
                    sl.sceneToLoad = "Prologue";
                if (sl.fadeCanvasGroup == null && fadeGroup != null)
                    sl.fadeCanvasGroup = fadeGroup;
                EditorUtility.SetDirty(sl);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        return $"OK weapons={catalog.weaponOptions.Length} buffs={catalog.buffOptions.Length} startDemo={(existingStart != null)} fade={(fadeGroup != null)}";
    }

    private static void PopulateCatalog(MatchPrepCatalog catalog)
    {
        var weapons = new List<SurvivorWeaponDataSO>();
        var buffs = new List<SurvivorBuffDataSO>();

        SurvivorMinigameConfig config = AssetDatabase.LoadAssetAtPath<SurvivorMinigameConfig>(ConfigPath);
        if (config != null)
        {
            if (config.availableWeapons != null)
                weapons.AddRange(config.availableWeapons.Where(w => w != null));
            if (config.availableBuffs != null)
                buffs.AddRange(config.availableBuffs.Where(b => b != null));
        }

        if (weapons.Count == 0)
        {
            foreach (string g in AssetDatabase.FindAssets("t:SurvivorWeaponDataSO"))
            {
                SurvivorWeaponDataSO w = AssetDatabase.LoadAssetAtPath<SurvivorWeaponDataSO>(AssetDatabase.GUIDToAssetPath(g));
                if (w != null)
                    weapons.Add(w);
            }
        }

        if (buffs.Count == 0)
        {
            foreach (string g in AssetDatabase.FindAssets("t:SurvivorBuffDataSO"))
            {
                SurvivorBuffDataSO b = AssetDatabase.LoadAssetAtPath<SurvivorBuffDataSO>(AssetDatabase.GUIDToAssetPath(g));
                if (b != null)
                    buffs.Add(b);
            }
        }

        if (weapons.Count > 24)
            weapons = weapons.Take(24).ToList();
        if (buffs.Count > 24)
            buffs = buffs.Take(24).ToList();

        catalog.weaponOptions = weapons.ToArray();
        catalog.buffOptions = buffs.ToArray();
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null)
            Object.DestroyImmediate(t.gameObject);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image AddPanelImage(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    private static TMP_Text AddLabel(
        GameObject parent,
        string name,
        string text,
        float fontSize,
        TextAlignmentOptions align,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        TMP_FontAsset font,
        bool muted = false)
    {
        GameObject go = CreateUIObject(name, parent.transform);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        SurvivorUiStyle.ApplyTextOnDark(tmp, muted);
        return tmp;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        TMP_FontAsset font)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = SurvivorUiStyle.LightPanel;

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = SurvivorUiStyle.LightPanel;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.87f, 0.9f, 1f);
        btn.colors = colors;

        GameObject textGo = CreateUIObject("Text", go.transform);
        Stretch(textGo);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        SurvivorUiStyle.ApplyTextOnLight(tmp);
        return btn;
    }

    private static ScrollRect CreateScrollList(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        out Transform contentRoot)
    {
        GameObject scrollGo = CreateUIObject(name, parent);
        RectTransform srt = scrollGo.GetComponent<RectTransform>();
        srt.anchorMin = anchorMin;
        srt.anchorMax = anchorMax;
        srt.offsetMin = offsetMin;
        srt.offsetMax = offsetMax;
        scrollGo.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.55f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIObject("Viewport", scrollGo.transform);
        Stretch(viewport);
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = crt;
        contentRoot = content.transform;
        return scroll;
    }

    private static void SetRef(Object target, string prop, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogWarning($"[MainMenuUiBuilder] Missing prop {prop} on {target.GetType().Name}");
            return;
        }

        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
