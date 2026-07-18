using UnityEngine;

/// <summary>Magnet-following pickup that grants a random Scope buff on collect — dropped by
/// SurvivorScopeStructure, mirrors SurvivorXPGem's movement/collection pattern.</summary>
[RequireComponent(typeof(Collider))]
public class SurvivorScopePickup : MonoBehaviour
{
    private const float CollectDistance = 0.6f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorBuffDataSO scopeBuff;
    private float magnetRadius;
    private float moveSpeed = 7f;

    public void Initialize(SurvivorMinigameController owner, Transform target, SurvivorBuffDataSO buff, float pickupMagnetRadius)
    {
        controller = owner;
        playerTarget = target;
        scopeBuff = buff;
        magnetRadius = pickupMagnetRadius;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || playerTarget == null)
            return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance <= CollectDistance * CollectDistance)
        {
            scopeBuff?.Apply(controller);
            Destroy(gameObject);
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }
}
