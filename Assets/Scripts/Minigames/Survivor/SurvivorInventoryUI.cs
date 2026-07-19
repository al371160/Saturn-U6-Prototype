using UnityEngine;

/// <summary>
/// Retired — Survivor weapons/buffs now sync into InventoryCanvas via SurvivorInventorySync.
/// Kept as a no-op stub so existing scene references do not break; safe to remove from scenes.
/// </summary>
public class SurvivorInventoryUI : MonoBehaviour
{
    public SurvivorMinigameController controller;
    public KeyCode toggleKey = KeyCode.Tab;
    public TMPro.TMP_FontAsset font;

    private void Awake()
    {
        enabled = false;
    }
}
