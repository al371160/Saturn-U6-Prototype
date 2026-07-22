using UnityEngine;

/// <summary>
/// Clip catalog for player + Survivor SFX. Assign clips in the inspector;
/// null fields are safe no-ops so we can fill remaining actions later.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Player Audio Library")]
public class PlayerAudioLibrary : ScriptableObject
{
    [Header("Movement")]
    public AudioClip footstep;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip dash;
    public AudioClip glideLoop;
    [Tooltip("Wind whoosh layered while gliding / freefalling.")]
    public AudioClip windLoop;
    public AudioClip swimLoop;

    [Header("Inventory / UI")]
    public AudioClip inventoryOpen;
    public AudioClip inventoryClose;
    public AudioClip uiSelect;
    public AudioClip uiHover;

    [Header("Pickups")]
    public AudioClip pickupGeneric;
    public AudioClip pickupImportant;
    public AudioClip pickupWeapon;

    [Header("Survivor combat (legacy single clips)")]
    public AudioClip hitImpact;
    public AudioClip crateShatter;
    public AudioClip levelUp;
    public AudioClip playerHurt;
    public AudioClip playerDeath;

    [Header("Survivor hit profiles")]
    public SurvivorHitSfx enemyHit = new SurvivorHitSfx();
    public SurvivorHitSfx eliteHit = new SurvivorHitSfx();
    public SurvivorHitSfx bossHit = new SurvivorHitSfx();
    public SurvivorHitSfx crateHit = new SurvivorHitSfx();
    public SurvivorHitSfx environmentHit = new SurvivorHitSfx();

    [Header("Ambient loops")]
    public AudioClip ambientCombatLoop;
    [Range(0f, 1f)] public float ambientCombatVolume = 0.18f;
    public AudioClip ambientBedLoop;
    [Range(0f, 1f)] public float ambientBedVolume = 0.12f;

    [Header("Weapon themes (keyword match on name / id / element)")]
    public SurvivorWeaponThemeSfx[] weaponThemes;

    [Header("Fire-by-weapon-type fallbacks")]
    public AudioClip fireProjectile;
    public AudioClip fireHitscan;
    public AudioClip fireMelee;
    public AudioClip fireOrbit;
    public AudioClip fireAura;
    public AudioClip fireBoomerang;
    public AudioClip fireChain;
    public AudioClip fireBouncing;
    public AudioClip firePoisonPool;
    public AudioClip fireExplosive;
    public AudioClip fireHoming;
    public AudioClip fireDrone;

    public SurvivorHitSfx GetHitProfile(SurvivorHitAudioKind kind)
    {
        switch (kind)
        {
            case SurvivorHitAudioKind.Elite:
                return eliteHit;
            case SurvivorHitAudioKind.Boss:
                return bossHit;
            case SurvivorHitAudioKind.Crate:
                return crateHit;
            case SurvivorHitAudioKind.Environment:
                return environmentHit;
            default:
                return enemyHit;
        }
    }

    public AudioClip GetFireFallback(SurvivorWeaponType weaponType)
    {
        switch (weaponType)
        {
            case SurvivorWeaponType.Hitscan:
                return fireHitscan;
            case SurvivorWeaponType.Melee:
                return fireMelee;
            case SurvivorWeaponType.Orbit:
                return fireOrbit != null ? fireOrbit : fireMelee;
            case SurvivorWeaponType.Aura:
                return fireAura;
            case SurvivorWeaponType.Boomerang:
                return fireBoomerang;
            case SurvivorWeaponType.Chain:
                return fireChain;
            case SurvivorWeaponType.BouncingBullet:
                return fireBouncing;
            case SurvivorWeaponType.PoisonPool:
                return firePoisonPool;
            case SurvivorWeaponType.Explosive:
                return fireExplosive;
            case SurvivorWeaponType.Homing:
                return fireHoming;
            case SurvivorWeaponType.Drone:
                return fireDrone;
            case SurvivorWeaponType.Projectile:
            default:
                return fireProjectile;
        }
    }

    public SurvivorWeaponThemeSfx FindWeaponTheme(SurvivorWeaponDataSO weapon)
    {
        if (weapon == null || weaponThemes == null || weaponThemes.Length == 0)
            return null;

        string haystack = $"{weapon.displayName} {weapon.weaponId} {weapon.element}".ToLowerInvariant();
        for (int i = 0; i < weaponThemes.Length; i++)
        {
            SurvivorWeaponThemeSfx theme = weaponThemes[i];
            if (theme == null || theme.keywords == null || !theme.HasFire)
                continue;

            for (int k = 0; k < theme.keywords.Length; k++)
            {
                string keyword = theme.keywords[k];
                if (string.IsNullOrEmpty(keyword))
                    continue;
                if (haystack.Contains(keyword.ToLowerInvariant()))
                    return theme;
            }
        }

        return null;
    }
}
