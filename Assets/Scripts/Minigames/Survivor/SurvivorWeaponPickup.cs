using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World weapon drop: approach for a screen-space overlay prompt (name / rarity / stars / E),
/// press E to equip. The prompt is a single shared overlay parented on SurvivorHud (falling back to
/// a dedicated overlay canvas if the HUD isn't built yet) — it no longer billboards a world-space
/// canvas, so it stays crisp and legible regardless of camera distance/angle.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorWeaponPickup : MonoBehaviour
{
    private const float InteractRadius = 1.85f;
    private const float SpinDegreesPerSecond = 30f;

    private static GameObject sharedPromptRoot;
    private static Image sharedBubbleOutline;
    private static TMP_Text sharedPromptText;
    private static SurvivorWeaponPickup activeOwner;

    private SurvivorMinigameController controller;
    private SurvivorWeaponDataSO weapon;
    private int startStar = 1;
    private GameObject visual;

    private bool playerInRange;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        SurvivorWeaponDataSO weaponData,
        int starLevel = 1,
        float pickupMagnetRadius = 7f)
    {
        // target / magnetRadius retained for call-site compatibility; pickup is E-interact only.
        controller = owner;
        weapon = weaponData;
        startStar = weapon != null ? Mathf.Clamp(starLevel, 1, weapon.MaxStar) : 1;
        ConfigureCollider();
        BuildVisual();
    }

    private void ConfigureCollider()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        SphereCollider sphere = col as SphereCollider;
        if (sphere != null)
            sphere.radius = InteractRadius;
    }

    private void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "WeaponPickupVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
        Object.Destroy(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null && weapon != null)
        {
            Color rarity = SurvivorLootRarity.GetColor(weapon.rarity);
            Color tint = Color.Lerp(weapon.weaponColor, rarity, 0.55f);
            tint.a = 1f;
            renderer.material.color = tint;
        }
    }

    private void EnsurePromptUi()
    {
        if (sharedPromptRoot != null)
            return;

        Transform parent = ResolveOverlayParent();

        GameObject rootObject = new GameObject("WeaponPickupPrompt", typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 210f);

        GameObject outlineObject = new GameObject("BubbleOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineObject.transform.SetParent(rootObject.transform, false);
        sharedBubbleOutline = outlineObject.GetComponent<Image>();
        sharedBubbleOutline.color = Color.white;
        RectTransform outlineRt = outlineObject.GetComponent<RectTransform>();
        outlineRt.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRt.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRt.pivot = new Vector2(0.5f, 0.5f);

        GameObject fillObject = new GameObject("BubbleFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(outlineObject.transform, false);
        Image bubbleFill = fillObject.GetComponent<Image>();
        bubbleFill.color = SurvivorUiStyle.DarkPanel;
        RectTransform fillRt = fillObject.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(8f, 8f);
        fillRt.offsetMax = new Vector2(-8f, -8f);

        GameObject textObject = new GameObject("PromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(outlineObject.transform, false);
        sharedPromptText = textObject.GetComponent<TextMeshProUGUI>();
        sharedPromptText.alignment = TextAlignmentOptions.Center;
        sharedPromptText.fontSize = 32f;
        SurvivorUiStyle.ApplyTextOnDark(sharedPromptText);
        TMP_FontAsset promptFont = ResolvePromptFont();
        SurvivorUiStyle.ApplyFont(sharedPromptText, promptFont);
        sharedPromptText.enableWordWrapping = true;
        sharedPromptText.overflowMode = TextOverflowModes.Overflow;
        sharedPromptText.raycastTarget = false;

        ContentSizeFitter textFitter = textObject.AddComponent<ContentSizeFitter>();
        textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement textLe = textObject.AddComponent<LayoutElement>();
        textLe.minWidth = 220f;
        textLe.preferredWidth = 360f;

        ContentSizeFitter outlineFitter = outlineObject.AddComponent<ContentSizeFitter>();
        outlineFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        outlineFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = outlineObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 18, 18);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        fillObject.transform.SetAsFirstSibling();

        sharedPromptRoot = rootObject;
        sharedPromptRoot.SetActive(false);
    }

    /// <summary>Prefers the Survivor HUD overlay canvas; falls back to a dedicated overlay canvas
    /// (built once, reused) if a pickup somehow spawns before the HUD does.</summary>
    private Transform ResolveOverlayParent()
    {
        if (controller != null && controller.hudCanvas != null)
            return controller.hudCanvas.transform;

        GameObject overlayObject = new GameObject("SurvivorWeaponPickupOverlayCanvas");
        Canvas overlayCanvas = overlayObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayObject.AddComponent<CanvasScaler>();
        overlayObject.AddComponent<GraphicRaycaster>();
        return overlayObject.transform;
    }

    private void Update()
    {
        if (visual != null)
            visual.transform.Rotate(Vector3.up, SpinDegreesPerSecond * Time.deltaTime, Space.World);

        if (!playerInRange || weapon == null || controller == null || !controller.IsRunning)
            return;

        if (controller.IsPaused && !controller.IsUpgradeMenuOpen)
            return;

        if (activeOwner == this)
            RefreshPromptText();

        if (Input.GetKeyDown(KeyCode.E))
            TryCollect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || weapon == null)
            return;

        playerInRange = true;
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        HidePrompt();
    }

    private void TryCollect()
    {
        if (controller == null || weapon == null || controller.WeaponManager == null)
            return;

        bool alreadyOwned = controller.WeaponManager.IsEquipped(weapon);
        if (!alreadyOwned && !controller.WeaponManager.CanEquipNewWeapon())
        {
            if (activeOwner == this && sharedPromptText != null)
                sharedPromptText.text = "Weapon slots full (10)\nUpgrade owned weapons only";
            return;
        }

        if (!controller.WeaponManager.EquipOrUpgrade(weapon, startStar))
            return;

        controller.NotifyLoadoutChanged();
        SurvivorAudio.PlayWeaponPickup();
        HidePrompt();
        Destroy(gameObject);
    }

    private void ShowPrompt()
    {
        EnsurePromptUi();
        if (sharedPromptRoot == null || sharedPromptText == null)
            return;

        activeOwner = this;
        RefreshPromptText();
        sharedPromptRoot.SetActive(true);
    }

    /// <summary>Rebuilds the shared prompt's text/color for whichever pickup currently owns it.</summary>
    private void RefreshPromptText()
    {
        if (sharedPromptText == null || weapon == null)
            return;

        string name = !string.IsNullOrEmpty(weapon.displayName) ? weapon.displayName : "Weapon";
        SurvivorWeaponRarity rarity = weapon.rarity;
        Color rarityColor = SurvivorLootRarity.GetColor(rarity);
        int maxStar = Mathf.Max(1, weapon.MaxStar);
        string stars = new string('\u2605', startStar) + new string('\u2606', Mathf.Max(0, maxStar - startStar));

        sharedPromptText.text = $"{name} — {rarity}\n{stars}\nE to pick up";
        SurvivorUiStyle.ApplyTextOnDark(sharedPromptText);
        if (sharedBubbleOutline != null)
            sharedBubbleOutline.color = rarityColor;

        if (sharedPromptRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(sharedPromptRoot.GetComponent<RectTransform>());
    }

    private void HidePrompt()
    {
        if (activeOwner != this)
            return;

        activeOwner = null;
        if (sharedPromptRoot != null)
            sharedPromptRoot.SetActive(false);
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
    }

    private TMP_FontAsset ResolvePromptFont()
    {
        if (SurvivorDamagePopup.SharedFont != null)
            return SurvivorDamagePopup.SharedFont;

        if (controller != null && controller.config != null && controller.config.levelUpFont != null)
            return controller.config.levelUpFont;

        return null;
    }

    private void OnDestroy()
    {
        HidePrompt();
    }
}
