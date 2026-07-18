using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggleable inventory panel (default key: Tab) showing currently equipped weapons (icon + star
/// level) and acquired buffs (icon + stack count). Built procedurally like SurvivorLevelUpUI rather
/// than from a prefab, and rebuilt every time it's open so it always reflects current state.
/// </summary>
public class SurvivorInventoryUI : MonoBehaviour
{
    public SurvivorMinigameController controller;
    public KeyCode toggleKey = KeyCode.Tab;
    public TMP_FontAsset font;

    private Canvas canvas;
    private Transform weaponRow;
    private Transform buffRow;
    private readonly List<GameObject> spawnedSlots = new List<GameObject>();
    private bool isOpen;

    private void Start()
    {
        BuildUiIfMissing();
        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            if (isOpen)
                Show();
            else
                Hide();
        }

        if (isOpen)
            Refresh();
    }

    private void Show()
    {
        canvas.gameObject.SetActive(true);
        Refresh();
    }

    private void Hide()
    {
        canvas.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        ClearSlots();

        if (controller?.WeaponManager != null)
        {
            foreach (SurvivorWeaponBehavior weapon in controller.WeaponManager.EquippedWeapons)
            {
                if (weapon?.Data == null)
                    continue;

                spawnedSlots.Add(CreateSlot(weaponRow, weapon.Data.icon, weapon.Data.weaponColor,
                    weapon.Data.displayName, $"Lv.{weapon.StarLevel}/{weapon.Data.MaxStar}"));
            }
        }

        if (controller != null)
        {
            foreach (KeyValuePair<SurvivorBuffDataSO, int> entry in controller.AcquiredBuffs)
            {
                if (entry.Key == null)
                    continue;

                spawnedSlots.Add(CreateSlot(buffRow, entry.Key.icon, entry.Key.iconColor,
                    entry.Key.displayName, $"x{entry.Value}"));
            }
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i]);
        }

        spawnedSlots.Clear();
    }

    private GameObject CreateSlot(Transform parent, Sprite icon, Color color, string title, string subLabel)
    {
        GameObject slot = new GameObject("Slot_" + title);
        slot.transform.SetParent(parent, false);

        Image background = slot.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);

        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 110f);

        LayoutElement layoutElement = slot.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 96f;
        layoutElement.preferredHeight = 110f;

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slot.transform, false);
        Image iconImage = iconObject.AddComponent<Image>();
        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
        }
        else
        {
            iconImage.color = color;
        }
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        iconRect.anchoredPosition = new Vector2(0f, -8f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(slot.transform, false);
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.97f, 1f);
        ConfigureLabel(titleText, 8f, 12f);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.sizeDelta = new Vector2(-8f, 30f);
        titleRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject subObject = new GameObject("SubLabel");
        subObject.transform.SetParent(slot.transform, false);
        TextMeshProUGUI subText = subObject.AddComponent<TextMeshProUGUI>();
        subText.text = subLabel;
        subText.alignment = TextAlignmentOptions.Center;
        subText.fontStyle = FontStyles.Bold;
        subText.color = new Color(0.85f, 0.9f, 1f);
        ConfigureLabel(subText, 10f, 14f);
        RectTransform subRect = subObject.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0f);
        subRect.pivot = new Vector2(0.5f, 0f);
        subRect.sizeDelta = new Vector2(-8f, 18f);
        subRect.anchoredPosition = new Vector2(0f, 2f);

        return slot;
    }

    private void ConfigureLabel(TextMeshProUGUI label, float minSize, float maxSize)
    {
        if (font != null)
            label.font = font;

        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableAutoSizing = true;
        label.fontSizeMin = minSize;
        label.fontSizeMax = maxSize;
    }

    private void BuildUiIfMissing()
    {
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject("SurvivorInventoryCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(900f, 260f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.color = Color.white;
        if (font != null)
            titleText.font = font;
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 36f);
        titleRect.anchoredPosition = Vector2.zero;

        weaponRow = CreateSectionRow(panel.transform, "Weapons", -44f);
        buffRow = CreateSectionRow(panel.transform, "Buffs", -152f);
    }

    private Transform CreateSectionRow(Transform parent, string label, float yOffset)
    {
        GameObject labelObject = new GameObject(label + "Label");
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontSize = 16;
        labelText.color = new Color(0.8f, 0.85f, 0.95f);
        if (font != null)
            labelText.font = font;
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.sizeDelta = new Vector2(200f, 24f);
        labelRect.anchoredPosition = new Vector2(16f, yOffset);

        GameObject rowObject = new GameObject(label + "Row");
        rowObject.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.sizeDelta = new Vector2(860f, 110f);
        rowRect.anchoredPosition = new Vector2(16f, yOffset - 24f);

        return rowObject.transform;
    }
}
