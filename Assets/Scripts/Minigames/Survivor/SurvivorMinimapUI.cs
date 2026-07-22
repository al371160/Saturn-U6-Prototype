using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small always-on radar (top-right) showing nearby SurvivorLandmarkMarkers relative to the player,
/// expandable to a large fixed-scale view of the whole play area with M. North-up.
/// Draws the live storm safe-zone ring and a phase countdown clock.
/// </summary>
public class SurvivorMinimapUI : MonoBehaviour
{
    public SurvivorMinigameController controller;
    public KeyCode toggleKey = KeyCode.M;
    public TMP_FontAsset font;

    [Header("Small radar")]
    public float radarWorldRange = 220f;
    public float radarPixelSize = 160f;

    [Header("Expanded map (fixed scale, whole play area)")]
    [Tooltip("Used as fallback; at runtime expanded size is ~90% of the shorter screen axis.")]
    public float expandedPixelSize = 900f;
    public float mapWorldHalfExtent = 520f;

    private static readonly List<SurvivorLandmarkMarker> Landmarks = new List<SurvivorLandmarkMarker>();

    public static void RegisterLandmark(SurvivorLandmarkMarker marker)
    {
        if (!Landmarks.Contains(marker))
            Landmarks.Add(marker);
    }

    public static void UnregisterLandmark(SurvivorLandmarkMarker marker)
    {
        Landmarks.Remove(marker);
    }

    private class MarkerWidgets
    {
        public SurvivorLandmarkMarker source;
        public RectTransform dot;
        public TextMeshProUGUI label;
    }

    private Canvas canvas;
    private RectTransform radarPanel;
    private RectTransform radarPlayerIcon;
    private RectTransform expandedPanel;
    private RectTransform expandedPlayerIcon;
    private RectTransform radarStormRing;
    private RectTransform expandedStormRing;
    private TextMeshProUGUI stormClockLabel;
    private readonly List<MarkerWidgets> radarMarkers = new List<MarkerWidgets>();
    private readonly List<MarkerWidgets> expandedMarkers = new List<MarkerWidgets>();
    private bool expanded;
    private Transform playerTransform;
    private SurvivorStormController storm;

    private void Start()
    {
        BuildUiIfMissing();
        SetExpanded(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetExpanded(!expanded);

        if (storm == null)
            storm = SurvivorStormController.Instance != null
                ? SurvivorStormController.Instance
                : FindFirstObjectByType<SurvivorStormController>();

        if (playerTransform == null)
        {
            SurvivorMinigamePlayer player = controller != null ? controller.MinigamePlayer : null;
            if (player != null)
                playerTransform = player.transform;
        }

        UpdateStormClock();

        if (playerTransform == null)
            return;

        SyncMarkerWidgetCount();
        UpdateRadar();
        if (expanded)
            UpdateExpanded();
    }

    private void SetExpanded(bool value)
    {
        expanded = value;
        if (expandedPanel != null)
        {
            if (expanded)
            {
                float size = Mathf.Min(Screen.width, Screen.height) * 0.9f;
                expandedPixelSize = Mathf.Max(720f, size);
                expandedPanel.sizeDelta = new Vector2(expandedPixelSize, expandedPixelSize);
            }

            expandedPanel.gameObject.SetActive(expanded);
        }
    }

    private void UpdateStormClock()
    {
        if (stormClockLabel == null)
            return;

        if (storm == null)
        {
            stormClockLabel.text = "";
            return;
        }

        float remaining = storm.PhaseRemainingSeconds;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        int phase = storm.CurrentPhaseIndex + 1;
        int totalPhases = storm.phases != null ? storm.phases.Length : 0;
        stormClockLabel.text = $"STORM {phase}/{totalPhases}  {minutes:00}:{seconds:00}";
    }

    private void SyncMarkerWidgetCount()
    {
        while (radarMarkers.Count < Landmarks.Count)
        {
            radarMarkers.Add(CreateMarkerWidget(radarPanel, false));
            expandedMarkers.Add(CreateMarkerWidget(expandedPanel, true));
        }

        while (radarMarkers.Count > Landmarks.Count)
        {
            DestroyMarkerWidget(radarMarkers, radarMarkers.Count - 1);
            DestroyMarkerWidget(expandedMarkers, expandedMarkers.Count - 1);
        }
    }

    private void DestroyMarkerWidget(List<MarkerWidgets> list, int index)
    {
        if (list[index].dot != null)
            Destroy(list[index].dot.gameObject);
        list.RemoveAt(index);
    }

    private void UpdateRadar()
    {
        Vector3 playerPos = playerTransform.position;
        float playerYaw = playerTransform.eulerAngles.y;
        radarPlayerIcon.localRotation = Quaternion.Euler(0f, 0f, -playerYaw);

        float halfSize = radarPixelSize * 0.5f;
        float scale = halfSize / radarWorldRange;

        for (int i = 0; i < Landmarks.Count; i++)
        {
            SurvivorLandmarkMarker landmark = Landmarks[i];
            MarkerWidgets widget = radarMarkers[i];
            if (landmark == null)
            {
                widget.dot.gameObject.SetActive(false);
                continue;
            }

            Vector3 delta = landmark.transform.position - playerPos;
            Vector2 uiPos = new Vector2(delta.x, delta.z) * scale;
            float dist = uiPos.magnitude;
            float maxDist = halfSize - 8f;
            if (dist > maxDist)
                uiPos = uiPos.normalized * maxDist;

            widget.dot.gameObject.SetActive(true);
            widget.dot.anchoredPosition = uiPos;
            widget.dot.GetComponent<Image>().color = landmark.mapColor;
        }

        UpdateStormRing(radarStormRing, radarPanel, playerPos, scale, radarWorldRange, playerRelative: true);
    }

    private void UpdateExpanded()
    {
        float halfSize = expandedPixelSize * 0.5f;
        float scale = halfSize / mapWorldHalfExtent;

        Vector2 playerUiPos = new Vector2(playerTransform.position.x, playerTransform.position.z) * scale;
        expandedPlayerIcon.anchoredPosition = playerUiPos;
        expandedPlayerIcon.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);

        for (int i = 0; i < Landmarks.Count; i++)
        {
            SurvivorLandmarkMarker landmark = Landmarks[i];
            MarkerWidgets widget = expandedMarkers[i];
            if (landmark == null)
            {
                widget.dot.gameObject.SetActive(false);
                continue;
            }

            widget.dot.gameObject.SetActive(true);
            Vector2 uiPos = new Vector2(landmark.transform.position.x, landmark.transform.position.z) * scale;
            widget.dot.anchoredPosition = uiPos;
            widget.dot.GetComponent<Image>().color = landmark.mapColor;
            widget.label.text = landmark.displayName;
        }

        UpdateStormRing(expandedStormRing, expandedPanel, Vector3.zero, scale, mapWorldHalfExtent, playerRelative: false);
    }

    private void UpdateStormRing(RectTransform ring, RectTransform panel, Vector3 playerPos, float scale, float halfExtent, bool playerRelative)
    {
        if (ring == null || storm == null)
        {
            if (ring != null)
                ring.gameObject.SetActive(false);
            return;
        }

        float radiusPx = storm.CurrentRadius * scale;
        float panelHalf = panel.sizeDelta.x * 0.5f;
        if (radiusPx < 4f)
        {
            ring.gameObject.SetActive(false);
            return;
        }

        ring.gameObject.SetActive(true);
        ring.sizeDelta = new Vector2(radiusPx * 2f, radiusPx * 2f);

        if (playerRelative)
        {
            Vector3 center = storm.CenterPosition;
            Vector2 uiPos = new Vector2(center.x - playerPos.x, center.z - playerPos.z) * scale;
            ring.anchoredPosition = uiPos;
        }
        else
        {
            Vector3 center = storm.CenterPosition;
            ring.anchoredPosition = new Vector2(center.x, center.z) * scale;
        }

        // Keep ring readable even if larger than panel — UI will clip via mask if present.
        if (radiusPx > panelHalf * 3f)
            ring.gameObject.SetActive(radiusPx < panelHalf * 8f);
    }

    private MarkerWidgets CreateMarkerWidget(RectTransform parent, bool withLabel)
    {
        GameObject dotObject = new GameObject("LandmarkDot");
        dotObject.transform.SetParent(parent, false);
        Image dotImage = dotObject.AddComponent<Image>();
        dotImage.color = Color.white;
        RectTransform dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = withLabel ? new Vector2(14f, 14f) : new Vector2(9f, 9f);

        MarkerWidgets widget = new MarkerWidgets { dot = dotRect };

        if (withLabel)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(dotObject.transform, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 12f;
            SurvivorUiStyle.ApplyFont(label, font);
            SurvivorUiStyle.ApplyTextOnDark(label);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(120f, 20f);
            labelRect.anchoredPosition = new Vector2(0f, -4f);
            widget.label = label;
        }

        return widget;
    }

    private void BuildUiIfMissing()
    {
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject("SurvivorMinimapCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject radarObject = new GameObject("Radar");
        radarObject.transform.SetParent(canvasObject.transform, false);
        Image radarBg = radarObject.AddComponent<Image>();
        radarBg.color = SurvivorUiStyle.DarkPanel;
        radarPanel = radarObject.GetComponent<RectTransform>();
        radarPanel.anchorMin = new Vector2(1f, 1f);
        radarPanel.anchorMax = new Vector2(1f, 1f);
        radarPanel.pivot = new Vector2(1f, 1f);
        radarPanel.sizeDelta = new Vector2(radarPixelSize, radarPixelSize);
        radarPanel.anchoredPosition = new Vector2(-24f, -24f);

        radarStormRing = CreateStormRing(radarPanel);
        radarPlayerIcon = CreatePlayerArrow(radarPanel);

        GameObject hintObject = new GameObject("ToggleHint");
        hintObject.transform.SetParent(radarPanel, false);
        TextMeshProUGUI hint = hintObject.AddComponent<TextMeshProUGUI>();
        hint.text = "[M]";
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontSize = 11f;
        SurvivorUiStyle.ApplyFont(hint, font);
        SurvivorUiStyle.ApplyTextOnDark(hint, muted: true);
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(60f, 16f);
        hintRect.anchoredPosition = new Vector2(0f, 4f);

        GameObject clockObject = new GameObject("StormClock");
        clockObject.transform.SetParent(canvasObject.transform, false);
        stormClockLabel = clockObject.AddComponent<TextMeshProUGUI>();
        stormClockLabel.alignment = TextAlignmentOptions.Top;
        stormClockLabel.fontSize = 26f;
        stormClockLabel.color = Color.white;
        SurvivorUiStyle.ApplyFont(stormClockLabel, font);
        RectTransform clockRect = stormClockLabel.rectTransform;
        clockRect.anchorMin = new Vector2(0.5f, 1f);
        clockRect.anchorMax = new Vector2(0.5f, 1f);
        clockRect.pivot = new Vector2(0.5f, 1f);
        clockRect.sizeDelta = new Vector2(420f, 40f);
        clockRect.anchoredPosition = new Vector2(0f, -18f);

        float size = Mathf.Min(Screen.width, Screen.height) * 0.9f;
        expandedPixelSize = Mathf.Max(720f, size);

        GameObject expandedObject = new GameObject("ExpandedMap");
        expandedObject.transform.SetParent(canvasObject.transform, false);
        Image expandedBg = expandedObject.AddComponent<Image>();
        expandedBg.color = SurvivorUiStyle.DarkPanel;
        expandedPanel = expandedObject.GetComponent<RectTransform>();
        expandedPanel.anchorMin = new Vector2(0.5f, 0.5f);
        expandedPanel.anchorMax = new Vector2(0.5f, 0.5f);
        expandedPanel.pivot = new Vector2(0.5f, 0.5f);
        expandedPanel.sizeDelta = new Vector2(expandedPixelSize, expandedPixelSize);
        expandedPanel.anchoredPosition = Vector2.zero;

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(expandedPanel, false);
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = "MAP";
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 22f;
        title.color = Color.white;
        if (font != null)
            title.font = font;
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 34f);
        titleRect.anchoredPosition = new Vector2(0f, 20f);

        expandedStormRing = CreateStormRing(expandedPanel);
        expandedPlayerIcon = CreatePlayerArrow(expandedPanel);
    }

    private RectTransform CreateStormRing(RectTransform parent)
    {
        GameObject ringObject = new GameObject("StormRing");
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.SetAsFirstSibling();
        Image ringImage = ringObject.AddComponent<Image>();
        ringImage.sprite = CreateRingSprite();
        ringImage.type = Image.Type.Simple;
        ringImage.color = new Color(1f, 1f, 1f, 0.9f);
        ringImage.raycastTarget = false;
        RectTransform ringRect = ringObject.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = new Vector2(64f, 64f);
        ringRect.anchoredPosition = Vector2.zero;
        return ringRect;
    }

    private static Sprite CreateRingSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        float outer = center - 1f;
        float inner = outer - 4f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = d <= outer && d >= inner ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private RectTransform CreatePlayerArrow(RectTransform parent)
    {
        GameObject arrowObject = new GameObject("PlayerArrow");
        arrowObject.transform.SetParent(parent, false);
        Image arrowImage = arrowObject.AddComponent<Image>();
        arrowImage.color = new Color(1f, 0.85f, 0.2f);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.3f);
        arrowRect.sizeDelta = new Vector2(12f, 16f);
        arrowRect.anchoredPosition = Vector2.zero;
        return arrowRect;
    }
}
