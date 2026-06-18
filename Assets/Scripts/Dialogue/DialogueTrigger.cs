using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Sequence")]
    public List<DialogueNodeSO> dialogueSequence;
    private int currentNodeIndex = 0;

    public NPCDialogueBubble npcDialogueBubble;

    [Header("Optional")]
    public DialogueNodeSO fallbackNode;
    public GameObject particleEffect;

    public void TriggerDialogue()
    {
        if (dialogueSequence == null || dialogueSequence.Count == 0)
        {
            Debug.LogWarning("Dialogue sequence is empty.");
            return;
        }

        if (currentNodeIndex >= dialogueSequence.Count)
        {
            Debug.Log("Dialogue sequence finished. No more dialogue to trigger.");
            return;
        }

        NPCDialogueBubble npcBubble = GetComponentInChildren<NPCDialogueBubble>();
        

        if (npcBubble != null)
        {
            DialogueManager.instance.npcBubble = npcBubble;
            DialogueManager.instance.playerBubble.currentTrigger = this;

            DialogueNodeSO currentNode = GetCurrentNode();
            if (currentNode != null)
            {
                DialogueManager.instance.StartDialogue(currentNode, this);

                if (particleEffect != null)
                {
                    particleEffect.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("Current dialogue node is null.");
            }
        }
        else
        {
            Debug.LogWarning("No NPCDialogueBubble found on NPC! replacing with public assignment");
            if (npcDialogueBubble != null) {
                npcBubble = npcDialogueBubble;
            } else 
            {
                Debug.Log("ur cooked lil bro");
            }
        }
    }

    public DialogueNodeSO GetCurrentNode()
    {
        if (currentNodeIndex >= 0 && currentNodeIndex < dialogueSequence.Count)
        {
            return dialogueSequence[currentNodeIndex];
        }
        return null;
    }

    public void AdvanceToNextNode()
    {
        if (currentNodeIndex < dialogueSequence.Count - 1)
        {
            currentNodeIndex++;
        }
        else
        {
            Debug.Log("Reached end of dialogue sequence. Staying on last node.");
            // Optional: Disable this trigger or loop/reset if desired
            // currentNodeIndex = 0;
            // gameObject.SetActive(false);
        }
    }

    public bool HasNextNode()
    {
        return currentNodeIndex + 1 < dialogueSequence.Count;
    }

    public DialogueNodeSO GetFallbackNode()
    {
        return fallbackNode;
    }
}
