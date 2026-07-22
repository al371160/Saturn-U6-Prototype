using UnityEngine;

/// <summary>Boss-fired projectile — travels horizontally and damages the Survivor player on contact.
/// Uses overlap + distance checks because CharacterController does not reliably receive triggers.</summary>
public class SurvivorBossProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float hitRadius = 0.45f;
    private bool consumed;

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
        hitRadius = Mathf.Max(0.45f, transform.localScale.x * 0.75f);
    }

    private void Update()
    {
        if (consumed)
            return;

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
        if (consumed)
            return;

        SurvivorMinigamePlayer player = other.GetComponentInParent<SurvivorMinigamePlayer>();
        if (player != null)
            ApplyHit(player);
    }

    private void TryHitPlayer()
    {
        SurvivorMinigamePlayer player = FindNearestPlayer();
        if (player == null)
            return;

        Vector3 flatProjectile = transform.position;
        flatProjectile.y = 0f;
        Vector3 flatPlayer = player.transform.position;
        flatPlayer.y = 0f;

        float reach = hitRadius + 0.55f;
        if ((flatProjectile - flatPlayer).sqrMagnitude <= reach * reach)
            ApplyHit(player);
    }

    private SurvivorMinigamePlayer FindNearestPlayer()
    {
        if (SurvivorMinigameController.Instance != null && SurvivorMinigameController.Instance.MinigamePlayer != null)
            return SurvivorMinigameController.Instance.MinigamePlayer;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius * 2f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            SurvivorMinigamePlayer player = hits[i].GetComponentInParent<SurvivorMinigamePlayer>();
            if (player != null)
                return player;
        }

        return null;
    }

    private void ApplyHit(SurvivorMinigamePlayer player)
    {
        if (consumed || player == null)
            return;

        consumed = true;
        player.TakeProjectileDamage(damage);
        Destroy(gameObject);
    }
}
