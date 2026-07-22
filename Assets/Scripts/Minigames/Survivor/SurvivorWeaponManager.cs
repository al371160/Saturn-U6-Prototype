using System.Collections.Generic;
using UnityEngine;

public class SurvivorWeaponManager : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private Transform manualWeaponRoot;
    private readonly Dictionary<string, SurvivorWeaponBehavior> equippedWeapons = new Dictionary<string, SurvivorWeaponBehavior>();

    public IReadOnlyCollection<SurvivorWeaponBehavior> EquippedWeapons => equippedWeapons.Values;

    /// <summary>Toggled with E. While locked, no weapons fire. While unlocked, only holding LMB
    /// fires any weapon (manual aim also redirects toward the cursor).</summary>
    public static bool FireLocked { get; private set; }
    public KeyCode fireLockToggleKey = KeyCode.E;

    private void Update()
    {
        if (Input.GetKeyDown(fireLockToggleKey))
            FireLocked = !FireLocked;
    }

    /// <summary>Global multipliers layered on top of every weapon's own star-based stats, driven by buffs.</summary>
    public float DamageMultiplier { get; private set; } = 1f;
    public float RateMultiplier { get; private set; } = 1f;
    public float RangeMultiplier { get; private set; } = 1f;

    public void AddDamageMultiplier(float delta) => DamageMultiplier = Mathf.Max(0.1f, DamageMultiplier + delta);
    public void AddRateMultiplier(float delta) => RateMultiplier = Mathf.Max(0.1f, RateMultiplier + delta);
    public void AddRangeMultiplier(float delta) => RangeMultiplier = Mathf.Max(0.1f, RangeMultiplier + delta);

    /// <summary>
    /// This component's own transform is the root for "auto" weapons (orbit, projectile) — it only
    /// tracks the player's position (via SurvivorPositionFollower), never their look rotation, so
    /// ranged weapons don't visually spin/tilt as the player aims. Manual weapons (melee, etc.) get
    /// a separate child root that IS parented to the player, so they swing with aim direction.
    /// </summary>
    public void Initialize(SurvivorMinigameController owner, Transform playerTransform)
    {
        controller = owner;
        DamageMultiplier = 1f;
        RateMultiplier = 1f;
        RangeMultiplier = 1f;
        ClearWeapons();

        SurvivorPositionFollower follower = GetComponent<SurvivorPositionFollower>();
        if (follower == null)
            follower = gameObject.AddComponent<SurvivorPositionFollower>();
        follower.Initialize(playerTransform);

        if (manualWeaponRoot == null)
        {
            GameObject manualRootObject = new GameObject("ManualWeaponRoot");
            manualRootObject.transform.SetParent(playerTransform, false);
            manualWeaponRoot = manualRootObject.transform;
        }
    }

    public void ClearWeapons()
    {
        foreach (SurvivorWeaponBehavior weapon in equippedWeapons.Values)
        {
            if (weapon != null)
                Destroy(weapon.gameObject);
        }

        equippedWeapons.Clear();
        controller?.NotifyLoadoutChanged();
    }

    public bool IsEquipped(SurvivorWeaponDataSO data)
    {
        return data != null && equippedWeapons.ContainsKey(data.weaponId);
    }

    /// <summary>0 if not yet equipped, otherwise the weapon's current star level.</summary>
    public int GetStarLevel(SurvivorWeaponDataSO data)
    {
        if (data != null && equippedWeapons.TryGetValue(data.weaponId, out SurvivorWeaponBehavior weapon))
            return weapon.StarLevel;

        return 0;
    }

    public bool CanUpgrade(SurvivorWeaponDataSO data)
    {
        if (data == null)
            return false;

        if (equippedWeapons.TryGetValue(data.weaponId, out SurvivorWeaponBehavior weapon))
            return !weapon.IsMaxStar || HasAvailableBranch(data, weapon.StarLevel);

        return true;
    }

    private static bool HasAvailableBranch(SurvivorWeaponDataSO data, int currentStar)
    {
        if (data.evolutionOptions == null)
            return false;

        foreach (SurvivorWeaponEvolutionOption option in data.evolutionOptions)
        {
            if (option.targetWeapon != null && currentStar >= option.requiredStar)
                return true;
        }

        return false;
    }

    public void EquipOrUpgrade(SurvivorWeaponDataSO data)
    {
        EquipOrUpgrade(data, 1);
    }

    /// <summary>Equip at startStar, or if already owned raise to max(current, startStar) then +1 if equal
    /// only when startStar is the default upgrade path (startStar &lt;= current). Drop pickup uses max.</summary>
    public void EquipOrUpgrade(SurvivorWeaponDataSO data, int startStar)
    {
        if (data == null || controller == null)
            return;

        int desiredStar = Mathf.Clamp(startStar, 1, data.MaxStar);

        if (equippedWeapons.TryGetValue(data.weaponId, out SurvivorWeaponBehavior weapon))
        {
            if (desiredStar > weapon.StarLevel)
                weapon.SetStarLevel(desiredStar);
            else
                UpgradeWeapon(weapon);

            controller.NotifyLoadoutChanged();
            return;
        }

        SurvivorWeaponBehavior newWeapon = CreateWeaponBehavior(data);
        newWeapon.Initialize(controller, data, desiredStar);
        equippedWeapons[data.weaponId] = newWeapon;
        controller.NotifyLoadoutChanged();
    }

    /// <summary>Replaces an equipped weapon with a specific evolution branch target — called from a
    /// branch choice's own callback (SurvivorUpgradePool), not the generic EquipOrUpgrade path, since
    /// a weapon can have several available branches and the player picks exactly one.</summary>
    public void EvolveWeapon(SurvivorWeaponDataSO fromData, SurvivorWeaponDataSO targetData)
    {
        if (fromData == null || targetData == null)
            return;

        if (equippedWeapons.TryGetValue(fromData.weaponId, out SurvivorWeaponBehavior weapon))
        {
            equippedWeapons.Remove(fromData.weaponId);
            Destroy(weapon.gameObject);
        }

        EquipOrUpgrade(targetData);
    }

    private void UpgradeWeapon(SurvivorWeaponBehavior weapon)
    {
        if (!weapon.IsMaxStar)
            weapon.SetStarLevel(weapon.StarLevel + 1);
    }

    private SurvivorWeaponBehavior CreateWeaponBehavior(SurvivorWeaponDataSO data)
    {
        GameObject weaponObject = new GameObject($"Weapon_{data.displayName}");
        Transform parent = data.isManualWeapon ? manualWeaponRoot : transform;
        weaponObject.transform.SetParent(parent, false);

        switch (data.weaponType)
        {
            case SurvivorWeaponType.Projectile:
                return weaponObject.AddComponent<SurvivorProjectileWeapon>();
            case SurvivorWeaponType.Aura:
                return weaponObject.AddComponent<SurvivorAuraWeapon>();
            case SurvivorWeaponType.Boomerang:
                return weaponObject.AddComponent<SurvivorBoomerangWeapon>();
            case SurvivorWeaponType.Chain:
                return weaponObject.AddComponent<SurvivorChainWeapon>();
            case SurvivorWeaponType.Hitscan:
                return weaponObject.AddComponent<SurvivorHitscanWeapon>();
            case SurvivorWeaponType.BouncingBullet:
                return weaponObject.AddComponent<SurvivorBouncingBulletWeapon>();
            case SurvivorWeaponType.PoisonPool:
                return weaponObject.AddComponent<SurvivorPoisonPoolWeapon>();
            case SurvivorWeaponType.Explosive:
                return weaponObject.AddComponent<SurvivorExplosiveWeapon>();
            case SurvivorWeaponType.Homing:
                return weaponObject.AddComponent<SurvivorHomingWeapon>();
            case SurvivorWeaponType.Drone:
                return weaponObject.AddComponent<SurvivorDroneWeapon>();
            case SurvivorWeaponType.Melee:
                return weaponObject.AddComponent<SurvivorMeleeWeapon>();
            case SurvivorWeaponType.Orbit:
            default:
                return weaponObject.AddComponent<SurvivorOrbitWeapon>();
        }
    }
}
