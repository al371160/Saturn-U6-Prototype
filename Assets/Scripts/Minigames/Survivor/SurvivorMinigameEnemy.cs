using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivorMinigameEnemy : MonoBehaviour, ISurvivorDamageable, ISurvivorContactDamageSource
{
    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private float health;
    private float moveSpeed;
    private float contactDamage;
    private int xpReward;
    private LayerMask groundMask;
    private float groundSnapRayHeight;
    private float groundHeightOffset;

    public float ContactDamage => contactDamage;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        float maxHealth,
        float speed,
        float damage,
        int xpDrop,
        LayerMask groundLayerMask,
        float groundSnapHeight,
        float groundOffset)
    {
        controller = owner;
        playerTarget = target;
        health = maxHealth;
        moveSpeed = speed;
        contactDamage = damage;
        xpReward = xpDrop;
        groundMask = groundLayerMask;
        groundSnapRayHeight = groundSnapHeight;
        groundHeightOffset = groundOffset;
    }

    private void Update()
    {
        if (playerTarget == null || controller == null || !controller.IsRunning || controller.IsPaused)
            return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        Vector3 nextPosition = transform.position;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            nextPosition += toPlayer.normalized * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        transform.position = SurvivorGroundUtility.SnapToGround(nextPosition, groundMask, groundSnapRayHeight, groundHeightOffset);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Die();
    }

    private void OnCollisionStay(Collision collision)
    {
        SurvivorMinigamePlayer player = collision.collider.GetComponent<SurvivorMinigamePlayer>();
        if (player != null)
            player.TakeContactDamage(contactDamage);
    }

    private void Die()
    {
        int bonusXP = controller?.MinigamePlayer != null ? controller.MinigamePlayer.BonusXPPerKill : 0;
        controller?.SpawnXPGem(transform.position, xpReward + bonusXP);
        controller?.RegisterKill();
        Destroy(gameObject);
    }
}
