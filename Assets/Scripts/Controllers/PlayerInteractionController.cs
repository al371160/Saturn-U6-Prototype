using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public Transform interactionOrigin;
    public GameObject alertPanel;
    public LayerMask selectableMask;

    [Header("Minigame Manager")]
    public MinigameManager minigameManager;

    [Header("Audio")]
    public AudioClip interactionSound;
    public AudioSource audioSource;

    private DialogueTrigger currentTrigger;
    private Transform currentTarget;
    private bool justInteracted = false;
    private Transform lastSoundTarget;

    void Update()
    {
        currentTrigger = null;
        bool foundInteractable = false;

        // Reset outline on previous target
        if (currentTarget != null)
        {
            Outline previousOutline = currentTarget.GetComponent<Outline>();
            if (previousOutline != null)
                previousOutline.enabled = false;

            currentTarget = null;
        }

        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Collider[] hitColliders = Physics.OverlapSphere(origin, interactionRange);

        foreach (Collider collider in hitColliders)
        {
            GameObject obj = collider.gameObject;

            bool isValidByTag = obj.CompareTag("NPC") || obj.CompareTag("girlTalk");
            bool isValidByLayer = ((1 << obj.layer) & selectableMask) != 0;

            if (isValidByTag || isValidByLayer || obj.GetComponent<Item>() != null)
            {
                currentTarget = obj.transform;

                Debug.Log($"Found interactable: {obj.name} (Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)})");
                // Enable or add outline
                Outline outline = obj.GetComponent<Outline>();
                if (outline == null) outline = obj.AddComponent<Outline>();
                outline.enabled = true;

                // ✅ Play sound once when new target enters range
                if (obj.transform != lastSoundTarget)
                {
                    if (interactionSound != null && audioSource != null)
                        audioSource.PlayOneShot(interactionSound);

                    lastSoundTarget = obj.transform;
                }

                // Handle Item interaction (minigame trigger)
                Item itemComponent = obj.GetComponent<Item>();
                if (itemComponent != null)
                {
                    foundInteractable = true;

                    if (!justInteracted)
                    {
                        alertPanel.SetActive(true);

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            alertPanel.SetActive(false);
                            justInteracted = true;

                            minigameManager.StartMinigame(itemComponent.itemData,
                                () => { itemComponent.CollectItem(); },
                                () => { Debug.Log("Minigame failed."); }); // You can add fail behavior later
                        }
                    }
                }
                else if (isValidByTag)
                {
                    // Handle NPC and girlTalk interactions
                    currentTrigger = obj.GetComponent<DialogueTrigger>() ?? obj.GetComponentInParent<DialogueTrigger>();
                    foundInteractable = true;

                    if (!justInteracted)
                    {
                        alertPanel.SetActive(true);

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            currentTrigger?.TriggerDialogue();
                            alertPanel.SetActive(false);
                            justInteracted = true;
                        }
                    }
                }
                else
                {
                    foundInteractable = true;
                }

                break; // Only handle the first interactable found
            }
        }

        if (!foundInteractable)
        {
            alertPanel.SetActive(false);
            justInteracted = false;
            lastSoundTarget = null; // Reset so sound can play again later
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, interactionRange);
    }
}
