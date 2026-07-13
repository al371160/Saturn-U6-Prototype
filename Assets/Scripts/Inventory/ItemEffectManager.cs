using UnityEngine;
using System.Collections.Generic;

public class ItemEffectManager : MonoBehaviour
{
    public static ItemEffectManager Instance;
    public InventoryManager inventoryManager;
    public GameObject player;

    [Header("Equipable Item References")]
    public GameObject shovelObject;
    public GameObject axeObject;
    public GameObject suitObject;

    private ItemSO equippedSuit = null;

    private Dictionary<ItemSO.StatToChange, GameObject> equipables;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        equipables = new Dictionary<ItemSO.StatToChange, GameObject>
        {
            { ItemSO.StatToChange.shovelEquip, shovelObject },
            { ItemSO.StatToChange.axeEquip, axeObject },
            { ItemSO.StatToChange.suitEquip, suitObject },
        };
    }

    public void ApplyEffect(ItemSO item)
    {
        switch (item.statToChange)
        {
            case ItemSO.StatToChange.maxStamina:
                if (player != null)
                    player.GetComponent<PlayerController>().ChangeMaxStamina(item.amountToChangeStat);
                break;

            // Handle other effects...
        }
    }

    public void EquipEffect(ItemSO item)
    {
        if (!equipables.TryGetValue(item.statToChange, out GameObject equipObject) || equipObject == null)
        {
            Debug.LogWarning($"No equipable object mapped for: {item.statToChange}");
            return;
        }

        bool isCurrentlyEquipped = equipObject.activeSelf;

        // Trigger animation + close inventory
        player.GetComponent<PlayerController>().playerAnim.SetTrigger("equipping");
        inventoryManager.CloseInventory();

        if (!isCurrentlyEquipped)
        {
            // Unequip all other items
            foreach (var kvp in equipables)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
        }

        // Toggle selected item (re-equipping same item turns it off)
        bool nowEquipped = !isCurrentlyEquipped;
        equipObject.SetActive(nowEquipped);
        Debug.Log(nowEquipped ? $"Equipping {item.statToChange}" : $"Unequipping {item.statToChange}");

        // Apply or remove suit stats
        if (item.statToChange == ItemSO.StatToChange.suitEquip)
        {
            PlayerController pc = player.GetComponent<PlayerController>();

            if (nowEquipped)
            {
                equippedSuit = item;
                OxygenSystem.Instance?.ApplySuit(item.suitMaxAltitude);
                if (pc != null) pc.sprintSpeed = item.suitMaxSpeed;
            }
            else
            {
                equippedSuit = null;
                OxygenSystem.Instance?.RemoveSuit();
                if (pc != null) pc.sprintSpeed = pc.walkSpeed; // reset to default
            }
        }
    }
}
