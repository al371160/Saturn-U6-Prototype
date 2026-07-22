using UnityEngine;

/// <summary>
/// Basic destructible world structure (mock crate primitive) that drops a random Scope buff
/// pickup when destroyed by the player's weapons — routes through the same ISurvivorDamageable /
/// SurvivorCombatFX.ApplyHit pathway every enemy already uses, so any weapon can break it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorScopeStructure : MonoBehaviour, ISurvivorDamageable
{
    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorBuffDataSO[] possibleScopes;
    private float health = 40f;

    public void Initialize(SurvivorMinigameController owner, Transform target, SurvivorBuffDataSO[] scopes, float structureHealth)
    {
        controller = owner;
        playerTarget = target;
        possibleScopes = scopes;
        health = structureHealth;
        BuildVisual();
    }

    private void BuildVisual()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
            litShader = Shader.Find("Standard");

        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "MockCrate";
        crate.transform.SetParent(transform, false);
        crate.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        Destroy(crate.GetComponent<Collider>());
        crate.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.55f, 0.4f, 0.2f) };
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

        if (possibleScopes != null && possibleScopes.Length > 0)
        {
            SurvivorBuffDataSO chosen = possibleScopes[Random.Range(0, possibleScopes.Length)];
            SpawnPickup(chosen);
        }

        Destroy(gameObject);
    }

    private void SpawnPickup(SurvivorBuffDataSO chosen)
    {
        GameObject pickupObject = new GameObject("ScopePickup");
        pickupObject.transform.position = transform.position + Vector3.up * 0.8f;

        SphereCollider col = pickupObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.4f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(pickupObject.transform, false);
        visual.transform.localScale = Vector3.one * 0.5f;
        Destroy(visual.GetComponent<Collider>());
        Renderer visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
            visualRenderer.material.color = chosen.iconColor;

        SurvivorScopePickup pickup = pickupObject.AddComponent<SurvivorScopePickup>();
        pickup.Initialize(controller, playerTarget, chosen, 6f);
    }
}
