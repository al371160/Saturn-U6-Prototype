using UnityEngine;

/// <summary>
/// World-dressing exploding barrel — takes weapon damage like any other ISurvivorDamageable, and
/// once destroyed detonates for AoE damage against nearby enemies/props (and the player, ignoring
/// contact i-frames the same way a boss projectile does). Also deals a small touch damage to the
/// player while still standing, via ISurvivorContactDamageSource, since it's meant to read as a
/// hazard even before it pops.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorExplodingBarrel : MonoBehaviour, ISurvivorDamageable, ISurvivorContactDamageSource
{
    private float health = 30f;
    private float explosionDamage = 45f;
    private float explosionRadius = 6f;
    private float touchDamage = 4f;
    private bool exploded;

    public float ContactDamage => touchDamage;

    public void Initialize(float maxHealth, float aoeDamage, float aoeRadius, float contactTouchDamage)
    {
        health = Mathf.Max(1f, maxHealth);
        explosionDamage = aoeDamage;
        explosionRadius = Mathf.Max(1f, aoeRadius);
        touchDamage = contactTouchDamage;
    }

    public void TakeDamage(float amount)
    {
        if (exploded)
            return;

        health -= amount;
        if (health <= 0f)
            Explode();
    }

    private void Explode()
    {
        if (exploded)
            return;
        exploded = true;

        SurvivorExplosionFX.Play(transform.position, explosionRadius, new Color(1f, 0.55f, 0.15f));
        SurvivorAudio.PlayDestroyForTarget(gameObject);
        SurvivorCombatFX.Shake(0.8f);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject == gameObject)
                continue;

            ISurvivorDamageable damageable = hitObject.GetComponentInParent<ISurvivorDamageable>();
            if (damageable != null && !ReferenceEquals(damageable, this))
                SurvivorCombatFX.ApplyHit(hitObject, explosionDamage, SurvivorElementType.Fire, Vector3.zero, 4f);

            SurvivorMinigamePlayer player = hitObject.GetComponentInParent<SurvivorMinigamePlayer>();
            if (player != null)
                player.TakeProjectileDamage(explosionDamage);
        }

        Destroy(gameObject);
    }
}
