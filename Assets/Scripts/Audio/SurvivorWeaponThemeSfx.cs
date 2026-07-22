using UnityEngine;

/// <summary>
/// Named theme bank for weapon SFX. Matched against weapon display name / id / element
/// (e.g. keywords "frost","cryo","ice" → ice fire clip for Frost Saber).
/// </summary>
[System.Serializable]
public class SurvivorWeaponThemeSfx
{
    [Tooltip("Inspector label only.")]
    public string themeName = "Theme";

    [Tooltip("Case-insensitive substrings matched against displayName, weaponId, and element.")]
    public string[] keywords;

    public AudioClip fire;
    [Tooltip("Optional layered whoosh / elemental accent played with fire.")]
    public AudioClip accent;

    [Range(0f, 1f)] public float fireVolume = 0.55f;
    [Range(0f, 1f)] public float accentVolume = 0.35f;

    public bool HasFire => fire != null || accent != null;
}
