using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivorXPGem : MonoBehaviour
{
    private const float CollectDistance = 0.4f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private int xpValue;
    private float magnetRadius;
    private float moveSpeed = 9f;

    public void Initialize(SurvivorMinigameController owner, Transform target, int value, float pickupMagnetRadius)
    {
        controller = owner;
        playerTarget = target;
        xpValue = value;
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
            Collect();
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }

    private void Collect()
    {
        controller.Progression?.AddXP(xpValue);
        Destroy(gameObject);
    }
}
