using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manifest Docks' crane interactable — pulling the lever drops a guaranteed high-value Scope
/// pickup near the crane after a short delay, reusing SurvivorScopePickup rather than a new loot
/// pathway. One-shot per round: a deliberate, delayed, noisy pull rather than free loot.
/// </summary>
public class SurvivorCraneLoot : MonoBehaviour
{
    public float interactRange = 4f;
    public KeyCode interactKey = KeyCode.E;
    public float dropDelay = 1.5f;
    public Vector3 dropOffset = new Vector3(0f, -12f, 5f);

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private bool used;

    private void Start()
    {
        controller = FindFirstObjectByType<SurvivorMinigameController>();
    }

    private void Update()
    {
        if (used || controller == null || !controller.IsRunning || controller.IsPaused)
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
        {
            used = true;
            StartCoroutine(DropRoutine());
        }
    }

    private IEnumerator DropRoutine()
    {
        yield return new WaitForSeconds(dropDelay);
        SpawnGuaranteedScope();
    }

    private void SpawnGuaranteedScope()
    {
        if (controller == null || controller.config == null || controller.config.availableBuffs == null || playerTarget == null)
            return;

        List<SurvivorBuffDataSO> scopePool = new List<SurvivorBuffDataSO>();
        foreach (SurvivorBuffDataSO buff in controller.config.availableBuffs)
        {
            if (buff != null && buff.buffType == SurvivorBuffType.CameraZoom)
                scopePool.Add(buff);
        }

        if (scopePool.Count == 0)
            return;

        // Bias toward the highest tier authored (Scope tiers are appended 2x/4x/8x/15x in order) —
        // the gold-container drop should feel like the best pull, not a random one.
        SurvivorBuffDataSO chosen = scopePool[scopePool.Count - 1];

        GameObject pickupObject = new GameObject("CraneScopePickup");
        pickupObject.transform.position = transform.position + dropOffset;

        SphereCollider col = pickupObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;

        SurvivorScopePickup pickup = pickupObject.AddComponent<SurvivorScopePickup>();
        pickup.Initialize(controller, playerTarget, chosen, 8f);

        SurvivorCombatFX.Shake(1f);
    }
}
