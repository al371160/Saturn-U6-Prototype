using UnityEngine;

/// <summary>
/// Breakable glass pane for building windows. Takes a few hits, then shatters with FX.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShatterableGlassPane : MonoBehaviour, ISurvivorDamageable
{
    public float maxHealth = 18f;
    public Color glassColor = new Color(0.55f, 0.75f, 0.95f, 0.35f);

    private float health;
    private Renderer glassRenderer;
    private bool shattered;

    private void Awake()
    {
        health = Mathf.Max(1f, maxHealth);
        glassRenderer = GetComponentInChildren<Renderer>();
        if (glassRenderer != null)
            glassRenderer.material = SurvivorTransparentMaterial.Create(glassColor, glassColor.a);
    }

    public void Configure(float hp, Color color)
    {
        maxHealth = Mathf.Max(1f, hp);
        health = maxHealth;
        glassColor = color;
        if (glassRenderer == null)
            glassRenderer = GetComponentInChildren<Renderer>();
        if (glassRenderer != null)
            glassRenderer.material = SurvivorTransparentMaterial.Create(glassColor, glassColor.a);
    }

    public void TakeDamage(float amount)
    {
        if (shattered)
            return;

        health -= amount;
        if (glassRenderer != null)
        {
            float t = Mathf.Clamp01(health / maxHealth);
            Color cracked = Color.Lerp(new Color(0.85f, 0.9f, 1f, 0.55f), glassColor, t);
            glassRenderer.material = SurvivorTransparentMaterial.Create(cracked, cracked.a);
        }

        if (health <= 0f)
            Shatter();
    }

    private void Shatter()
    {
        if (shattered)
            return;

        shattered = true;
        SurvivorExplosionFX.Play(transform.position, 1.1f, glassColor);
        SurvivorAudio.PlayDestroyForTarget(SurvivorHitAudioKind.Environment);
        Destroy(gameObject);
    }
}
