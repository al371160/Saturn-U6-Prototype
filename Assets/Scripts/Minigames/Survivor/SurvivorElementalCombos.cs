public enum SurvivorComboReaction
{
    None,
    BurstDamage,     // Fire + Poison
    ShatterStun,     // Ice + Lightning
    TrueDamageAmp    // Bleed + anything
}

/// <summary>
/// Fixed lookup table of Noita-style elemental combos — a small, curated set of pairings rather
/// than a full chemistry simulation. Checked once per hit in SurvivorStatusEffect.ApplyElement
/// before the incoming element's own DOT/slow would otherwise just stack independently alongside
/// whatever's already active.
/// </summary>
public static class SurvivorElementalCombos
{
    public static SurvivorComboReaction GetReaction(SurvivorElementType existing, SurvivorElementType incoming)
    {
        if (existing == SurvivorElementType.None || incoming == SurvivorElementType.None || existing == incoming)
            return SurvivorComboReaction.None;

        if (IsPair(existing, incoming, SurvivorElementType.Fire, SurvivorElementType.Poison))
            return SurvivorComboReaction.BurstDamage;

        if (IsPair(existing, incoming, SurvivorElementType.Ice, SurvivorElementType.Lightning))
            return SurvivorComboReaction.ShatterStun;

        if (existing == SurvivorElementType.Bleed || incoming == SurvivorElementType.Bleed)
            return SurvivorComboReaction.TrueDamageAmp;

        return SurvivorComboReaction.None;
    }

    private static bool IsPair(SurvivorElementType a, SurvivorElementType b, SurvivorElementType x, SurvivorElementType y)
    {
        return (a == x && b == y) || (a == y && b == x);
    }
}
