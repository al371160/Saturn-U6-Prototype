/*using UnityEngine;
using System.Collections.Generic;

namespace RedstoneinventeGameStudio
{
    [CreateAssetMenu(menuName = "Dialogue/NPC Dialogue")]
    public class NPCDialogueSO : ScriptableObject
    {
        public string title;
        [TextArea(15, 20)]
        public string lines;

        public bool hasChoices;
        public List<DialogueChoice> choices;
        
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public int nextDialogueIndex; // index of the next dialogue in NPCManager.dialogues
        public UnityEngine.Events.UnityEvent onChoose; // Optional: trigger events based on choice
    }
}
*/