using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space aim crosshair (both camera modes), auto-fire chip, and scope zoom slider.
/// </summary>
public class SurvivorCombatHudUI : MonoBehaviour
{
    private TextMeshProUGUI autoFireLabel;
    private TextMeshProUGUI scopeLabel;
    private Slider scopeSlider;
    private RectTransform crosshairRoot;
    private bool built;
    private SurvivorMinigameController controller;

    public void EnsureBuilt(Transform hudRoot)
    {
        if (built || hudRoot == null)
            return;

        controller = hudRoot.GetComponentInParent<SurvivorMinigameController>();
        if (controller == null)
            controller = FindFirstObjectByType<SurvivorMinigameController>();

        BuildAutoFireChip(hudRoot);
        BuildScopeSlider(hudRoot);
        BuildCrosshair(hudRoot);
        built = true;
    }

    private void Update()
    {
        if (!built)
            return;

        if (autoFireLabel != null)
        {
            bool on = SurvivorWeaponManager.AutoFireEnabled;
            autoFireLabel.text = on ? "AUTO-FIRE: ON  [Q]" : "AUTO-FIRE: OFF  [Q]";
            autoFireLabel.color = on ? new Color(0.45f, 1f, 0.55f, 0.95f) : new Color(1f, 0.55f, 0.4f, 0.95f);
        }

        UpdateScopeUi();
        UpdateCrosshair();
    }

    private void UpdateScopeUi()
    {
        if (scopeSlider == null)
            return;

        SurvivorMinigamePlayer player = controller != null ? controller.MinigamePlayer : null;
        if (player == null)
        {
            scopeSlider.interactable = false;
            if (scopeLabel != null)
                scopeLabel.text = "SCOPE: —";
            return;
        }

        float maxMag = player.HighestScopeMagnitude;
        bool hasScope = maxMag > 0.001f;
        scopeSlider.interactable = hasScope;

        if (!hasScope)
        {
            if (scopeLabel != null)
                scopeLabel.text = "SCOPE: 1.0x (none)";
            return;
        }

        // Mouse wheel adjusts scope while cursor is locked in combat.
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float next = Mathf.Clamp01(player.ScopeZoomSlider + scroll * 0.08f);
            player.SetScopeZoomSlider(next);
            scopeSlider.SetValueWithoutNotify(next);
        }
        else if (Mathf.Abs(scopeSlider.value - player.ScopeZoomSlider) > 0.001f)
        {
            scopeSlider.SetValueWithoutNotify(player.ScopeZoomSlider);
        }

        float effective = player.EffectiveScopeZoomMultiplier;
        if (scopeLabel != null)
            scopeLabel.text = $"SCOPE: {effective:0.0}x  (scroll)";
    }

    private void OnScopeSliderChanged(float value)
    {
        SurvivorMinigamePlayer player = controller != null ? controller.MinigamePlayer : null;
        player?.SetScopeZoomSlider(value);
    }

    private void UpdateCrosshair()
    {
        if (crosshairRoot == null)
            return;

        crosshairRoot.gameObject.SetActive(true);

        // Shoulder (locked): center. Top-down (unlocked+hidden): follow invisible mouse.
        Vector2 screenPos = Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : (Vector2)Input.mousePosition;

        Canvas canvas = crosshairRoot.GetComponentInParent<Canvas>();
        Camera eventCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect != null
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out Vector2 local))
        {
            crosshairRoot.anchoredPosition = local;
        }
        else
        {
            crosshairRoot.position = screenPos;
        }
    }

    private void BuildAutoFireChip(Transform hudRoot)
    {
        GameObject chip = new GameObject("AutoFireChip");
        chip.transform.SetParent(hudRoot, false);
        autoFireLabel = chip.AddComponent<TextMeshProUGUI>();
        autoFireLabel.fontSize = 20;
        autoFireLabel.alignment = TextAlignmentOptions.TopRight;
        autoFireLabel.raycastTarget = false;

        RectTransform rect = autoFireLabel.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -120f);
        rect.sizeDelta = new Vector2(280f, 36f);
    }

    private void BuildScopeSlider(Transform hudRoot)
    {
        GameObject root = new GameObject("ScopeZoomPanel");
        root.transform.SetParent(hudRoot, false);
        RectTransform panel = root.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-24f, -160f);
        panel.sizeDelta = new Vector2(260f, 54f);

        GameObject labelObject = new GameObject("ScopeLabel");
        labelObject.transform.SetParent(root.transform, false);
        scopeLabel = labelObject.AddComponent<TextMeshProUGUI>();
        scopeLabel.fontSize = 16;
        scopeLabel.alignment = TextAlignmentOptions.MidlineRight;
        scopeLabel.raycastTarget = false;
        scopeLabel.text = "SCOPE: 1.0x";
        RectTransform labelRect = scopeLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.55f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        GameObject sliderObject = new GameObject("ScopeSlider");
        sliderObject.transform.SetParent(root.transform, false);
        scopeSlider = sliderObject.AddComponent<Slider>();
        scopeSlider.minValue = 0f;
        scopeSlider.maxValue = 1f;
        scopeSlider.wholeNumbers = false;
        scopeSlider.value = 1f;
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObject.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.35f);
        bgRect.anchorMax = new Vector2(1f, 0.65f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        scopeSlider.targetGraphic = bgImage;

        // Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.45f, 0.75f, 1f, 0.95f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        scopeSlider.fillRect = fillRect;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 22f);
        scopeSlider.handleRect = handleRect;
        scopeSlider.direction = Slider.Direction.LeftToRight;

        scopeSlider.onValueChanged.AddListener(OnScopeSliderChanged);
    }

    private void BuildCrosshair(Transform hudRoot)
    {
        GameObject root = new GameObject("AimCrosshair");
        root.transform.SetParent(hudRoot, false);
        crosshairRoot = root.AddComponent<RectTransform>();
        crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
        crosshairRoot.sizeDelta = new Vector2(28f, 28f);

        CreateCrosshairBar(crosshairRoot, "H", new Vector2(18f, 2f));
        CreateCrosshairBar(crosshairRoot, "V", new Vector2(2f, 18f));
    }

    private static Image CreateCrosshairBar(Transform parent, string name, Vector2 size)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(parent, false);
        Image image = bar.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.85f);
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return image;
    }
}
