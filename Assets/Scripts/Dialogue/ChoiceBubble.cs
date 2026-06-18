using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChoiceBubble : MonoBehaviour
{
    public GameObject choiceButtonPrefab;
    public Transform choiceContainer;
    public PlayerController playerController;

    private List<Button> activeChoices = new List<Button>();
    private System.Action<DialogueChoice> onChoiceSelected;

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        for (int i = 0; i < activeChoices.Count && i < 9; i++) // Limit to keys 1-9
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                TriggerChoice(i);
            }
        }
    }

    public void ShowChoices(List<DialogueChoice> choices, System.Action<DialogueChoice> onChoiceSelected)
    {
        gameObject.SetActive(true);
        this.onChoiceSelected = onChoiceSelected;

        ClearChoices();

        foreach (var choice in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            label.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                onChoiceSelected?.Invoke(choice);
                HideChoices();
            });

            activeChoices.Add(btn);
        }
    }

    private void TriggerChoice(int index)
    {
        if (index >= 0 && index < activeChoices.Count)
        {
            activeChoices[index].onClick.Invoke();
        }
    }

    public void HideChoices()
    {
        ClearChoices();
        gameObject.SetActive(false);
        playerController.canMove = true;
    }

    private void ClearChoices()
    {
        foreach (var btn in activeChoices)
        {
            Destroy(btn.gameObject);
        }
        activeChoices.Clear();
    }
}
