using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDialogueBubble : MonoBehaviour
{
    public GameObject bubblePanel;
    public TMP_Text textLabel;
    public GameObject choicePanel;
    public GameObject choiceButtonPrefab;
    public Animator animator;
    public BubblePopAnimation bubblePop;
    public DialogueTrigger currentTrigger;

    //audio
    public AudioClip typingSound;
    public AudioSource audioSource;

    [Header("Typing Sound Settings")]
    public int charactersPerSound = 2; // Change this in Inspector to control frequency

    public float typeSpeed = 0.03f;
    private bool typing = false;
    public bool IsTyping() => typing;


    public void ShowLine(string line)
    {
        StopAllCoroutines();
        ClearChoices();
        bubblePanel.SetActive(true);
        textLabel.text = "";
        bubblePop.PlayPop();
        if (animator) animator.Play("PopIn", -1, 0f);
        StartCoroutine(TypeText(line));
    }

    IEnumerator TypeText(string text)
    {
        typing = true;
        textLabel.text = "";
        int charCount = 0;

        foreach (char c in text)
        {
            textLabel.text += c;
            charCount++;

            if (charactersPerSound > 0 && charCount % charactersPerSound == 0 && !char.IsWhiteSpace(c) && typingSound && audioSource)
            {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        typing = false;
    }


    public void ShowChoices(List<DialogueChoice> choices)
    {
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicePanel.transform);
            TMP_Text btnText = buttonObj.GetComponentInChildren<TMP_Text>();
            btnText.text = choice.choiceText;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager.instance.HandleDialogueChoice(choice);

                if (currentTrigger != null)
                {
                    DialogueManager.instance.StartDialogue(choice.nextNode, currentTrigger);
                }
                else
                {
                    Debug.LogWarning("PlayerDialogueBubble.currentTrigger is null!");
                    DialogueManager.instance.StartDialogue(choice.nextNode, null);  // fallback, but better to have trigger
                }

                HideBubble();
            });

        }
    }


    void ClearChoices()
    {
        foreach (Transform child in choicePanel.transform)
            Destroy(child.gameObject);
    }

    public void HideBubble()
    {
        bubblePanel.SetActive(false);
        textLabel.text = "";
        ClearChoices();
    }
}
