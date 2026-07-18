using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Halfway Hitch's interactable — rerolls the player's first equipped weapon for a new random one
/// from the level's available pool when the player is nearby and presses the interact key. Reuses
/// SurvivorWeaponManager.EvolveWeapon rather than any new equip pathway.
/// </summary>
public class SurvivorForgeExchange : MonoBehaviour
{
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    private SurvivorMinigameController controller;
    private Transform playerTarget;

    private void Start()
    {
        controller = FindFirstObjectByType<SurvivorMinigameController>();
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused)
            return;

        if (playerTarget == null)
        {
            SurvivorMinigamePlayer player = FindFirstObjectByType<SurvivorMinigamePlayer>();
            if (player == null)
                return;

            playerTarget = player.transform;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > interactRange)
            return;

        if (Input.GetKeyDown(interactKey))
            RerollWeapon();
    }

    private void RerollWeapon()
    {
        if (controller.WeaponManager == null || controller.config == null || controller.config.availableWeapons == null)
            return;

        List<SurvivorWeaponBehavior> equipped = new List<SurvivorWeaponBehavior>(controller.WeaponManager.EquippedWeapons);
        if (equipped.Count == 0)
            return;

        SurvivorWeaponBehavior current = equipped[0];
        SurvivorWeaponDataSO newWeapon = controller.config.availableWeapons[Random.Range(0, controller.config.availableWeapons.Length)];
        if (newWeapon == null)
            return;

        controller.WeaponManager.EvolveWeapon(current.Data, newWeapon);
    }
}
