using UnityEngine;

/// <summary>
/// Generic boss executor — reads a SurvivorBossDataSO's attack pools rather than hardcoding a
/// single attack, so any number of distinct bosses can be authored as data assets. Keeps the
/// original Chase/Telegraph/Attack/Recover state machine (it was sound); only attack selection
/// and execution are generalized.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorBossEnemy : MonoBehaviour, ISurvivorDamageable, ISurvivorStatusTarget
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
    private SurvivorBossDataSO data;
    private float health;
    private float currentMoveSpeed;
    private LayerMask groundMask;
    private float groundSnapRayHeight;
    private float groundHeightOffset;

    private BossState state;
    private float stateTimer;
    private int currentPhase = 1;
    private Vector3 attackStartPosition;
    private Vector3 attackTargetPosition;
    private SurvivorBossAttackDefinition activeAttack;
    private int activeAttackIndex;
    private float[] activeCooldownsRef;
    private float[] phase1Cooldowns;
    private float[] phase2Cooldowns;
    private Renderer bossRenderer;
    private Color baseColor;
    private Vector3 knockbackVelocity;
    private float slowMultiplier = 1f;

    /// <summary>Bosses are heavy — knockback and slows land, just muted, so fights stay winnable.</summary>
    private const float BossKnockbackResistance = 0.3f;
    private const float BossMinSlowMultiplier = 0.5f;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        SurvivorBossDataSO bossData,
        LayerMask groundLayerMask,
        float groundSnapHeight,
        float groundOffset)
    {
        controller = owner;
        playerTarget = target;
        data = bossData;
        health = data.maxHealth;
        currentMoveSpeed = data.moveSpeed;
        groundMask = groundLayerMask;
        groundSnapRayHeight = groundSnapHeight;
        groundHeightOffset = groundOffset;

        transform.localScale = Vector3.one * data.scale;

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null)
        {
            bossRenderer.material.color = data.bossColor;
            baseColor = data.bossColor;
        }

        phase1Cooldowns = new float[data.phase1Attacks != null ? data.phase1Attacks.Length : 0];
        phase2Cooldowns = new float[data.phase2Attacks != null ? data.phase2Attacks.Length : 0];

        currentPhase = 1;
        state = BossState.Chase;
        stateTimer = 0f;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || playerTarget == null)
            return;

        TickCooldowns(phase1Cooldowns);
        TickCooldowns(phase2Cooldowns);

        if (currentPhase == 1 && health <= data.maxHealth * data.enrageHealthFraction)
            EnterPhase2();

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

    private static void TickCooldowns(float[] cooldowns)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0f)
                cooldowns[i] -= Time.deltaTime;
        }
    }

    private void EnterPhase2()
    {
        currentPhase = 2;
        currentMoveSpeed = data.moveSpeed * data.enrageMoveSpeedMultiplier;
        StartCoroutine(EnragePulse());
    }

    private System.Collections.IEnumerator EnragePulse()
    {
        if (bossRenderer == null)
            yield break;

        for (int i = 0; i < 3; i++)
        {
            bossRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.15f);
            bossRenderer.material.color = data.bossColor;
            yield return new WaitForSeconds(0.15f);
        }
    }

    private (SurvivorBossAttackDefinition[] pool, float[] cooldowns) ActivePoolAndCooldowns()
    {
        return currentPhase == 1
            ? (data.phase1Attacks, phase1Cooldowns)
            : (data.phase2Attacks, phase2Cooldowns);
    }

    private void UpdateChase()
    {
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        var (pool, cooldowns) = ActivePoolAndCooldowns();
        float triggerRange = 2.5f;
        if (pool != null)
        {
            foreach (SurvivorBossAttackDefinition attack in pool)
                triggerRange = Mathf.Max(triggerRange, attack.range > 0f ? attack.range : attack.radius);
        }

        if (toPlayer.magnitude <= triggerRange && TrySelectAttack(pool, cooldowns, out SurvivorBossAttackDefinition selected, out int selectedIndex))
        {
            EnterTelegraph(selected, selectedIndex, cooldowns);
            return;
        }

        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Vector3 nextPosition = transform.position + toPlayer.normalized * (currentMoveSpeed * slowMultiplier * Time.deltaTime);

            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                nextPosition += knockbackVelocity * Time.deltaTime;
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }

            transform.position = SurvivorGroundUtility.SnapToGround(nextPosition, groundMask, groundSnapRayHeight, groundHeightOffset);
            transform.rotation = Quaternion.LookRotation(toPlayer);
        }
    }

    /// <summary>Weighted-random pick among attacks not currently on cooldown, so the same move
    /// doesn't repeat back-to-back — the pool + cooldown combo is what gives bosses attack variety.</summary>
    private bool TrySelectAttack(SurvivorBossAttackDefinition[] pool, float[] cooldowns, out SurvivorBossAttackDefinition selected, out int selectedIndex)
    {
        selected = default;
        selectedIndex = -1;

        if (pool == null || pool.Length == 0)
            return false;

        float totalWeight = 0f;
        for (int i = 0; i < pool.Length; i++)
        {
            if (cooldowns[i] <= 0f)
                totalWeight += Mathf.Max(0.01f, pool[i].weight);
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < pool.Length; i++)
        {
            if (cooldowns[i] > 0f)
                continue;

            cumulative += Mathf.Max(0.01f, pool[i].weight);
            if (roll <= cumulative)
            {
                selected = pool[i];
                selectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private void EnterTelegraph(SurvivorBossAttackDefinition attack, int attackIndex, float[] cooldowns)
    {
        state = BossState.Telegraph;
        activeAttack = attack;
        activeAttackIndex = attackIndex;
        activeCooldownsRef = cooldowns;
        stateTimer = attack.telegraphDuration;
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
        stateTimer = Mathf.Max(0.15f, activeAttack.telegraphDuration * 0.75f);
        attackStartPosition = transform.position;

        if (activeAttack.attackType == SurvivorBossAttackType.ProjectileVolley)
            FireProjectileVolley();
    }

    private void UpdateAttack()
    {
        float duration = Mathf.Max(0.15f, activeAttack.telegraphDuration * 0.75f);
        stateTimer -= Time.deltaTime;

        switch (activeAttack.attackType)
        {
            case SurvivorBossAttackType.Lunge:
            case SurvivorBossAttackType.ChargeSweep:
            {
                float t = 1f - Mathf.Clamp01(stateTimer / duration);
                Vector3 target = attackTargetPosition;
                if (activeAttack.attackType == SurvivorBossAttackType.ChargeSweep)
                {
                    Vector3 throughDir = (attackTargetPosition - attackStartPosition);
                    throughDir = throughDir.sqrMagnitude > 0.01f ? throughDir.normalized : transform.forward;
                    target = attackTargetPosition + throughDir * activeAttack.range;
                }

                Vector3 movePosition = Vector3.Lerp(attackStartPosition, target, t);
                transform.position = SurvivorGroundUtility.SnapToGround(movePosition, groundMask, groundSnapRayHeight, groundHeightOffset);
                DamagePlayerWithinRadius(activeAttack.radius);
                break;
            }
            case SurvivorBossAttackType.GroundSlam:
                if (stateTimer <= 0f)
                    DamagePlayerWithinRadius(activeAttack.radius);
                break;
        }

        if (stateTimer <= 0f)
            EnterRecover();
    }

    private void DamagePlayerWithinRadius(float radius)
    {
        if (playerTarget == null)
            return;

        SurvivorMinigamePlayer player = playerTarget.GetComponent<SurvivorMinigamePlayer>();
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= Mathf.Max(0.5f, radius))
            player.TakeContactDamage(activeAttack.damage);
    }

    private void FireProjectileVolley()
    {
        if (playerTarget == null)
            return;

        int count = Mathf.Max(1, activeAttack.projectileCount);
        Vector3 baseDirection = playerTarget.position - transform.position;
        baseDirection.y = 0f;
        baseDirection = baseDirection.sqrMagnitude > 0.01f ? baseDirection.normalized : transform.forward;

        float spread = count > 1 ? 20f : 0f;
        float startAngle = -spread * (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + spread * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            SpawnBossProjectile(direction);
        }
    }

    private void SpawnBossProjectile(Vector3 direction)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "SurvivorBossProjectile";
        projectileObject.transform.position = transform.position + Vector3.up * 1.2f;
        projectileObject.transform.localScale = Vector3.one * 0.6f;

        Collider col = projectileObject.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer projectileRenderer = projectileObject.GetComponent<Renderer>();
        if (projectileRenderer != null)
            projectileRenderer.material.color = data.bossColor;

        projectileObject.AddComponent<SurvivorBossProjectile>().Launch(direction, 12f, activeAttack.damage, Mathf.Max(1f, activeAttack.range));
    }

    private void EnterRecover()
    {
        state = BossState.Recover;
        stateTimer = data.recoverDuration;

        if (activeCooldownsRef != null && activeAttackIndex >= 0 && activeAttackIndex < activeCooldownsRef.Length)
            activeCooldownsRef[activeAttackIndex] = activeAttack.cooldownAfterUse;

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

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (direction.sqrMagnitude > 0.0001f)
            knockbackVelocity += direction.normalized * force * BossKnockbackResistance;
    }

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = Mathf.Clamp(multiplier, BossMinSlowMultiplier, 1f);
    }

    private void Die()
    {
        controller?.RegisterBossDefeated(this, data != null ? data.bossXPReward : 0);
        Destroy(gameObject);
    }
}
