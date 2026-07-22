using UnityEngine;

/// <summary>Thin facade so Survivor systems can fire SFX without owning AudioSources.</summary>
public static class SurvivorAudio
{
    public static void PlayHit()
    {
        PlayHitForTarget(SurvivorHitAudioKind.Enemy);
    }

    public static void PlayHitForTarget(GameObject target)
    {
        PlayHitForTarget(ResolveHitKind(target));
    }

    public static void PlayHitForTarget(SurvivorHitAudioKind kind)
    {
        PlayerAudioHub hub = PlayerAudioHub.Instance;
        PlayerAudioLibrary lib = hub != null ? hub.Library : null;
        if (hub == null || lib == null)
            return;

        SurvivorHitSfx profile = lib.GetHitProfile(kind);
        if (profile != null && profile.HasAny)
        {
            if (profile.impact != null)
                hub.PlayOneShot(profile.impact, profile.impactVolume);
            if (profile.react != null)
                hub.PlayOneShot(profile.react, profile.reactVolume);
            return;
        }

        hub.PlayOneShot(lib.hitImpact, 0.45f);
    }

    public static void PlayCrateHit()
    {
        PlayHitForTarget(SurvivorHitAudioKind.Crate);
    }

    public static void PlayCrateShatter()
    {
        PlayDestroyForTarget(SurvivorHitAudioKind.Crate);
    }

    public static void PlayDestroyForTarget()
    {
        PlayDestroyForTarget(SurvivorHitAudioKind.Environment);
    }

    public static void PlayDestroyForTarget(GameObject target)
    {
        PlayDestroyForTarget(ResolveHitKind(target));
    }

    public static void PlayDestroyForTarget(SurvivorHitAudioKind kind)
    {
        PlayerAudioHub hub = PlayerAudioHub.Instance;
        PlayerAudioLibrary lib = hub != null ? hub.Library : null;
        if (hub == null || lib == null)
            return;

        SurvivorHitSfx profile = lib.GetHitProfile(kind);
        if (profile != null && profile.destroy != null)
        {
            hub.PlayOneShot(profile.destroy, profile.destroyVolume);
            return;
        }

        if (kind == SurvivorHitAudioKind.Crate && lib.crateShatter != null)
        {
            hub.PlayOneShot(lib.crateShatter, 0.8f);
            return;
        }

        hub.PlayOneShot(lib.hitImpact, 0.6f);
    }

    public static void PlayWeaponPickup()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.pickupWeapon != null ? lib.pickupWeapon : lib.pickupImportant, 0.85f);
    }

    public static void PlayBuffPickup()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.pickupGeneric, 0.75f);
    }

    public static void PlayLevelUp()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.levelUp != null ? lib.levelUp : lib.uiSelect, 0.9f);
    }

    public static void PlayUiSelect()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.uiSelect, 0.7f);
    }

    public static void PlayPlayerHurt()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.playerHurt, 0.7f);
    }

    public static void PlayWeaponFire()
    {
        PlayerAudioHub.Instance?.PlayLibrary(lib => lib.hitImpact, 0.35f);
    }

    public static void PlayWeaponFire(SurvivorWeaponDataSO weapon)
    {
        PlayerAudioHub hub = PlayerAudioHub.Instance;
        PlayerAudioLibrary lib = hub != null ? hub.Library : null;
        if (hub == null || lib == null)
            return;

        if (weapon != null)
        {
            SurvivorWeaponThemeSfx theme = lib.FindWeaponTheme(weapon);
            if (theme != null && theme.HasFire)
            {
                if (theme.fire != null)
                    hub.PlayOneShot(theme.fire, theme.fireVolume);
                if (theme.accent != null)
                    hub.PlayOneShot(theme.accent, theme.accentVolume);
                return;
            }

            AudioClip typeClip = lib.GetFireFallback(weapon.weaponType);
            if (typeClip != null)
            {
                hub.PlayOneShot(typeClip, 0.55f);
                return;
            }
        }

        hub.PlayOneShot(lib.hitImpact, 0.35f);
    }

    public static void StartCombatAmbience()
    {
        PlayerAudioHub hub = PlayerAudioHub.Instance;
        PlayerAudioLibrary lib = hub != null ? hub.Library : null;
        if (hub == null || lib == null)
            return;

        if (lib.ambientCombatLoop != null)
            hub.StartAmbientCombat(lib.ambientCombatLoop, lib.ambientCombatVolume);
        if (lib.ambientBedLoop != null)
            hub.StartAmbientBed(lib.ambientBedLoop, lib.ambientBedVolume);
    }

    public static void StopCombatAmbience()
    {
        PlayerAudioHub.Instance?.StopAmbient();
    }

    public static SurvivorHitAudioKind ResolveHitKind(GameObject target)
    {
        if (target == null)
            return SurvivorHitAudioKind.Enemy;

        if (target.GetComponentInParent<SurvivorBossEnemy>() != null)
            return SurvivorHitAudioKind.Boss;

        SurvivorMinigameEnemy enemy = target.GetComponentInParent<SurvivorMinigameEnemy>();
        if (enemy != null)
            return enemy.IsElite ? SurvivorHitAudioKind.Elite : SurvivorHitAudioKind.Enemy;

        if (target.GetComponentInParent<SurvivorLootCrate>() != null)
            return SurvivorHitAudioKind.Crate;

        return SurvivorHitAudioKind.Environment;
    }
}
