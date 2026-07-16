using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SurvivorLevelUpUI : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private Canvas canvas;
    private TextMeshProUGUI titleText;
    private Transform choiceRoot;
    private TMP_FontAsset font;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    public void Initialize(SurvivorMinigameController owner)
    {
        controller = owner;
        font = owner.config != null ? owner.config.levelUpFont : null;
        BuildUiIfMissing();
        Hide();
    }

    public void ShowChoices(List<SurvivorUpgradeChoice> choices, string title = "LEVEL UP! Choose an upgrade")
    {
        BuildUiIfMissing();
        ClearButtons();

        if (titleText != null)
            titleText.text = title;

        for (int i = 0; i < choices.Count; i++)
            spawnedButtons.Add(CreateChoiceButton(choices[i], i, choices.Count));

        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }

        spawnedButtons.Clear();
    }

    private GameObject CreateChoiceButton(SurvivorUpgradeChoice choice, int index, int total)
    {
        GameObject buttonObject = new GameObject($"Choice_{index}");
        buttonObject.transform.SetParent(choiceRoot, false);

        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 200f);
        float middleIndex = (total - 1) * 0.5f;
        rect.anchoredPosition = new Vector2((index - middleIndex) * 340f, 0f);

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(buttonObject.transform, false);
        Image icon = iconObject.AddComponent<Image>();
        icon.color = choice.IconColor;
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        iconRect.anchoredPosition = new Vector2(0f, -16f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = choice.Title;
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.97f, 1f);
        ConfigureLabel(title, font, 14f, 22f);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 32f);
        titleRect.anchoredPosition = new Vector2(0f, -84f);

        GameObject descriptionObject = new GameObject("Description");
        descriptionObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI description = descriptionObject.AddComponent<TextMeshProUGUI>();
        description.text = choice.Description;
        description.alignment = TextAlignmentOptions.Top;
        description.color = new Color(0.85f, 0.88f, 0.95f);
        ConfigureLabel(description, font, 10f, 16f);
        RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
        descriptionRect.anchorMin = Vector2.zero;
        descriptionRect.anchorMax = Vector2.one;
        descriptionRect.offsetMin = new Vector2(12f, 12f);
        descriptionRect.offsetMax = new Vector2(-12f, -118f);

        button.onClick.AddListener(() => OnChoiceSelected(choice));

        return buttonObject;
    }

    private void ConfigureLabel(TextMeshProUGUI label, TMP_FontAsset labelFont, float minSize, float maxSize)
    {
        if (labelFont != null)
            label.font = labelFont;

        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableAutoSizing = true;
        label.fontSizeMin = minSize;
        label.fontSizeMax = maxSize;
    }

    private void OnChoiceSelected(SurvivorUpgradeChoice choice)
    {
        choice.Apply?.Invoke(controller);
        Hide();
        controller.ResumeAfterUpgrade();
    }

    private void BuildUiIfMissing()
    {
        if (canvas != null)
            return;

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObject = new GameObject("SurvivorLevelUpCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(canvasObject.transform, false);
        titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "LEVEL UP! Choose an upgrade";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 36;
        titleText.color = Color.white;
        if (font != null)
            titleText.font = font;

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(800f, 60f);

        GameObject choiceRootObject = new GameObject("Choices");
        choiceRootObject.transform.SetParent(canvasObject.transform, false);
        RectTransform choiceRootRect = choiceRootObject.AddComponent<RectTransform>();
        choiceRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        choiceRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        choiceRootRect.anchoredPosition = Vector2.zero;
        choiceRootRect.sizeDelta = Vector2.zero;
        choiceRoot = choiceRootObject.transform;

        canvas.gameObject.SetActive(false);
    }
}
