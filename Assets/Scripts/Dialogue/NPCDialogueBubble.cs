using System.Collections;
using TMPro;
using UnityEngine;

public class NPCDialogueBubble : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bubblePanel;
    public TMP_Text textLabel;

    [Header("Typing Settings")]
    public float typeSpeed = 0.03f;
    private bool typing = false;
    public bool IsTyping() => typing;

    [Header("Typing Sound Settings")]
    public AudioClip typingSound;
    public AudioSource audioSource;
    public int charactersPerSound = 2;

    public void ShowLine(string line)
    {
        StopAllCoroutines();
        bubblePanel.SetActive(true);
        textLabel.text = "";
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

            if (charactersPerSound > 0 &&
                charCount % charactersPerSound == 0 &&
                !char.IsWhiteSpace(c) &&
                typingSound && audioSource)
            {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        typing = false;
    }

    public void HideBubble()
    {
        bubblePanel.SetActive(false);
        textLabel.text = "";
    }
}
