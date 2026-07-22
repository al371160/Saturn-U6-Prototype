using UnityEngine;

/// <summary>
/// Breakable loot box: visual shrinks as it takes damage, shatters at 0 HP, drops a weapon or buff.
/// Root collider stays full-size so player weapons keep connecting.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorLootCrate : MonoBehaviour, ISurvivorDamageable
{
    public const float CrateWorldSize = 1.1f;

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

    private void SpawnDrop()
    {
        bool dropWeapon = weaponPool != null && weaponPool.Length > 0 && Random.value <= weaponDropChance;
        if (dropWeapon)
        {
            SurvivorWeaponDataSO weapon = SurvivorLootRarity.PickWeightedWeapon(weaponPool);
            if (weapon == null)
                return;

            int startStar = SurvivorLootRarity.RollStartStar(weapon);
            SpawnWeaponPickup(weapon, startStar, transform.position + Vector3.up * 0.5f);
            return;
        }

        if (buffPool == null || buffPool.Length == 0)
            return;

        SurvivorBuffDataSO buff = buffPool[Random.Range(0, buffPool.Length)];
        if (buff == null)
            return;

        GameObject buffPickup = new GameObject("ScopePickup");
        buffPickup.transform.position = transform.position + Vector3.up * 0.6f;
        SphereCollider buffCol = buffPickup.AddComponent<SphereCollider>();
        buffCol.isTrigger = true;
        buffCol.radius = 0.4f;

        GameObject visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualSphere.transform.SetParent(buffPickup.transform, false);
        visualSphere.transform.localScale = Vector3.one * 0.5f;
        Object.Destroy(visualSphere.GetComponent<Collider>());
        Renderer r = visualSphere.GetComponent<Renderer>();
        if (r != null)
            r.material.color = buff.iconColor;

        buffPickup.AddComponent<SurvivorScopePickup>().Initialize(controller, playerTarget, buff, 6f);
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
