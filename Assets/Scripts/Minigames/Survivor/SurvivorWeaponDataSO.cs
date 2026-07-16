using UnityEngine;

public enum SurvivorWeaponType
{
    Orbit,
    Projectile,
    Aura,
    Boomerang,
    Chain
}

[System.Serializable]
public struct SurvivorWeaponStarStats
{
    public float damage;
    [Tooltip("Orbit: rotation speed in deg/sec. Projectile/Boomerang: seconds between volleys. Aura/Chain: seconds between ticks.")]
    public float rate;
    [Tooltip("Orbit: orbit radius. Projectile/Boomerang: travel speed. Aura: radius. Chain: jump radius.")]
    public float range;
    [Tooltip("Orbit: number of orbiters. Projectile/Boomerang: shots per volley. Chain: max jumps. Unused by Aura.")]
    public int count;
}

[CreateAssetMenu(menuName = "Minigames/Survivor Weapon")]
public class SurvivorWeaponDataSO : ScriptableObject
{
    [Header("Identity")]
    public string weaponId;
    public string displayName;
    [TextArea]
    public string description;
    public SurvivorWeaponType weaponType;
    public Color weaponColor = Color.white;

    [Header("Shared")]
    public float hitRadius = 0.45f;
    [Tooltip("Manual weapons (melee, etc.) are parented to the player and swing with aim direction. " +
        "Auto weapons (orbit, projectile) only track the player's position, not rotation.")]
    public bool isManualWeapon = false;

    [Header("Star Progression (element 0 = star 1)")]
    public SurvivorWeaponStarStats[] starStats = new SurvivorWeaponStarStats[6];

    [Header("Evolution")]
    [Tooltip("Optional weapon this becomes when upgraded past max star.")]
    public SurvivorWeaponDataSO evolvesInto;

    public int MaxStar => Mathf.Max(1, starStats.Length);

    public SurvivorWeaponStarStats GetStats(int star)
    {
        int index = Mathf.Clamp(star - 1, 0, starStats.Length - 1);
        return starStats[index];
    }
}
