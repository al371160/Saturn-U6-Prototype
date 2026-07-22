using UnityEngine;

/// <summary>Boss-fired projectile — travels horizontally and damages the Survivor player on contact.
/// Uses overlap checks because CharacterController does not reliably receive trigger messages.</summary>
public class SurvivorBossProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float hitRadius = 0.45f;

    public void Launch(Vector3 travelDirection, float travelSpeed, float hitDamage, float maxRange)
    {
        direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        direction.Normalize();

        speed = travelSpeed;
        damage = hitDamage;
        lifetime = travelSpeed > 0f ? maxRange / travelSpeed : 2f;
        hitRadius = Mathf.Max(0.35f, transform.localScale.x * 0.6f);
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        TryHitPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        SurvivorMinigamePlayer player = other.GetComponentInParent<SurvivorMinigamePlayer>();
        if (player != null)
            ApplyHit(player);
    }

    private void TryHitPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            SurvivorMinigamePlayer player = hits[i].GetComponentInParent<SurvivorMinigamePlayer>();
            if (player == null)
                continue;

            ApplyHit(player);
            return;
        }
    }

    private void ApplyHit(SurvivorMinigamePlayer player)
    {
        player.TakeContactDamage(damage);
        Destroy(gameObject);
    }
}
