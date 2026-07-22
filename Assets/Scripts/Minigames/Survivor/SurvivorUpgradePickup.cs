using UnityEngine;

/// <summary>
/// Boss-drop buff pickup — magnet-follows and grants a random stat buff on collect, mirroring
/// SurvivorScopePickup's movement/collection pattern. Unlike level-up choices, this is the one
/// place a player can still receive a buff (including a Scope) as ground loot — reserved for
/// boss kills so it reads as a meaningful reward rather than everyday crate filler.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorUpgradePickup : MonoBehaviour
{
    private const float CollectDistance = 0.6f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorBuffDataSO buff;
    private float magnetRadius;
    private float moveSpeed = 7f;

    public void Initialize(SurvivorMinigameController owner, Transform target, SurvivorBuffDataSO buffData, float pickupMagnetRadius)
    {
        controller = owner;
        playerTarget = target;
        buff = buffData;
        magnetRadius = pickupMagnetRadius;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || playerTarget == null || buff == null)
            return;

        if (controller.IsPaused && !controller.IsUpgradeMenuOpen)
            return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance <= CollectDistance * CollectDistance)
        {
            buff.Apply(controller);
            SurvivorAudio.PlayBuffPickup();
            Destroy(gameObject);
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }
}
