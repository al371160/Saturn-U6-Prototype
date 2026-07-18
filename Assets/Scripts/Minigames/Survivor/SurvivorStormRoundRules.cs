using UnityEngine;

/// <summary>
/// Storm-to-center mode: win by destroying the center SurvivorStormObjective before the storm
/// fully closes; if nobody breaks it in time, the round ends in a draw.
/// </summary>
public class SurvivorStormRoundRules : SurvivorRoundRules
{
    public SurvivorStormController stormController;
    public SurvivorStormObjective objective;

    public override SurvivorRoundResult? OnTick(float deltaTime)
    {
        if (objective != null && objective.IsDestroyed)
            return new SurvivorRoundResult(SurvivorRoundOutcome.Win, "The Choir Spire was destroyed.");

        if (stormController != null && stormController.HasFullyClosed)
            return new SurvivorRoundResult(SurvivorRoundOutcome.Draw, "The storm closed before the Spire fell.");

        return null;
    }
}
