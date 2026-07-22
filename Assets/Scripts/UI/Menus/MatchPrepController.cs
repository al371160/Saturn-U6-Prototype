using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fortnite-like match prep placeholders: character/skin, weapon pick, buff multi-select.
/// </summary>
public class MatchPrepController : MonoBehaviour
{
    private const int MaxBuffSelections = 3;
    private const string CatalogResourcePath = "MatchPrepCatalog";

    [Header("Navigation")]
    [SerializeField] private MainMenuController mainMenu;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button cancelButton;

    [Header("Character / Skin")]
    [SerializeField] private string[] characterIds = { "Operative", "Scout", "Heavy" };
    [SerializeField] private string[] skinIds = { "Default", "Ash", "Verdant", "Night" };
    [SerializeField] private Button prevCharacterButton;
    [SerializeField] private Button nextCharacterButton;
    [SerializeField] private Button prevSkinButton;
    [SerializeField] private Button nextSkinButton;
    [SerializeField] private TMP_Text characterLabel;
    [SerializeField] private TMP_Text skinLabel;

    [Header("Loadout Pools")]
    [SerializeField] private MatchPrepCatalog catalog;
    [SerializeField] private SurvivorWeaponDataSO[] weaponOptions;
    [SerializeField] private SurvivorBuffDataSO[] buffOptions;
    [SerializeField] private Transform weaponListRoot;
    [SerializeField] private Transform buffListRoot;
    [SerializeField] private TMP_Text selectedWeaponLabel;
    [SerializeField] private TMP_Text selectedBuffsLabel;
    [SerializeField] private TMP_FontAsset uiFont;

    private int characterIndex;
    private int skinIndex;
    private SurvivorWeaponDataSO selectedWeapon;
    private readonly List<SurvivorBuffDataSO> selectedBuffs = new List<SurvivorBuffDataSO>();
    private readonly List<Button> weaponButtons = new List<Button>();
    private readonly List<Button> buffButtons = new List<Button>();
    private readonly List<SurvivorWeaponDataSO> weaponButtonSources = new List<SurvivorWeaponDataSO>();
    private readonly List<SurvivorBuffDataSO> buffButtonSources = new List<SurvivorBuffDataSO>();

    private void Awake()
    {
        Wire(prevCharacterButton, () => CycleCharacter(-1));
        Wire(nextCharacterButton, () => CycleCharacter(1));
        Wire(prevSkinButton, () => CycleSkin(-1));
        Wire(nextSkinButton, () => CycleSkin(1));
        Wire(readyButton, OnReady);
        Wire(cancelButton, OnCancel);
    }

    private void OnEnable()
    {
        ResolvePools();
        EnsureDefaults();
        RebuildWeaponList();
        RebuildBuffList();
        RefreshLabels();
    }

    private void ResolvePools()
    {
        if (catalog == null)
            catalog = Resources.Load<MatchPrepCatalog>(CatalogResourcePath);

        if ((weaponOptions == null || weaponOptions.Length == 0) && catalog != null)
            weaponOptions = catalog.weaponOptions;

        if ((buffOptions == null || buffOptions.Length == 0) && catalog != null)
            buffOptions = catalog.buffOptions;

#if UNITY_EDITOR
        if (weaponOptions == null || weaponOptions.Length == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SurvivorWeaponDataSO");
            var list = new List<SurvivorWeaponDataSO>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                SurvivorWeaponDataSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivorWeaponDataSO>(path);
                if (asset != null)
                    list.Add(asset);
            }
            weaponOptions = list.ToArray();
        }

        if (buffOptions == null || buffOptions.Length == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SurvivorBuffDataSO");
            var list = new List<SurvivorBuffDataSO>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                SurvivorBuffDataSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SurvivorBuffDataSO>(path);
                if (asset != null)
                    list.Add(asset);
            }
            buffOptions = list.ToArray();
        }
#endif

        if ((weaponOptions == null || weaponOptions.Length == 0) ||
            (buffOptions == null || buffOptions.Length == 0))
        {
            SurvivorMinigameConfig config = Resources.Load<SurvivorMinigameConfig>("Level 1 Survivor Config");
            if (config == null)
            {
                SurvivorMinigameConfig[] all = Resources.LoadAll<SurvivorMinigameConfig>(string.Empty);
                if (all != null && all.Length > 0)
                    config = all[0];
            }

            if (config != null)
            {
                if (weaponOptions == null || weaponOptions.Length == 0)
                    weaponOptions = config.availableWeapons;
                if (buffOptions == null || buffOptions.Length == 0)
                    buffOptions = config.availableBuffs;
            }
        }
    }

    private void EnsureDefaults()
    {
        if (characterIds == null || characterIds.Length == 0)
            characterIds = new[] { "Operative" };
        if (skinIds == null || skinIds.Length == 0)
            skinIds = new[] { "Default" };

        characterIndex = Mathf.Clamp(characterIndex, 0, characterIds.Length - 1);
        skinIndex = Mathf.Clamp(skinIndex, 0, skinIds.Length - 1);

        if (selectedWeapon == null && weaponOptions != null && weaponOptions.Length > 0)
            selectedWeapon = weaponOptions[0];
    }

    private void RebuildWeaponList()
    {
        ClearChildren(weaponListRoot, weaponButtons);
        weaponButtonSources.Clear();
        if (weaponListRoot == null || weaponOptions == null)
            return;

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            SurvivorWeaponDataSO weapon = weaponOptions[i];
            if (weapon == null)
                continue;

            Button button = CreateOptionButton(
                weaponListRoot,
                string.IsNullOrEmpty(weapon.displayName) ? weapon.name : weapon.displayName,
                weaponButtons.Count);
            SurvivorWeaponDataSO captured = weapon;
            button.onClick.AddListener(() => SelectWeapon(captured));
            weaponButtons.Add(button);
            weaponButtonSources.Add(weapon);
        }

        HighlightWeaponButtons();
    }

    private void RebuildBuffList()
    {
        ClearChildren(buffListRoot, buffButtons);
        buffButtonSources.Clear();
        if (buffListRoot == null || buffOptions == null)
            return;

        for (int i = 0; i < buffOptions.Length; i++)
        {
            SurvivorBuffDataSO buff = buffOptions[i];
            if (buff == null)
                continue;

            Button button = CreateOptionButton(
                buffListRoot,
                string.IsNullOrEmpty(buff.displayName) ? buff.name : buff.displayName,
                buffButtons.Count);
            SurvivorBuffDataSO captured = buff;
            button.onClick.AddListener(() => ToggleBuff(captured));
            buffButtons.Add(button);
            buffButtonSources.Add(buff);
        }

        HighlightBuffButtons();
    }

    private Button CreateOptionButton(Transform parent, string label, int index)
    {
        GameObject go = new GameObject($"Option_{index}_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 40f);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;

        Image image = go.GetComponent<Image>();
        image.color = SurvivorUiStyle.DarkPanel;

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = SurvivorUiStyle.DarkPanel;
        colors.highlightedColor = new Color(0.18f, 0.2f, 0.24f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        colors.selectedColor = new Color(0.22f, 0.45f, 0.7f, 1f);
        button.colors = colors;

        GameObject textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 2f);
        textRt.offsetMax = new Vector2(-8f, -2f);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontSize = 18f;
        tmp.raycastTarget = false;
        SurvivorUiStyle.ApplyTextOnDark(tmp);
        if (uiFont != null)
            SurvivorUiStyle.ApplyFont(tmp, uiFont);

        return button;
    }

    private void SelectWeapon(SurvivorWeaponDataSO weapon)
    {
        selectedWeapon = weapon;
        HighlightWeaponButtons();
        RefreshLabels();
    }

    private void ToggleBuff(SurvivorBuffDataSO buff)
    {
        if (buff == null)
            return;

        int existing = selectedBuffs.IndexOf(buff);
        if (existing >= 0)
        {
            selectedBuffs.RemoveAt(existing);
        }
        else
        {
            if (selectedBuffs.Count >= MaxBuffSelections)
            {
                Debug.Log($"[MatchPrep] Max {MaxBuffSelections} buffs.");
                return;
            }
            selectedBuffs.Add(buff);
        }

        HighlightBuffButtons();
        RefreshLabels();
    }

    private void HighlightWeaponButtons()
    {
        for (int i = 0; i < weaponButtons.Count; i++)
        {
            if (weaponButtons[i] == null || i >= weaponButtonSources.Count)
                continue;
            bool selected = weaponButtonSources[i] == selectedWeapon;
            Image image = weaponButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = selected ? new Color(0.22f, 0.45f, 0.7f, 1f) : SurvivorUiStyle.DarkPanel;
        }
    }

    private void HighlightBuffButtons()
    {
        for (int i = 0; i < buffButtons.Count; i++)
        {
            if (buffButtons[i] == null || i >= buffButtonSources.Count)
                continue;
            bool selected = selectedBuffs.Contains(buffButtonSources[i]);
            Image image = buffButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = selected ? new Color(0.2f, 0.55f, 0.35f, 1f) : SurvivorUiStyle.DarkPanel;
        }
    }

    private void CycleCharacter(int delta)
    {
        if (characterIds == null || characterIds.Length == 0)
            return;
        characterIndex = (characterIndex + delta + characterIds.Length) % characterIds.Length;
        RefreshLabels();
    }

    private void CycleSkin(int delta)
    {
        if (skinIds == null || skinIds.Length == 0)
            return;
        skinIndex = (skinIndex + delta + skinIds.Length) % skinIds.Length;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (characterLabel != null)
            characterLabel.text = $"Character: {characterIds[characterIndex]}";
        if (skinLabel != null)
            skinLabel.text = $"Skin: {skinIds[skinIndex]}";
        if (selectedWeaponLabel != null)
        {
            selectedWeaponLabel.text = selectedWeapon == null
                ? "Weapon: (none)"
                : $"Weapon: {(string.IsNullOrEmpty(selectedWeapon.displayName) ? selectedWeapon.name : selectedWeapon.displayName)}";
        }
        if (selectedBuffsLabel != null)
        {
            if (selectedBuffs.Count == 0)
            {
                selectedBuffsLabel.text = "Buffs: (none)";
            }
            else
            {
                var names = new List<string>(selectedBuffs.Count);
                for (int i = 0; i < selectedBuffs.Count; i++)
                {
                    SurvivorBuffDataSO buff = selectedBuffs[i];
                    names.Add(string.IsNullOrEmpty(buff.displayName) ? buff.name : buff.displayName);
                }
                selectedBuffsLabel.text = $"Buffs ({selectedBuffs.Count}/{MaxBuffSelections}): {string.Join(", ", names)}";
            }
        }
    }

    private async void OnReady()
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        session.SetLoadout(
            selectedWeapon,
            selectedBuffs,
            characterIds[characterIndex],
            skinIds[skinIndex]);

        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;

        // Cloud: establish session before entering the shared ready lobby.
        if (session.IsCloudSession && !session.IsLocalTest)
        {
            bool ok = await SaturnCloudMatchmaker.EnsureExists().QueueByPartySizeAsync(session.PartySize);
            if (!ok)
                Debug.LogWarning("[MatchPrep] Cloud queue failed — entering lobby offline/local.");
        }

        // Loadout done → party lobby (Ready there triggers group deploy).
        if (mainMenu != null)
            mainMenu.ShowPartyLobby();
    }

    private void OnCancel()
    {
        if (mainMenu != null)
            mainMenu.ShowMultiplayer();
    }

    private static void ClearChildren(Transform root, List<Button> cache)
    {
        cache?.Clear();
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
