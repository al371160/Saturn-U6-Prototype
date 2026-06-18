using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Node")]
public class DialogueNodeSO : ScriptableObject
{
    public List<DialogueLine> lines;
    public List<DialogueChoice> choices;

    [Header("Optional Item Check")]
    public string requiredItemName;
    public int requiredItemCount = 1;
    public bool checkOnly = false; // If true, don't take the item, just check

    [Header("Fallback Node")]
    public DialogueNodeSO fallbackNode;
}

[System.Serializable]
public class DialogueLine
{
    public bool isPlayer;
    public string speakerName;
    [TextArea(2, 5)]
    public string text;
    public float waitAfterLine = 0.5f;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueNodeSO nextNode;
}
