using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivorBossEnemy : MonoBehaviour, ISurvivorDamageable
{
    private enum BossState
    {
        Chase,
        Telegraph,
        Attack,
        Recover
    }

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private float health;
    private float moveSpeed;
    private float attackRange;
    private float attackDamage;
    private float attackRadius;
    private float telegraphDuration;
    private float attackDuration;
    private float recoverDuration;
    private LayerMask groundMask;
    private float groundSnapRayHeight;
    private float groundHeightOffset;

    private BossState state;
    private float stateTimer;
    private Vector3 attackStartPosition;
    private Vector3 attackTargetPosition;
    private Renderer bossRenderer;
    private Color baseColor;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        float maxHealth,
        float speed,
        float range,
        float damage,
        float radius,
        float telegraph,
        float attackTime,
        float recover,
        LayerMask groundLayerMask,
        float groundSnapHeight,
        float groundOffset)
    {
        controller = owner;
        playerTarget = target;
        health = maxHealth;
        moveSpeed = speed;
        attackRange = range;
        attackDamage = damage;
        attackRadius = radius;
        telegraphDuration = telegraph;
        attackDuration = attackTime;
        recoverDuration = recover;
        groundMask = groundLayerMask;
        groundSnapRayHeight = groundSnapHeight;
        groundHeightOffset = groundOffset;

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null)
            baseColor = bossRenderer.material.color;

        state = BossState.Chase;
        stateTimer = 0f;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || playerTarget == null)
            return;

        switch (state)
        {
            case BossState.Chase:
                UpdateChase();
                break;
            case BossState.Telegraph:
                UpdateTelegraph();
                break;
            case BossState.Attack:
                UpdateAttack();
                break;
            case BossState.Recover:
                UpdateRecover();
                break;
        }
    }

    private void UpdateChase()
    {
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude <= attackRange)
        {
            EnterTelegraph();
            return;
        }

        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Vector3 nextPosition = transform.position + toPlayer.normalized * (moveSpeed * Time.deltaTime);
            transform.position = SurvivorGroundUtility.SnapToGround(nextPosition, groundMask, groundSnapRayHeight, groundHeightOffset);
            transform.rotation = Quaternion.LookRotation(toPlayer);
        }
    }

    private void EnterTelegraph()
    {
        state = BossState.Telegraph;
        stateTimer = telegraphDuration;
        attackTargetPosition = playerTarget.position;

        if (bossRenderer != null)
            bossRenderer.material.color = Color.red;
    }

    private void UpdateTelegraph()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;

        state = BossState.Attack;
        stateTimer = attackDuration;
        attackStartPosition = transform.position;
    }

    private void UpdateAttack()
    {
        stateTimer -= Time.deltaTime;
        float t = attackDuration <= 0f ? 1f : 1f - Mathf.Clamp01(stateTimer / attackDuration);
        Vector3 lungePosition = Vector3.Lerp(attackStartPosition, attackTargetPosition, t);
        transform.position = SurvivorGroundUtility.SnapToGround(lungePosition, groundMask, groundSnapRayHeight, groundHeightOffset);

        if (stateTimer <= 0f)
        {
            DamagePlayerAtCurrentPosition();
            EnterRecover();
        }
    }

    private void DamagePlayerAtCurrentPosition()
    {
        if (playerTarget == null)
            return;

        SurvivorMinigamePlayer player = playerTarget.GetComponent<SurvivorMinigamePlayer>();
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= attackRadius)
            player.TakeContactDamage(attackDamage);
    }

    private void EnterRecover()
    {
        state = BossState.Recover;
        stateTimer = recoverDuration;

        if (bossRenderer != null)
            bossRenderer.material.color = baseColor;
    }

    private void UpdateRecover()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            state = BossState.Chase;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        controller?.RegisterBossDefeated();
        Destroy(gameObject);
    }
}
