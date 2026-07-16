using UnityEngine;

public class SurvivorMinigamePlayer : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private SurvivorMinigameConfig config;
    private PlayerController playerController;
    private float currentHealth;
    private float contactCooldown;
    private float maxHealthBonus;
    private float baseWalkSpeed;
    private float baseSprintSpeed;
    private float baseStaminaRegenRate;
    private float moveSpeedMultiplier = 1f;
    private float staminaRegenMultiplier = 1f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => (config != null ? config.playerMaxHealth : 100f) + maxHealthBonus;
    public float HealthRegenPerSecond { get; private set; }
    public float DamageReductionPercent { get; private set; }
    public float MagnetRadiusBonus { get; private set; }
    public int BonusXPPerKill { get; private set; }

    public void Initialize(SurvivorMinigameController owner, SurvivorMinigameConfig levelConfig)
    {
        controller = owner;
        config = levelConfig;
        playerController = GetComponent<PlayerController>();

        if (playerController != null)
        {
            baseWalkSpeed = playerController.walkSpeed;
            baseSprintSpeed = playerController.sprintSpeed;
            baseStaminaRegenRate = playerController.staminaRegenRate;
        }

        maxHealthBonus = 0f;
        moveSpeedMultiplier = 1f;
        staminaRegenMultiplier = 1f;
        HealthRegenPerSecond = 0f;
        DamageReductionPercent = 0f;
        MagnetRadiusBonus = 0f;
        BonusXPPerKill = 0;
        currentHealth = MaxHealth;
        contactCooldown = 0f;
    }

    public void ApplyMaxHealthBonus(float bonus)
    {
        maxHealthBonus += bonus;
        currentHealth = MaxHealth;
        controller?.RefreshHud();
    }

    public void ApplyMoveSpeedBonus(float percent)
    {
        moveSpeedMultiplier += percent;

        if (playerController != null)
        {
            playerController.walkSpeed = baseWalkSpeed * moveSpeedMultiplier;
            playerController.sprintSpeed = baseSprintSpeed * moveSpeedMultiplier;
        }
    }

    public void ApplyMaxStaminaBonus(float amount)
    {
        if (playerController != null)
            playerController.maxStamina += amount;
    }

    public void ApplyStaminaRegenBonus(float percent)
    {
        staminaRegenMultiplier += percent;

        if (playerController != null)
            playerController.staminaRegenRate = baseStaminaRegenRate * staminaRegenMultiplier;
    }

    public void ApplyHealthRegenBonus(float perSecond)
    {
        HealthRegenPerSecond += perSecond;
    }

    public void ApplyDamageReductionBonus(float percent)
    {
        DamageReductionPercent = Mathf.Clamp(DamageReductionPercent + percent, 0f, 0.9f);
    }

    public void ApplyMagnetRadiusBonus(float amount)
    {
        MagnetRadiusBonus += amount;
    }

    public void ApplyBonusXPPerKill(int amount)
    {
        BonusXPPerKill += amount;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        controller?.RefreshHud();
    }

    public void GrantInvulnerability(float seconds)
    {
        contactCooldown = Mathf.Max(contactCooldown, seconds);
    }

    private void Update()
    {
        if (contactCooldown > 0f)
            contactCooldown -= Time.deltaTime;

        if (HealthRegenPerSecond > 0f && currentHealth < MaxHealth && controller != null && controller.IsRunning && !controller.IsPaused)
            Heal(HealthRegenPerSecond * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused)
            return;

        ISurvivorContactDamageSource source = hit.gameObject.GetComponent<ISurvivorContactDamageSource>();
        if (source != null)
            TakeContactDamage(source.ContactDamage);
    }

    public void TakeContactDamage(float amount)
    {
        if (controller == null || !controller.IsRunning || config == null)
            return;

        if (contactCooldown > 0f)
            return;

        contactCooldown = config.contactDamageCooldown;
        currentHealth -= amount * (1f - DamageReductionPercent);
        controller.RefreshHud();

        if (currentHealth <= 0f)
            controller.HandlePlayerDefeated();
    }
}
