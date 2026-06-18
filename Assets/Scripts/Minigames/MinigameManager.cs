using UnityEngine;
using System;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance;

    [Header("Minigame References (Assigned in Scene)")]
    public QuickTimeMinigame quickTimeMinigame;

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
        else
        {
            Debug.LogWarning("Unsupported minigame type or no minigame required.");
            onMinigameComplete?.Invoke(); // Instantly collect if no minigame is required
        }
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
