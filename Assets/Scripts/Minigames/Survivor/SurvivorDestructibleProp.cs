using UnityEngine;

/// <summary>
/// Optional-destroyable static blocker (bush, rock, etc. from SurvivorWorldPropFactory) — plain
/// ISurvivorDamageable with no drop table, unlike SurvivorLootCrate/SurvivorScopeStructure. Just
/// gives weapons something satisfying to break in the open world.
/// </summary>
public class SurvivorDestructibleProp : MonoBehaviour, ISurvivorDamageable
{
    private float health = 20f;

    public void Initialize(float maxHealth)
    {
        health = Mathf.Max(1f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        SurvivorAudio.PlayDestroyForTarget(gameObject);
        Destroy(gameObject);
    }
}
