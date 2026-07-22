using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class SurvivorMinigameEnemy : MonoBehaviour, ISurvivorDamageable, ISurvivorContactDamageSource, ISurvivorStatusTarget
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
    private float groundSnapInterval;
    private float groundSnapTimer;
    private float cachedGroundY;
    private Vector3 knockbackVelocity;
    private float slowMultiplier = 1f;
    private float verticalVelocity;
    private float airborneHeight;
    private bool wasAirborne;
    private float squashTimer;
    private float hitPunchTimer;
    private Vector3 baseScale = Vector3.one;
    private float jigglePhaseOffset;
    private NavMeshAgent agent;
    private bool useNavMesh;
    private SurvivorEnemyArchetype archetype = SurvivorEnemyArchetype.Chaser;
    private float behaviorTimer;
    private float orbitAngle;
    private float shootCooldown;
    private bool isLunging;
    private Vector3 lungeVelocity;
    private Vector3 spawnAnchor;

    private const float KnockbackGravity = -30f;
    private const float KnockbackLaunchMultiplier = 1.5f;
    private const float JiggleAmplitude = 0.12f;
    private const float JiggleSpeed = 6f;
    private const float LandingSquashAmount = 0.35f;
    private const float LandingSquashRecoverySpeed = 10f;
    private const float HitPunchAmount = 0.45f;
    private const float HitPunchRecoverySpeed = 14f;

    private const float EliteStatMultiplier = 2.5f;
    private const float EliteContactDamageMultiplier = 1.5f;
    private const float EliteScaleMultiplier = 1.6f;
    private static readonly Color EliteColor = new Color(0.85f, 0.15f, 0.75f);

    public bool IsElite { get; private set; }
    public SurvivorEnemyArchetype Archetype => archetype;
    public float ContactDamage => contactDamage;

    /// <summary>Bumps stats/size/color up — called by the spawner right after Initialize() based
    /// on SurvivorMinigameController.enemyEliteness.</summary>
    public void MakeElite()
    {
        IsElite = true;
        health *= EliteStatMultiplier;
        contactDamage *= EliteContactDamageMultiplier;
        xpReward = Mathf.RoundToInt(xpReward * EliteStatMultiplier);

        baseScale *= EliteScaleMultiplier;
        transform.localScale = baseScale;

        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
            enemyRenderer.material.color = EliteColor;

        if (agent != null)
            agent.radius = Mathf.Max(0.35f, agent.radius * EliteScaleMultiplier);
    }

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        float maxHealth,
        float speed,
        float damage,
        int xpDrop,
        LayerMask groundLayerMask,
        float groundSnapHeight,
        float groundOffset,
        float snapInterval,
        SurvivorEnemyArchetype enemyArchetype = SurvivorEnemyArchetype.Chaser)
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
        groundSnapInterval = Mathf.Max(0.05f, snapInterval);
        archetype = enemyArchetype;
        spawnAnchor = transform.position;
        behaviorTimer = Random.Range(0.2f, 1.2f);
        shootCooldown = Random.Range(0.4f, 1.2f);
        orbitAngle = Random.Range(0f, 360f);

        ApplyArchetypeStats();
        ApplyArchetypeVisual();

        cachedGroundY = transform.position.y;
        groundSnapTimer = Random.Range(0f, groundSnapInterval);

        baseScale = transform.localScale;
        jigglePhaseOffset = Random.Range(0f, Mathf.PI * 2f);

        SetupNavMeshAgent();
    }

    private void ApplyArchetypeStats()
    {
        switch (archetype)
        {
            case SurvivorEnemyArchetype.Tank:
                health *= 2.2f;
                moveSpeed *= 0.55f;
                contactDamage *= 1.4f;
                xpReward = Mathf.RoundToInt(xpReward * 1.5f);
                break;
            case SurvivorEnemyArchetype.Dasher:
                health *= 0.8f;
                moveSpeed *= 1.35f;
                break;
            case SurvivorEnemyArchetype.Shooter:
                health *= 0.85f;
                moveSpeed *= 0.75f;
                contactDamage *= 0.6f;
                break;
            case SurvivorEnemyArchetype.Orbiter:
                health *= 0.9f;
                moveSpeed *= 1.1f;
                break;
            case SurvivorEnemyArchetype.Lunger:
                health *= 1.1f;
                moveSpeed *= 0.9f;
                contactDamage *= 1.25f;
                break;
            case SurvivorEnemyArchetype.Skirmisher:
                health *= 1.15f;
                moveSpeed *= 1.25f;
                contactDamage *= 1.1f;
                xpReward = Mathf.RoundToInt(xpReward * 1.35f);
                break;
        }
    }

    private void ApplyArchetypeVisual()
    {
        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
            enemyRenderer.material.color = SurvivorEnemyTierList.GetColor(archetype);

        switch (archetype)
        {
            case SurvivorEnemyArchetype.Tank:
                transform.localScale = new Vector3(1.35f, 2.1f, 1.35f);
                break;
            case SurvivorEnemyArchetype.Dasher:
                transform.localScale = new Vector3(0.7f, 1.5f, 0.7f);
                break;
            case SurvivorEnemyArchetype.Shooter:
                transform.localScale = new Vector3(0.85f, 1.7f, 0.85f);
                break;
            case SurvivorEnemyArchetype.Orbiter:
                transform.localScale = new Vector3(0.75f, 1.4f, 0.75f);
                break;
            case SurvivorEnemyArchetype.Lunger:
                transform.localScale = new Vector3(1.05f, 1.9f, 1.05f);
                break;
            case SurvivorEnemyArchetype.Skirmisher:
                transform.localScale = new Vector3(0.8f, 1.65f, 0.8f);
                break;
            default:
                transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
                break;
        }
    }

    private void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 1.1f;
        agent.radius = Mathf.Max(0.35f, baseScale.x * 0.35f);
        agent.height = Mathf.Max(1.2f, baseScale.y);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            useNavMesh = agent.isOnNavMesh;
        }
        else
        {
            useNavMesh = false;
            agent.enabled = false;
        }
    }

    private void Update()
    {
        if (playerTarget == null || controller == null || !controller.IsRunning || controller.IsPaused)
        {
            if (agent != null && agent.enabled)
                agent.isStopped = true;
            return;
        }

        bool airborne = airborneHeight > 0.01f || verticalVelocity > 0f || knockbackVelocity.sqrMagnitude > 0.25f || isLunging;
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        TickArchetypeBehavior(toPlayer, airborne);

        if (isLunging)
        {
            Vector3 lungePos = transform.position + lungeVelocity * Time.deltaTime;
            lungeVelocity = Vector3.Lerp(lungeVelocity, Vector3.zero, Time.deltaTime * 3f);
            if (lungeVelocity.sqrMagnitude < 0.5f)
                isLunging = false;
            transform.position = lungePos;
            UpdateSquashAndStretch();
            return;
        }

        if (useNavMesh && agent != null && agent.enabled && !airborne && UsesNavChase())
        {
            agent.isStopped = false;
            agent.speed = moveSpeed * slowMultiplier;
            Vector3 destination = ResolveChaseDestination(toPlayer);
            if ((destination - transform.position).sqrMagnitude > 0.01f)
                agent.SetDestination(destination);

            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                agent.Move(knockbackVelocity * Time.deltaTime);
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }
        }
        else
        {
            if (agent != null && agent.enabled)
                agent.isStopped = true;

            Vector3 nextPosition = transform.position;
            Vector3 move = ResolveDirectMove(toPlayer);
            if (move.sqrMagnitude > 0.01f && !airborne)
            {
                nextPosition += move.normalized * (moveSpeed * slowMultiplier * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(move);
            }

            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                nextPosition += knockbackVelocity * Time.deltaTime;
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }

            if (airborneHeight > 0f || verticalVelocity > 0f)
            {
                verticalVelocity += KnockbackGravity * Time.deltaTime;
                airborneHeight += verticalVelocity * Time.deltaTime;
                if (airborneHeight <= 0f)
                {
                    airborneHeight = 0f;
                    verticalVelocity = 0f;
                    TryRejoinNavMesh(nextPosition);
                }
            }

            groundSnapTimer -= Time.deltaTime;
            if (groundSnapTimer <= 0f)
            {
                groundSnapTimer = groundSnapInterval;
                cachedGroundY = SurvivorGroundUtility.SnapToGround(nextPosition, groundMask, groundSnapRayHeight, groundHeightOffset).y;
            }

            nextPosition.y = cachedGroundY + airborneHeight;
            transform.position = nextPosition;
        }

        UpdateSquashAndStretch();
    }

    private bool UsesNavChase()
    {
        return archetype != SurvivorEnemyArchetype.Orbiter;
    }

    private Vector3 ResolveChaseDestination(Vector3 toPlayer)
    {
        switch (archetype)
        {
            case SurvivorEnemyArchetype.Shooter:
                // Keep mid range — stop short of melee.
                if (toPlayer.sqrMagnitude < 36f)
                    return transform.position - toPlayer.normalized * 2f;
                return playerTarget.position;
            case SurvivorEnemyArchetype.Skirmisher:
                if (toPlayer.sqrMagnitude < 16f)
                    return transform.position + Quaternion.Euler(0f, 90f, 0f) * toPlayer.normalized * 4f;
                return playerTarget.position;
            default:
                return playerTarget.position;
        }
    }

    private Vector3 ResolveDirectMove(Vector3 toPlayer)
    {
        if (archetype == SurvivorEnemyArchetype.Orbiter)
        {
            orbitAngle += 90f * Time.deltaTime;
            Vector3 orbitOffset = new Vector3(Mathf.Cos(orbitAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(orbitAngle * Mathf.Deg2Rad)) * 5.5f;
            Vector3 orbitPoint = playerTarget.position + orbitOffset;
            Vector3 toOrbit = orbitPoint - transform.position;
            toOrbit.y = 0f;
            return toOrbit;
        }

        return toPlayer;
    }

    private void TickArchetypeBehavior(Vector3 toPlayer, bool airborne)
    {
        behaviorTimer -= Time.deltaTime;

        if (archetype == SurvivorEnemyArchetype.Shooter)
        {
            shootCooldown -= Time.deltaTime;
            if (shootCooldown <= 0f && toPlayer.sqrMagnitude < 220f && toPlayer.sqrMagnitude > 9f)
            {
                shootCooldown = Random.Range(1.1f, 1.8f);
                FireEnemyBolt(toPlayer.normalized);
            }
        }

        if (archetype == SurvivorEnemyArchetype.Dasher && behaviorTimer <= 0f && !airborne && toPlayer.sqrMagnitude > 4f && toPlayer.sqrMagnitude < 120f)
        {
            behaviorTimer = Random.Range(1.6f, 2.6f);
            isLunging = true;
            lungeVelocity = toPlayer.normalized * (moveSpeed * 4.5f);
            if (agent != null && agent.enabled)
                agent.isStopped = true;
        }

        if (archetype == SurvivorEnemyArchetype.Lunger && behaviorTimer <= 0f && !airborne && toPlayer.sqrMagnitude > 6f && toPlayer.sqrMagnitude < 100f)
        {
            behaviorTimer = Random.Range(2.2f, 3.4f);
            isLunging = true;
            lungeVelocity = toPlayer.normalized * (moveSpeed * 5.5f);
            if (agent != null && agent.enabled)
                agent.isStopped = true;
        }

        if (archetype == SurvivorEnemyArchetype.Skirmisher && behaviorTimer <= 0f)
        {
            behaviorTimer = Random.Range(0.8f, 1.5f);
            if (toPlayer.sqrMagnitude < 80f && Random.value < 0.45f)
            {
                isLunging = true;
                Vector3 side = Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * toPlayer.normalized;
                lungeVelocity = side * (moveSpeed * 3.8f);
            }
        }
    }

    private void FireEnemyBolt(Vector3 direction)
    {
        GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bolt.name = "SurvivorEnemyBolt";
        bolt.transform.position = transform.position + Vector3.up * 0.9f + direction * 0.6f;
        bolt.transform.localScale = Vector3.one * 0.35f;
        Object.Destroy(bolt.GetComponent<Collider>());
        Rigidbody rb = bolt.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        Renderer r = bolt.GetComponent<Renderer>();
        if (r != null)
            r.material.color = new Color(0.3f, 0.7f, 1f);

        float damage = Mathf.Max(4f, contactDamage * 0.85f);
        bolt.AddComponent<SurvivorBossProjectile>().Launch(direction, 14f, damage, 18f);
        SurvivorAudio.PlayHitForTarget(SurvivorHitAudioKind.Enemy);
    }

    private void TryRejoinNavMesh(Vector3 nearPosition)
    {
        if (agent == null)
            return;

        if (NavMesh.SamplePosition(nearPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.enabled = true;
            agent.Warp(hit.position);
            useNavMesh = agent.isOnNavMesh;
            agent.isStopped = false;
        }
    }

    private void UpdateSquashAndStretch()
    {
        bool isAirborneNow = airborneHeight > 0.01f;
        if (wasAirborne && !isAirborneNow)
            squashTimer = 1f;
        wasAirborne = isAirborneNow;

        if (squashTimer > 0f)
            squashTimer = Mathf.MoveTowards(squashTimer, 0f, Time.deltaTime * LandingSquashRecoverySpeed);

        if (hitPunchTimer > 0f)
            hitPunchTimer = Mathf.MoveTowards(hitPunchTimer, 0f, Time.deltaTime * HitPunchRecoverySpeed);

        float idleJiggle = Mathf.Sin(Time.time * JiggleSpeed + jigglePhaseOffset) * JiggleAmplitude;
        float stretchFromAir = isAirborneNow ? Mathf.Clamp(Mathf.Abs(verticalVelocity), 0f, 8f) * 0.02f : 0f;
        float squashFromLanding = -squashTimer * LandingSquashAmount;
        float hitPunch = hitPunchTimer * HitPunchAmount * Mathf.Sin(hitPunchTimer * Mathf.PI * 6f);

        float yScaleOffset = idleJiggle + stretchFromAir + squashFromLanding + hitPunch;
        float xzScaleOffset = -yScaleOffset * 0.5f;

        transform.localScale = new Vector3(
            baseScale.x * (1f + xzScaleOffset),
            baseScale.y * (1f + yScaleOffset),
            baseScale.z * (1f + xzScaleOffset));
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        hitPunchTimer = 1f;
        if (health <= 0f)
            Die();
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (direction.sqrMagnitude > 0.0001f)
            knockbackVelocity += direction.normalized * force;

        verticalVelocity += force * KnockbackLaunchMultiplier;
        if (agent != null && agent.enabled)
            agent.isStopped = true;
    }

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = Mathf.Clamp01(multiplier);
    }

    private void OnCollisionStay(Collision collision)
    {
        SurvivorMinigamePlayer player = collision.collider.GetComponent<SurvivorMinigamePlayer>();
        if (player != null)
            player.TakeContactDamage(contactDamage);
    }

    private void Die()
    {
        SurvivorHitAudioKind kind = IsElite ? SurvivorHitAudioKind.Elite : SurvivorHitAudioKind.Enemy;
        SurvivorAudio.PlayDestroyForTarget(kind, transform.position, useProximity: true);
        int bonusXP = controller?.MinigamePlayer != null ? controller.MinigamePlayer.BonusXPPerKill : 0;
        controller?.SpawnXPGem(transform.position, xpReward + bonusXP);
        controller?.RegisterKill();
        Destroy(gameObject);
    }
}
