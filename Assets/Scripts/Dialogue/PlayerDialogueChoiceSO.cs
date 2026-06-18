/* using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Dialogue/Player Dialogue Choice")]
public class PlayerDialogueChoiceSO : ScriptableObject
{
    public string promptText; // What the player is "saying"

    public List<PlayerChoice> choices;
}

[System.Serializable]
public class PlayerChoice
{
    public string choiceText;
    public UnityEvent onChosen;
    public PlayerDialogueChoiceSO nextChoice; // Chain dialogue choices
}
*/