using UnityEngine;

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
        float snapInterval)
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

        // Spawn position already arrived pre-snapped from the spawner; stagger the first
        // re-snap so hundreds of enemies don't all raycast on the same frame.
        cachedGroundY = transform.position.y;
        groundSnapTimer = Random.Range(0f, groundSnapInterval);

        baseScale = transform.localScale;
        jigglePhaseOffset = Random.Range(0f, Mathf.PI * 2f);
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
            nextPosition += toPlayer.normalized * (moveSpeed * slowMultiplier * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(toPlayer);
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

        // Knockback lift: fall back to the ground under gravity once launched.
        if (airborneHeight > 0f || verticalVelocity > 0f)
        {
            verticalVelocity += KnockbackGravity * Time.deltaTime;
            airborneHeight += verticalVelocity * Time.deltaTime;
            if (airborneHeight <= 0f)
            {
                airborneHeight = 0f;
                verticalVelocity = 0f;
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

        UpdateSquashAndStretch();
    }

    /// <summary>
    /// Simple procedural squash/stretch: a subtle idle jiggle at all times, an elongation while
    /// airborne from a knockback launch, and a compression pulse on landing. Phase-offset per
    /// instance so hundreds of enemies don't all pulse in sync.
    /// </summary>
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
        // Hit punch alternates sign at high speed so it reads as a shudder rather than a single squash.
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
        int bonusXP = controller?.MinigamePlayer != null ? controller.MinigamePlayer.BonusXPPerKill : 0;
        controller?.SpawnXPGem(transform.position, xpReward + bonusXP);
        controller?.RegisterKill();
        Destroy(gameObject);
    }
}
