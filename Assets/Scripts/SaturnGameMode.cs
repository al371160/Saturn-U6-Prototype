public enum SaturnGameMode
{
    Campaign,
    Multiplayer
}

/// <summary>
/// Lightweight entry point distinguishing Campaign (the original narrative/exploration game)
/// from Multiplayer (the new diep.io/survivor.io-style PvP-plus-hordes mode). Combat-depth
/// systems (enemies, weapons, buffs) are shared infrastructure either mode can use.
/// </summary>
public static class SaturnGameModeState
{
    public static SaturnGameMode CurrentMode { get; set; } = SaturnGameMode.Campaign;
}
