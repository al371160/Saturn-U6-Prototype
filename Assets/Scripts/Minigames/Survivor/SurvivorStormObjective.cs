using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Center objective for the storm-to-center mode ("the Choir Spire"): 4 destructible shield pylons
/// gate a central core. The core is invulnerable while enough pylons still stand; once shielded
/// down, damaging the core to 0 destroys it — the round's win condition (checked via IsDestroyed,
/// consumed by SurvivorStormRoundRules).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurvivorStormObjective : MonoBehaviour, ISurvivorDamageable
{
    [Header("Pylons")]
    public float pylonMaxHealth = 800f;
    [Tooltip("Shields drop once fewer than this many pylons remain standing.")]
    public int pylonsRequiredForShield = 2;

    [Header("Core")]
    public float coreMaxHealth = 6000f;

    public bool IsDestroyed { get; private set; }
    public bool IsShielded => pylonsAlive.Count >= pylonsRequiredForShield;

    private readonly List<SurvivorStormPylon> pylonsAlive = new List<SurvivorStormPylon>();
    private float coreHealth;
    private Renderer coreRenderer;
    private Color coreBaseColor = new Color(0.05f, 0.05f, 0.08f);
    private static readonly Color CoreCrackColor = new Color(0.85f, 0.15f, 0.75f);

    /// <summary>Self-wiring — finds sibling SurvivorStormPylon components under the shared parent
    /// so this prefab works standalone once placed, with no external initialization call needed.</summary>
    private void Start()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        Initialize(searchRoot.GetComponentsInChildren<SurvivorStormPylon>(true));
    }

    public void Initialize(SurvivorStormPylon[] pylons)
    {
        coreHealth = coreMaxHealth;
        coreRenderer = GetComponentInChildren<Renderer>();
        if (coreRenderer != null)
            coreBaseColor = coreRenderer.material.color;

        pylonsAlive.Clear();
        if (pylons == null)
            return;

        foreach (SurvivorStormPylon pylon in pylons)
        {
            if (pylon == null)
                continue;

            SurvivorStormPylon capturedPylon = pylon;
            pylon.Initialize(pylonMaxHealth, () => pylonsAlive.Remove(capturedPylon));
            pylonsAlive.Add(pylon);
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed || IsShielded)
            return;

        coreHealth -= amount;
        UpdateCoreCrackVisual();

        if (coreHealth <= 0f)
            IsDestroyed = true;
    }

    private void UpdateCoreCrackVisual()
    {
        if (coreRenderer == null)
            return;

        float damagedFraction = 1f - Mathf.Clamp01(coreHealth / coreMaxHealth);
        coreRenderer.material.color = Color.Lerp(coreBaseColor, CoreCrackColor, damagedFraction);
    }
}
