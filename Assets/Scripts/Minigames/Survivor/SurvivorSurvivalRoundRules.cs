using UnityEngine;

/// <summary>
/// Default endless-survival rules: win at the round timer, no special loss condition (matches the
/// existing "soft death" feel — HandlePlayerDefeated's full-heal-and-continue stays the default
/// unless this returns a result). Opt-in via SurvivorMinigameController.SetRoundRules — leaving no
/// rules attached at all preserves the original fully-endless behavior exactly.
/// </summary>
public class SurvivorSurvivalRoundRules : SurvivorRoundRules
{
    public float roundDurationSeconds = 900f;
    private float elapsed;

    public override SurvivorRoundResult? OnTick(float deltaTime)
    {
        elapsed += deltaTime;
        if (elapsed >= roundDurationSeconds)
            return new SurvivorRoundResult(SurvivorRoundOutcome.Win, "Survived the round.");

        return null;
    }
}
