// DialogueManager.cs (Final Cleaned-up Version)

using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;
    public PlayerController playerController;
    public Animator playerAnim;

    public PlayerDialogueBubble playerBubble;
    public NPCDialogueBubble npcBubble;
    [SerializeField] private ChoiceBubble choiceBubble;
    private DialogueNodeSO resumeNodeOnRetry = null;

    public bool isDialogueActive = false;


    //private InventoryManager inventoryManager;
    private DialogueTrigger currentTrigger; // Keep track of the trigger

    private void Awake() => instance = this;

public void StartDialogue(DialogueNodeSO node, DialogueTrigger trigger, bool resumeIfAny = true)
{
    if (isDialogueActive) return;

    isDialogueActive = true;
    currentTrigger = trigger;

    playerAnim.SetBool("isIdle", true);
    StopAllCoroutines();
    playerController.canMove = false;

    DialogueNodeSO nodeToStart = node;

    if (resumeIfAny && resumeNodeOnRetry != null)
    {
        nodeToStart = resumeNodeOnRetry;
        resumeNodeOnRetry = null; // reset after resuming
    }

    StartCoroutine(RunDialogue(nodeToStart));
}

    IEnumerator RunDialogue(DialogueNodeSO node)
    {
        /* // Item check
        if (node.requiresItem)
        {
            var inventoryHelper = Object.FindAnyObjectByType<InventoryEventHelper>();
            if (inventoryHelper == null)
            {
                Debug.LogWarning("No InventoryEventHelper found.");
                EndDialogue();
                yield break;
            }

            if (!inventoryHelper.inventoryManager.HasItem(node.requiredItemName, node.requiredItemCount))
            {
                Debug.Log("Missing item. Checking fallback...");
                if (node.fallbackNode != null)
                {
                    yield return RunDialogue(node.fallbackNode);
                }
                EndDialogue();
                yield break;
            }
            else
            {
                inventoryHelper.TakeItem(node.requiredItemName, node.requiredItemCount);
            }
        } */

        if (!string.IsNullOrEmpty(node.requiredItemName))
        {
            var inventoryHelper = Object.FindAnyObjectByType<InventoryEventHelper>();
            Debug.Log($"Current node: {node.name}, fallback node: {(node.fallbackNode != null ? node.fallbackNode.name : "null")}");

            bool hasItem = inventoryHelper.inventoryManager.HasItem(node.requiredItemName, node.requiredItemCount);

            if (node.checkOnly)
            {
                if (!hasItem)
                {
                    Debug.Log("Missing item. Showing fallback...");
                    if (node.fallbackNode != null)
                    {
                        yield return RunDialogue(node.fallbackNode);
                    }

                    // Save the current node to resume on next dialogue start
                    resumeNodeOnRetry = node;

                    EndDialogue(); // close UI, let player exit
                    yield break;   // stop this coroutine
                }

            }
            else
            {
                if (hasItem)
                {
                    inventoryHelper.TakeItem(node.requiredItemName, node.requiredItemCount);
                }
                else
                {
                    Debug.Log("Missing item. Showing fallback...");
                    if (node.fallbackNode != null)
                    {
                        yield return RunDialogue(node.fallbackNode);
                    }

                    // Save the current node to resume on next dialogue start
                    resumeNodeOnRetry = node;

                    EndDialogue(); // close UI, let player exit
                    yield break;   // stop this coroutine
                }
            }
        }

        foreach (var line in node.lines)
        {
            if (line.isPlayer)
            {
                npcBubble.HideBubble();
                playerBubble.ShowLine(line.text);
            }
            else
            {
                playerBubble.HideBubble();
                npcBubble.ShowLine(line.text);
            }

            yield return new WaitUntil(() => !playerBubble.IsTyping() && !npcBubble.IsTyping());
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.anyKeyDown);
        }

        if (node.choices != null && node.choices.Count > 0)
        {
            playerBubble.HideBubble();
            npcBubble.HideBubble();
            choiceBubble.ShowChoices(node.choices, (selectedChoice) =>
            {
                HandleDialogueChoice(selectedChoice);
                StartCoroutine(RunDialogue(selectedChoice.nextNode));
            });
        }
        else
        {
            EndDialogue();

            // Advance to next dialogue if triggered by a trigger
            if (currentTrigger != null)
            {
                currentTrigger.AdvanceToNextNode();
            }
        }
    }

    private void EndDialogue()
    {
        if (npcBubble.transform.parent.name == "The Boss")
        {
            // Somewhere in EndDialogue(), before or after isDialogueActive = false
            GameObject.Find("shipDoorCollider").GetComponent<DoorTrigger>().AllowOpening();

        }

        playerBubble.HideBubble();
        npcBubble.HideBubble();

        isDialogueActive = false; // ✅ Reset flag

        playerAnim.SetBool("isIdle", false);
        playerController.canMove = true;
    }


    public void HandleDialogueChoice(DialogueChoice choice)
    {
        InventoryEventHelper inventoryHelper = Object.FindAnyObjectByType<InventoryEventHelper>();

        if (inventoryHelper == null)
        {
            Debug.LogWarning("InventoryEventHelper not found in scene.");
            return;
        }

        if (choice.choiceText == "Confirm (Receive 1 Power Core)")
        {
            inventoryHelper.GiveItemByName("Power Core", 1);
            Debug.Log("giving");
        }
        else if (choice.choiceText == "get a Battery")
        {
            inventoryHelper.GiveItemByName("Battery", 1);
        }
    }
}
