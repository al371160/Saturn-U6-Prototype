using UnityEngine;

/// <summary>
/// Storm-to-center mode: shrinks a zone on the exact schedule the user specified (4/3/3/2/2/1-minute
/// phases = 15 min total), centered on world origin by default (matching
/// SurvivorBattleBus.centerPassDistance's existing use of origin-as-center). Damages the player
/// outside the ring, and drives SurvivorMinigameController.enemyIntensity/enemyEliteness
/// continuously from live distance-to-center rather than just at zone-shrink instants.
/// </summary>
public class SurvivorStormController : MonoBehaviour
{
    [System.Serializable]
    public struct StormPhase
    {
        public float durationSeconds;
        public float endRadius;
    }

    public SurvivorMinigameController controller;
    public SurvivorMinigamePlayer player;
    [Tooltip("Defaults to world origin if unset.")]
    public Transform stormCenter;

    public float startRadius = 1000f;
    public float damagePerSecondAtZone1 = 1f;
    public float damagePerSecondFinal = 15f;

    public StormPhase[] phases = new StormPhase[]
    {
        new StormPhase { durationSeconds = 240f, endRadius = 620f },
        new StormPhase { durationSeconds = 180f, endRadius = 380f },
        new StormPhase { durationSeconds = 180f, endRadius = 220f },
        new StormPhase { durationSeconds = 120f, endRadius = 120f },
        new StormPhase { durationSeconds = 120f, endRadius = 50f },
        new StormPhase { durationSeconds = 60f, endRadius = 35f },
    };

    public float CurrentRadius { get; private set; }
    public bool HasFullyClosed { get; private set; }

    private int currentPhaseIndex;
    private float phaseElapsed;
    private float phaseStartRadius;
    private float damageTickTimer;

    private void Start()
    {
        CurrentRadius = startRadius;
        phaseStartRadius = startRadius;

        // player/controller are added at runtime (SurvivorMinigameController.EnsurePlayerSystemsReady),
        // so they can't be wired as edit-time serialized references — fall back to a live lookup.
        if (controller == null)
            controller = FindFirstObjectByType<SurvivorMinigameController>();
        if (player == null)
            player = FindFirstObjectByType<SurvivorMinigamePlayer>();
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || controller.IsPaused || HasFullyClosed)
            return;

        // Player is spawned dynamically after the battle bus jump, so Start() can run before it
        // exists — keep looking until found rather than latching a permanent null.
        if (player == null)
            player = FindFirstObjectByType<SurvivorMinigamePlayer>();

        UpdatePhase();
        UpdateDistanceScaling();
        UpdateStormDamage();
    }

    private void UpdatePhase()
    {
        if (currentPhaseIndex >= phases.Length)
        {
            HasFullyClosed = true;
            return;
        }

        phaseElapsed += Time.deltaTime;
        StormPhase phase = phases[currentPhaseIndex];
        float t = phase.durationSeconds > 0f ? Mathf.Clamp01(phaseElapsed / phase.durationSeconds) : 1f;
        CurrentRadius = Mathf.Lerp(phaseStartRadius, phase.endRadius, t);

        if (phaseElapsed >= phase.durationSeconds)
        {
            phaseStartRadius = phase.endRadius;
            phaseElapsed = 0f;
            currentPhaseIndex++;
        }
    }

    private Vector3 CenterPosition => stormCenter != null ? stormCenter.position : Vector3.zero;

    private float DistanceFraction(Vector3 position)
    {
        Vector3 flat = position;
        flat.y = 0f;
        Vector3 center = CenterPosition;
        center.y = 0f;
        float distance = Vector3.Distance(flat, center);
        return Mathf.Clamp01(distance / Mathf.Max(1f, startRadius));
    }

    private void UpdateDistanceScaling()
    {
        if (controller == null || player == null)
            return;

        float t = DistanceFraction(player.transform.position);
        controller.enemyIntensity = Mathf.Lerp(2.2f, 0.65f, t);
        controller.enemyEliteness = t >= 0.55f ? 0f : 0.9f * (1f - t / 0.55f);
    }

    private void UpdateStormDamage()
    {
        if (player == null)
            return;

        Vector3 flat = player.transform.position;
        flat.y = 0f;
        Vector3 center = CenterPosition;
        center.y = 0f;
        float distance = Vector3.Distance(flat, center);

        if (distance <= CurrentRadius)
            return;

        damageTickTimer -= Time.deltaTime;
        if (damageTickTimer > 0f)
            return;

        damageTickTimer = 1f;
        float dps = Mathf.Lerp(damagePerSecondAtZone1, damagePerSecondFinal, (float)currentPhaseIndex / Mathf.Max(1, phases.Length - 1));
        player.TakeStormDamage(dps);
    }
}
