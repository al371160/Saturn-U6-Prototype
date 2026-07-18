using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small always-on radar (top-right) showing nearby SurvivorLandmarkMarkers relative to the player,
/// expandable to a fixed-scale view of the whole play area with M. North-up (doesn't rotate with the
/// player) — only the player arrow rotates to indicate facing, keeping the projection math simple.
/// Built procedurally like SurvivorInventoryUI/SurvivorLevelUpUI rather than from a prefab.
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
    public float expandedPixelSize = 520f;
    public float mapWorldHalfExtent = 280f;

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
    private readonly List<MarkerWidgets> radarMarkers = new List<MarkerWidgets>();
    private readonly List<MarkerWidgets> expandedMarkers = new List<MarkerWidgets>();
    private bool expanded;
    private Transform playerTransform;

    private void Start()
    {
        BuildUiIfMissing();
        SetExpanded(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetExpanded(!expanded);

        if (playerTransform == null)
        {
            SurvivorMinigamePlayer player = controller != null ? controller.MinigamePlayer : null;
            if (player != null)
                playerTransform = player.transform;
        }

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
        expandedPanel.gameObject.SetActive(expanded);
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
            label.color = Color.white;
            if (font != null)
                label.font = font;
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

        // Small always-on radar, top-right corner.
        GameObject radarObject = new GameObject("Radar");
        radarObject.transform.SetParent(canvasObject.transform, false);
        Image radarBg = radarObject.AddComponent<Image>();
        radarBg.color = new Color(0.05f, 0.06f, 0.08f, 0.75f);
        radarPanel = radarObject.GetComponent<RectTransform>();
        radarPanel.anchorMin = new Vector2(1f, 1f);
        radarPanel.anchorMax = new Vector2(1f, 1f);
        radarPanel.pivot = new Vector2(1f, 1f);
        radarPanel.sizeDelta = new Vector2(radarPixelSize, radarPixelSize);
        radarPanel.anchoredPosition = new Vector2(-24f, -24f);

        radarPlayerIcon = CreatePlayerArrow(radarPanel);

        GameObject hintObject = new GameObject("ToggleHint");
        hintObject.transform.SetParent(radarPanel, false);
        TextMeshProUGUI hint = hintObject.AddComponent<TextMeshProUGUI>();
        hint.text = "[M]";
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontSize = 11f;
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        if (font != null)
            hint.font = font;
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(60f, 16f);
        hintRect.anchoredPosition = new Vector2(0f, 4f);

        // Expanded full-map view, centered, toggled with M.
        GameObject expandedObject = new GameObject("ExpandedMap");
        expandedObject.transform.SetParent(canvasObject.transform, false);
        Image expandedBg = expandedObject.AddComponent<Image>();
        expandedBg.color = new Color(0.04f, 0.05f, 0.07f, 0.92f);
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

        expandedPlayerIcon = CreatePlayerArrow(expandedPanel);
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
