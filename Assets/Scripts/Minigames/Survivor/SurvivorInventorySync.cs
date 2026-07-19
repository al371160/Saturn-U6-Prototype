using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mirrors the Survivor run loadout (weapons + buffs) into InventoryCanvas ItemSlots so they
/// appear alongside overworld items like Power Core. Combat ownership stays on
/// SurvivorWeaponManager; this is a one-way display sync cleared when the run ends.
/// </summary>
public class SurvivorInventorySync : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private InventoryManager inventoryManager;
    private readonly HashSet<string> syncedNames = new HashSet<string>();

    public void Initialize(SurvivorMinigameController owner)
    {
        controller = owner;
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
            Debug.LogWarning("SurvivorInventorySync: no InventoryManager found — loadout will not appear in InventoryCanvas.");
    }

    public void Refresh()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager == null)
                return;
        }

        // Drop previous run entries, then rewrite from live combat state.
        inventoryManager.RemoveItems(syncedNames);
        syncedNames.Clear();

        if (controller?.WeaponManager != null)
        {
            foreach (SurvivorWeaponBehavior weapon in controller.WeaponManager.EquippedWeapons)
            {
                if (weapon?.Data == null)
                    continue;

                string name = weapon.Data.displayName;
                string description = string.IsNullOrEmpty(weapon.Data.description)
                    ? $"Survivor weapon — Lv.{weapon.StarLevel}"
                    : $"{weapon.Data.description}\nLv.{weapon.StarLevel}/{weapon.Data.MaxStar}";

                if (inventoryManager.UpsertItemQuiet(name, weapon.StarLevel, weapon.Data.icon, description))
                    syncedNames.Add(name);
            }
        }

        if (controller != null)
        {
            foreach (KeyValuePair<SurvivorBuffDataSO, int> entry in controller.AcquiredBuffs)
            {
                if (entry.Key == null || entry.Value <= 0)
                    continue;

                string name = entry.Key.displayName;
                string description = string.IsNullOrEmpty(entry.Key.description)
                    ? $"Survivor buff — x{entry.Value}"
                    : $"{entry.Key.description}\nx{entry.Value}";

                if (inventoryManager.UpsertItemQuiet(name, entry.Value, entry.Key.icon, description))
                    syncedNames.Add(name);
            }
        }
    }

    public void ClearSynced()
    {
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>();

        if (inventoryManager != null && syncedNames.Count > 0)
            inventoryManager.RemoveItems(syncedNames);

        syncedNames.Clear();
    }
}
