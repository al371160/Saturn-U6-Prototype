using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World weapon drop: approach for a rarity-colored bubble (name / rarity / E), press E to equip.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorWeaponPickup : MonoBehaviour
{
    private const float InteractRadius = 1.85f;
    private const float SpinDegreesPerSecond = 30f;

    private SurvivorMinigameController controller;
    private SurvivorWeaponDataSO weapon;
    private int startStar = 1;
    private GameObject visual;

    private bool playerInRange;
    private bool promptVisible;

    private Canvas promptCanvas;
    private RectTransform bubbleRoot;
    private Image bubbleOutline;
    private Image bubbleFill;
    private TMP_Text promptText;
    private Camera promptCamera;

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
        EnsurePromptUi();
        HidePrompt();
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
        if (bubbleRoot != null)
            return;

        promptCamera = Camera.main;

        GameObject canvasObject = new GameObject("WeaponPickupPromptCanvas");
        canvasObject.transform.SetParent(transform, false);
        promptCanvas = canvasObject.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.worldCamera = promptCamera;
        canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

        RectTransform canvasRt = promptCanvas.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(4f, 2.5f);
        canvasRt.localPosition = new Vector3(0f, 1.6f, 0f);
        canvasRt.localScale = Vector3.one * 0.01f;

        GameObject outlineObject = new GameObject("BubbleOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineObject.transform.SetParent(canvasObject.transform, false);
        bubbleOutline = outlineObject.GetComponent<Image>();
        bubbleOutline.color = Color.white;
        RectTransform outlineRt = outlineObject.GetComponent<RectTransform>();
        outlineRt.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRt.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRt.pivot = new Vector2(0.5f, 0.5f);

        GameObject fillObject = new GameObject("BubbleFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(outlineObject.transform, false);
        bubbleFill = fillObject.GetComponent<Image>();
        bubbleFill.color = SurvivorUiStyle.DarkPanel;
        RectTransform fillRt = fillObject.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(8f, 8f);
        fillRt.offsetMax = new Vector2(-8f, -8f);

        GameObject textObject = new GameObject("PromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(fillObject.transform, false);
        promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 36f;
        SurvivorUiStyle.ApplyTextOnDark(promptText);
        TMP_FontAsset promptFont = ResolvePromptFont();
        SurvivorUiStyle.ApplyFont(promptText, promptFont);
        promptText.enableWordWrapping = true;
        promptText.overflowMode = TextOverflowModes.Overflow;
        promptText.raycastTarget = false;

        ContentSizeFitter textFitter = textObject.AddComponent<ContentSizeFitter>();
        textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform textRt = textObject.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;

        ContentSizeFitter outlineFitter = outlineObject.AddComponent<ContentSizeFitter>();
        outlineFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        outlineFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = outlineObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // Re-parent text under outline for layout; fill stays as decorative inset.
        textObject.transform.SetParent(outlineObject.transform, false);
        fillObject.transform.SetAsFirstSibling();

        LayoutElement textLe = textObject.AddComponent<LayoutElement>();
        textLe.minWidth = 220f;
        textLe.preferredWidth = 320f;

        bubbleRoot = outlineRt;
        canvasObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (promptCanvas == null || !promptVisible)
            return;

        if (promptCamera == null)
            promptCamera = Camera.main;
        if (promptCamera == null)
            return;

        promptCanvas.worldCamera = promptCamera;
        Transform canvasTransform = promptCanvas.transform;
        canvasTransform.position = transform.position + Vector3.up * 1.6f;
        canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - promptCamera.transform.position);
    }

    private void Update()
    {
        if (visual != null)
            visual.transform.Rotate(Vector3.up, SpinDegreesPerSecond * Time.deltaTime, Space.World);

        if (!playerInRange || weapon == null || controller == null || !controller.IsRunning)
            return;

        if (controller.IsPaused && !controller.IsUpgradeMenuOpen)
            return;

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
        if (controller == null || weapon == null)
            return;

        controller.WeaponManager?.EquipOrUpgrade(weapon, startStar);
        controller.NotifyLoadoutChanged();
        SurvivorAudio.PlayWeaponPickup();
        HidePrompt();
        Destroy(gameObject);
    }

    private void ShowPrompt()
    {
        EnsurePromptUi();
        if (promptCanvas == null || promptText == null)
            return;

        string name = !string.IsNullOrEmpty(weapon.displayName) ? weapon.displayName : "Weapon";
        SurvivorWeaponRarity rarity = weapon.rarity;
        Color rarityColor = SurvivorLootRarity.GetColor(rarity);

        promptText.text = name + "\n" + rarity + " ★" + startStar + "\nE to pickup";
        SurvivorUiStyle.ApplyTextOnDark(promptText);
        if (bubbleOutline != null)
            bubbleOutline.color = rarityColor;

        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRoot);
        promptCanvas.gameObject.SetActive(true);
        promptVisible = true;
    }

    private void HidePrompt()
    {
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
        promptVisible = false;
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
