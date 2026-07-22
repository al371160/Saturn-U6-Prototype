using UnityEngine;

/// <summary>
/// Bits (currency) pickup — magnets/collects exactly like SurvivorXPGem, but on collect grants
/// Bits to the overworld InventoryManager instead of run XP. Bits survive the run (unlike XP/weapons),
/// so this routes straight into InventoryManager.AddItem the same way Item.cs/InventoryEventHelper do.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorCoinPickup : MonoBehaviour
{
    private const float CollectDistance = 0.4f;
    private const string BitItemName = "Bit";

    private static InventoryManager cachedInventoryManager;
    private static ItemSO cachedBitItem;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private int bitValue;
    private float magnetRadius;
    private float moveSpeed = 9f;

    public void Initialize(SurvivorMinigameController owner, Transform target, int value, float pickupMagnetRadius)
    {
        controller = owner;
        playerTarget = target;
        bitValue = Mathf.Max(1, value);
        magnetRadius = pickupMagnetRadius;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || playerTarget == null)
            return;

        if (controller.IsPaused && !controller.IsUpgradeMenuOpen)
            return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance <= CollectDistance * CollectDistance)
        {
            Collect();
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }

    private void Collect()
    {
        GrantBits(bitValue);
        SurvivorAudio.PlayBuffPickup();
        Destroy(gameObject);
    }

    private static void GrantBits(int amount)
    {
        if (amount <= 0)
            return;

        InventoryManager inventoryManager = ResolveInventoryManager();
        if (inventoryManager == null)
        {
            Debug.LogWarning("SurvivorCoinPickup: no InventoryManager found — Bits were not granted.");
            return;
        }

        ItemSO bitItem = ResolveBitItem(inventoryManager);
        string description = bitItem != null && !string.IsNullOrEmpty(bitItem.description)
            ? bitItem.description
            : "Bits — a valuable currency picked up in the field.";
        Sprite icon = bitItem != null ? bitItem.icon : null;

        inventoryManager.AddItem(BitItemName, amount, icon, description);
    }

    private static InventoryManager ResolveInventoryManager()
    {
        if (cachedInventoryManager == null)
            cachedInventoryManager = Object.FindFirstObjectByType<InventoryManager>();

        return cachedInventoryManager;
    }

    private static ItemSO ResolveBitItem(InventoryManager inventoryManager)
    {
        if (cachedBitItem != null)
            return cachedBitItem;

        cachedBitItem = inventoryManager.GetItemSO(BitItemName);
        return cachedBitItem;
    }
}
