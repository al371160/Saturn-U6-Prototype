using UnityEngine;

/// <summary>
/// Breakable loot box: shrinks as it takes damage, shatters at 0 HP, drops a weapon or buff pickup.
/// Spawns at predetermined map areas — not relative to the player.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorLootCrate : MonoBehaviour, ISurvivorDamageable
{
    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorWeaponDataSO[] weaponPool;
    private SurvivorBuffDataSO[] buffPool;
    private float maxHealth = 50f;
    private float health;
    private Vector3 baseScale = Vector3.one * 1.6f;
    private GameObject visual;
    private Renderer visualRenderer;

    [Tooltip("0–1 chance the drop is a weapon (rest are buffs).")]
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
        BuildVisual();
        UpdateScale();
    }

    private void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "LootCrateVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one;
        Object.Destroy(visual.GetComponent<Collider>());

        visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
            visualRenderer.material.color = new Color(0.55f, 0.38f, 0.18f);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        UpdateScale();

        if (health <= 0f)
            Shatter();
    }

    private void UpdateScale()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        float scaleMul = Mathf.Lerp(0.35f, 1f, t);
        transform.localScale = baseScale * scaleMul;

        if (visualRenderer != null)
        {
            Color c = Color.Lerp(new Color(0.9f, 0.35f, 0.15f), new Color(0.55f, 0.38f, 0.18f), t);
            visualRenderer.material.color = c;
        }
    }

    private void Shatter()
    {
        Color tint = visualRenderer != null ? visualRenderer.material.color : new Color(0.7f, 0.4f, 0.2f);
        SurvivorExplosionFX.Play(transform.position, 2.2f, tint);
        SpawnDrop();
        Destroy(gameObject);
    }

    private void SpawnDrop()
    {
        bool dropWeapon = weaponPool != null && weaponPool.Length > 0 && Random.value <= weaponDropChance;
        if (dropWeapon)
        {
            SurvivorWeaponDataSO weapon = weaponPool[Random.Range(0, weaponPool.Length)];
            if (weapon == null)
                return;

            GameObject pickupObject = new GameObject("SurvivorWeaponPickup");
            pickupObject.transform.position = transform.position + Vector3.up * 0.6f;
            SphereCollider col = pickupObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            pickupObject.AddComponent<SurvivorWeaponPickup>().Initialize(controller, playerTarget, weapon);
            return;
        }

        if (buffPool == null || buffPool.Length == 0)
            return;

        SurvivorBuffDataSO buff = buffPool[Random.Range(0, buffPool.Length)];
        if (buff == null)
            return;

        GameObject buffPickup = new GameObject("ScopePickup");
        buffPickup.transform.position = transform.position + Vector3.up * 0.8f;
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
}
