using UnityEngine;

public class InventoryEventHelper : MonoBehaviour
{
    public InventoryManager inventoryManager;

    // Give item from a prefab with customizable quantity override
    public void GiveItemFromPrefab(GameObject itemPrefab, int customQuantity)
    {
        Item item = itemPrefab.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning("No Item component found on the given prefab.");
            return;
        }

        ItemSO itemData = item.itemData;

        if (itemData == null)
        {
            Debug.LogWarning("Item prefab does not have an ItemSO assigned.");
            return;
        }

        int quantityToAdd = customQuantity > 0 ? customQuantity : 1; // Default to adding 1 if no custom quantity is specified

        int leftover = inventoryManager.AddItem(itemData.itemName, quantityToAdd, itemData.icon, itemData.description);

        if (leftover > 0)
        {
            Debug.Log("Could not add all items, leftover: " + leftover);
        }
    }

    // Give item by name using itemSOs, with a customizable quantity
    public void GiveItemByName(string itemName, int quantity)
    {
        foreach (var itemSO in inventoryManager.itemSOs)
        {
            if (itemSO.itemName == itemName)
            {
                inventoryManager.AddItem(itemSO.itemName, quantity, itemSO.icon, itemSO.description);
                return;
            }
        }

        Debug.LogWarning("Item not found in InventoryManager.itemSOs: " + itemName);
    }

    // Remove an item from inventory with customizable quantity
    public void TakeItem(string itemName, int quantity)
    {
        foreach (var slot in inventoryManager.itemSlot)
        {
            if (slot.itemName == itemName && slot.quantity > 0)
            {
                int removeAmount = Mathf.Min(quantity, slot.quantity);
                slot.RemoveItem(removeAmount);
                return;
            }
        }

        Debug.LogWarning("Item not found or not enough quantity to remove: " + itemName);
    }

    // Use an item
    public void UseItem(string itemName)
    {
        inventoryManager.UseItem(itemName);
    }
}
