using UnityEngine;

public enum SurvivorRoundOutcome
{
    None,
    Win,
    Loss,
    Draw
}

public struct SurvivorRoundResult
{
    public SurvivorRoundOutcome outcome;
    public string reason;

    public SurvivorRoundResult(SurvivorRoundOutcome outcome, string reason)
    {
        this.outcome = outcome;
        this.reason = reason;
    }
}

/// <summary>
/// Plug-in point defining what "win"/"lose" means for the active game mode.
/// SurvivorMinigameController stays mode-agnostic — it just calls into whichever rules object is
/// attached (via SetRoundRules) and ends the round when one returns a non-null result. Leaving
/// roundRules unset preserves the original endless-survival behavior exactly (no auto-win/loss).
/// </summary>
public abstract class SurvivorRoundRules : MonoBehaviour
{
    protected SurvivorMinigameController controller;

    public virtual void Initialize(SurvivorMinigameController owner)
    {
        controller = owner;
    }

    /// <summary>Called every frame the round is running.</summary>
    public virtual SurvivorRoundResult? OnTick(float deltaTime) => null;

    /// <summary>Called whenever an enemy is killed.</summary>
    public virtual SurvivorRoundResult? OnEnemyKilled() => null;

    /// <summary>Called when the player would otherwise be defeated. Returning a result here
    /// overrides the default soft-death (full heal + brief invulnerability) with a real round end.</summary>
    public virtual SurvivorRoundResult? OnPlayerDefeated() => null;
}
