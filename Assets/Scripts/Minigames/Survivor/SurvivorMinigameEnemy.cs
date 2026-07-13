using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivorMinigameEnemy : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private float health;
    private float moveSpeed;
    private float contactDamage;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        float maxHealth,
        float speed,
        float damage)
    {
        controller = owner;
        playerTarget = target;
        health = maxHealth;
        moveSpeed = speed;
        contactDamage = damage;
    }

    private void Update()
    {
        if (playerTarget == null || controller == null || !controller.IsRunning)
            return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(toPlayer);
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
        controller?.RegisterKill();
        Destroy(gameObject);
    }
}
