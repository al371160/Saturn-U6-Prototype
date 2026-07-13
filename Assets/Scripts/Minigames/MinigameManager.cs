using UnityEngine;
using System;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance;

    [Header("Minigame References (Assigned in Scene)")]
    public QuickTimeMinigame quickTimeMinigame;
    public SurvivorMinigameController survivorMinigame;
    public SurvivorMinigameConfig defaultSurvivorConfig;

    private GameObject activeMinigame;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartMinigame(ItemSO itemSO, Action onMinigameComplete, Action onMinigameFail)
    {
        if (activeMinigame != null)
        {
            Debug.LogWarning("Minigame already active.");
            return;
        }

        if (itemSO.requiredMinigame == ItemSO.MinigameType.QuickTime)
        {
            if (quickTimeMinigame != null)
            {
                activeMinigame = quickTimeMinigame.gameObject;

                quickTimeMinigame.OnMinigameComplete = () =>
                {
                    onMinigameComplete?.Invoke();
                    CleanupMinigame();
                };

                quickTimeMinigame.OnMinigameFail = () =>
                {
                    onMinigameFail?.Invoke();
                    CleanupMinigame();
                };

                quickTimeMinigame.gameObject.SetActive(true);
                quickTimeMinigame.StartMinigame();
            }
            else
            {
                Debug.LogError("QuickTimeMinigame reference not assigned.");
            }
        }
        else if (itemSO.requiredMinigame == ItemSO.MinigameType.Survivor)
        {
            StartSurvivorMinigame(onMinigameComplete, onMinigameFail);
        }
        else
        {
            Debug.LogWarning("Unsupported minigame type or no minigame required.");
            onMinigameComplete?.Invoke(); // Instantly collect if no minigame is required
        }
    }

    public void StartSurvivorMinigame(Action onMinigameComplete, Action onMinigameFail, SurvivorMinigameConfig configOverride = null)
    {
        if (activeMinigame != null)
        {
            Debug.LogWarning("Minigame already active.");
            return;
        }

        SurvivorMinigameController controller = survivorMinigame != null
            ? survivorMinigame
            : SurvivorMinigameController.Instance;

        SurvivorMinigameConfig config = configOverride != null ? configOverride : defaultSurvivorConfig;
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (controller == null || config == null || player == null)
        {
            Debug.LogError("Survivor minigame missing controller, config, or player.");
            onMinigameFail?.Invoke();
            return;
        }

        activeMinigame = controller.gameObject;
        controller.StartMinigame(
            config,
            player,
            () =>
            {
                onMinigameComplete?.Invoke();
                CleanupMinigame();
            },
            () =>
            {
                onMinigameFail?.Invoke();
                CleanupMinigame();
            });
    }

    private void CleanupMinigame()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }
    }
}
