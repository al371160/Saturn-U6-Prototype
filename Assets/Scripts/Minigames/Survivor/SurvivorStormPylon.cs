using UnityEngine;

/// <summary>One of SurvivorStormObjective's shield pylons — plain destructible, same
/// ISurvivorDamageable pattern as SurvivorScopeStructure.</summary>
[RequireComponent(typeof(Collider))]
public class SurvivorStormPylon : MonoBehaviour, ISurvivorDamageable
{
    private float health;
    private System.Action onDestroyed;

    public void Initialize(float maxHealth, System.Action destroyedCallback)
    {
        health = maxHealth;
        onDestroyed = destroyedCallback;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            onDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}
