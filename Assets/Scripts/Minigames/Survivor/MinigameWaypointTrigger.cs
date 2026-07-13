using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class MinigameWaypointTrigger : MonoBehaviour
{
    [Header("Minigame")]
    public SurvivorMinigameConfig minigameConfig;
    public SurvivorMinigameController minigameController;

    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public bool oneTimeUse = true;

    [Header("Optional Prompt")]
    public GameObject promptRoot;
    public TextMeshProUGUI promptText;
    [TextArea]
    public string promptMessage = "Press E to enter the survivor trial";

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onMinigameWon;
    public UnityEngine.Events.UnityEvent onMinigameLost;

    private Transform playerInRange;
    private bool hasBeenUsed;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (minigameController == null)
            minigameController = SurvivorMinigameController.Instance ?? FindFirstObjectByType<SurvivorMinigameController>();

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (playerInRange == null || minigameController == null || minigameConfig == null)
            return;

        if (oneTimeUse && hasBeenUsed)
            return;

        if (Input.GetKeyDown(interactKey))
            StartWaypointMinigame();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (oneTimeUse && hasBeenUsed)
            return;

        playerInRange = other.transform;
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (playerInRange == other.transform)
        {
            playerInRange = null;
            SetPromptVisible(false);
        }
    }

    private void StartWaypointMinigame()
    {
        if (minigameController == null || minigameConfig == null || playerInRange == null)
            return;

        Transform player = playerInRange;
        SetPromptVisible(false);
        playerInRange = null;

        minigameController.StartMinigame(
            minigameConfig,
            player,
            HandleWin,
            HandleLoss);
    }

    private void HandleWin()
    {
        if (oneTimeUse)
            hasBeenUsed = true;

        onMinigameWon?.Invoke();
    }

    private void HandleLoss()
    {
        onMinigameLost?.Invoke();
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptText != null && visible)
            promptText.text = promptMessage;
    }
}
