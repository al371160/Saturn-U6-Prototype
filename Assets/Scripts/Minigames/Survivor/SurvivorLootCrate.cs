using UnityEngine;

/// <summary>
/// Breakable loot box: visual shrinks as it takes damage, shatters at 0 HP, drops a weapon or buff.
/// Root collider stays full-size so player weapons keep connecting.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorLootCrate : MonoBehaviour, ISurvivorDamageable
{
    public const float CrateWorldSize = 2.2f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorWeaponDataSO[] weaponPool;
    private SurvivorBuffDataSO[] buffPool;
    private float maxHealth = 50f;
    private float health;
    private GameObject visual;
    private Renderer visualRenderer;
    private float weaponDropChance = 0.65f;

    public void Initialize(
        SurvivorMinigameController owner,
        Transform target,
        SurvivorWeaponDataSO[] weapons,
        SurvivorBuffDataSO[] buffs,
        float structureHealth = 50f,
        float weaponChance = 0.65f)
    {
        controller = owner;
        playerTarget = target;
        weaponPool = weapons;
        buffPool = buffs;
        maxHealth = Mathf.Max(1f, structureHealth);
        health = maxHealth;
        weaponDropChance = Mathf.Clamp01(weaponChance);

        transform.localScale = Vector3.one;
        BuildVisual();
        UpdateVisual();
    }

    private void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "LootCrateVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * CrateWorldSize;
        Object.Destroy(visual.GetComponent<Collider>());

        visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
            visualRenderer.material.color = new Color(0.55f, 0.38f, 0.18f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size = Vector3.one * CrateWorldSize;
            box.center = Vector3.zero;
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        UpdateVisual();
        // Hit SFX is played by SurvivorCombatFX.ApplyHit → PlayHitForTarget (avoids double-play).

        if (health <= 0f)
            Shatter();
    }

    private void UpdateVisual()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        // Mild shrink with damage — keep most of the silhouette readable.
        float scaleMul = Mathf.Lerp(0.78f, 1f, t);

        if (visual != null)
            visual.transform.localScale = Vector3.one * (CrateWorldSize * scaleMul);

        if (visualRenderer != null)
        {
            Color c = Color.Lerp(new Color(1f, 0.35f, 0.15f), new Color(0.55f, 0.38f, 0.18f), t);
            visualRenderer.material.color = c;
        }
    }

    private void Shatter()
    {
        Color tint = visualRenderer != null ? visualRenderer.material.color : new Color(0.7f, 0.4f, 0.2f);
        SurvivorExplosionFX.Play(transform.position, 2.2f, tint);
        SurvivorAudio.PlayDestroyForTarget(SurvivorHitAudioKind.Crate);
        SpawnDrop();
        Destroy(gameObject);
    }

    /// <summary>Crates always drop XP + Bits, and roll a chance at a weapon. Scope/buff pickups are
    /// not part of the default crate table anymore (bosses/SurvivorScopeStructure handle those) —
    /// buffPool is kept only for API/back-compat with callers that still pass one in.</summary>
    private void SpawnDrop()
    {
        SpawnXPReward();
        SpawnCoinReward();

        bool dropWeapon = weaponPool != null && weaponPool.Length > 0 && Random.value <= weaponDropChance;
        if (!dropWeapon)
            return;

        SurvivorWeaponDataSO weapon = SurvivorLootRarity.PickWeightedWeapon(weaponPool);
        if (weapon == null)
            return;

        int startStar = SurvivorLootRarity.RollStartStar(weapon);
        SpawnWeaponPickup(weapon, startStar, transform.position + Vector3.up * 0.5f);
    }

    /// <summary>A handful of small gems rather than one big chunk — reads as a burst of reward and
    /// still gets picked up even if the player only clips the crate's magnet radius.</summary>
    private void SpawnXPReward()
    {
        if (controller == null)
            return;

        int gemCount = Random.Range(2, 4);
        for (int i = 0; i < gemCount; i++)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.6f;
            Vector3 position = transform.position + new Vector3(jitter.x, 0.5f, jitter.y);
            controller.SpawnXPGem(position, Random.Range(3, 6));
        }
    }

    /// <summary>Bits (currency) — multiple small coin pickups rather than a single stack.</summary>
    private void SpawnCoinReward()
    {
        if (controller == null)
            return;

        int coinCount = Random.Range(1, 3);
        for (int i = 0; i < coinCount; i++)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.7f;
            Vector3 position = transform.position + new Vector3(jitter.x, 0.5f, jitter.y);
            SpawnCoinPickup(position, Random.Range(5, 11));
        }
    }

    private void SpawnCoinPickup(Vector3 position, int bitValue)
    {
        GameObject pickupObject = new GameObject("SurvivorCoinPickup");
        pickupObject.transform.position = position;
        SphereCollider col = pickupObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.35f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(pickupObject.transform, false);
        visual.transform.localScale = Vector3.one * 0.3f;
        Object.Destroy(visual.GetComponent<Collider>());
        Renderer coinRenderer = visual.GetComponent<Renderer>();
        if (coinRenderer != null)
            coinRenderer.material.color = new Color(1f, 0.85f, 0.2f);

        float magnetRadius = 4f + (controller.MinigamePlayer != null ? controller.MinigamePlayer.MagnetRadiusBonus : 0f);
        pickupObject.AddComponent<SurvivorCoinPickup>().Initialize(controller, playerTarget, bitValue, magnetRadius);
    }

    private void SpawnWeaponPickup(SurvivorWeaponDataSO weapon, int startStar, Vector3 position)
    {
        GameObject pickupObject = new GameObject("SurvivorWeaponPickup");
        pickupObject.transform.position = position;
        SphereCollider col = pickupObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.45f;
        pickupObject.AddComponent<SurvivorWeaponPickup>().Initialize(controller, playerTarget, weapon, startStar);
    }
}
