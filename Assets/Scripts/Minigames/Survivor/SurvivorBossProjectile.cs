using UnityEngine;

/// <summary>Boss-fired projectile (ProjectileVolley attack) — mirrors SurvivorProjectile's travel/
/// lifetime pattern but damages the player instead of enemies.</summary>
public class SurvivorBossProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;

    public void Launch(Vector3 travelDirection, float travelSpeed, float hitDamage, float maxRange)
    {
        direction = travelDirection;
        speed = travelSpeed;
        damage = hitDamage;
        lifetime = travelSpeed > 0f ? maxRange / travelSpeed : 2f;
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        SurvivorMinigamePlayer player = other.GetComponent<SurvivorMinigamePlayer>();
        if (player == null)
            return;

        player.TakeContactDamage(damage);
        Destroy(gameObject);
    }
}
