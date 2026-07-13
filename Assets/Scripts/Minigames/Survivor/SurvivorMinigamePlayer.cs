using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurvivorMinigamePlayer : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private SurvivorMinigameConfig config;
    private float currentHealth;
    private float contactCooldown;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => config != null ? config.playerMaxHealth : 100f;

    public void Initialize(SurvivorMinigameController owner, SurvivorMinigameConfig levelConfig)
    {
        controller = owner;
        config = levelConfig;
        currentHealth = config.playerMaxHealth;
        contactCooldown = 0f;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || config == null)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(horizontal, 0f, vertical);

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        Vector3 nextPosition = transform.position + move * (config.playerMoveSpeed * Time.deltaTime);
        nextPosition = controller.ClampToArena(nextPosition);
        transform.position = nextPosition;

        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(move);

        if (contactCooldown > 0f)
            contactCooldown -= Time.deltaTime;
    }

    public void TakeContactDamage(float amount)
    {
        if (controller == null || !controller.IsRunning || config == null)
            return;

        if (contactCooldown > 0f)
            return;

        contactCooldown = config.contactDamageCooldown;
        currentHealth -= amount;
        controller.RefreshHud();

        if (currentHealth <= 0f)
            controller.EndMinigame(false);
    }
}
