using UnityEngine;

/// <summary>Reaction-burst visual/shake for elemental combos — sibling to SurvivorCombatFX,
/// reusing its damage-popup and screen-shake helpers rather than building parallel VFX plumbing.</summary>
public static class SurvivorComboFX
{
    public static void PlayReaction(Vector3 position, SurvivorComboReaction reaction, float bonusDamage)
    {
        SurvivorCombatFX.ShowDamage(position, bonusDamage, ColorForReaction(reaction));
        SurvivorCombatFX.Shake(reaction == SurvivorComboReaction.ShatterStun ? 1.5f : 1f);
    }

    private static Color ColorForReaction(SurvivorComboReaction reaction)
    {
        switch (reaction)
        {
            case SurvivorComboReaction.BurstDamage:
                return new Color(1f, 0.5f, 0.1f);
            case SurvivorComboReaction.ShatterStun:
                return new Color(0.7f, 0.9f, 1f);
            case SurvivorComboReaction.TrueDamageAmp:
                return new Color(0.9f, 0.1f, 0.2f);
            default:
                return Color.white;
        }
    }
}
