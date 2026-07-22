using UnityEngine;

public enum SurvivorConsumableType
{
    FullHeal,
    Nuke
}

/// <summary>
/// Ground-only consumable pickup (Full Heal, Nuke) — magnet-follows and auto-applies on proximity,
/// mirroring SurvivorScopePickup's movement/collection pattern. These are never offered on the
/// level-up screen anymore; SurvivorMinigameController scatters a few directly onto the map instead.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorConsumablePickup : MonoBehaviour
{
    private const float CollectDistance = 0.6f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorConsumableType consumableType;
    private float magnetRadius;
    private float moveSpeed = 7f;

    public void Initialize(SurvivorMinigameController owner, Transform target, SurvivorConsumableType type, float pickupMagnetRadius)
    {
        controller = owner;
        playerTarget = target;
        consumableType = type;
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
            Apply();
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }

    private void Apply()
    {
        switch (consumableType)
        {
            case SurvivorConsumableType.FullHeal:
                if (controller.MinigamePlayer != null)
                    controller.MinigamePlayer.Heal(controller.MinigamePlayer.MaxHealth);
                break;
            case SurvivorConsumableType.Nuke:
                controller.NukeAllEnemies(200f);
                break;
        }

        SurvivorAudio.PlayBuffPickup();
        Destroy(gameObject);
    }
}
