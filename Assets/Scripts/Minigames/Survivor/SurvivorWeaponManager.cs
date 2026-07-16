using System.Collections.Generic;
using UnityEngine;

public class SurvivorWeaponManager : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private Transform manualWeaponRoot;
    private readonly Dictionary<string, SurvivorWeaponBehavior> equippedWeapons = new Dictionary<string, SurvivorWeaponBehavior>();

    public IReadOnlyCollection<SurvivorWeaponBehavior> EquippedWeapons => equippedWeapons.Values;

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
    }

    public bool IsEquipped(SurvivorWeaponDataSO data)
    {
        return data != null && equippedWeapons.ContainsKey(data.weaponId);
    }

    public bool CanUpgrade(SurvivorWeaponDataSO data)
    {
        if (data == null)
            return false;

        if (equippedWeapons.TryGetValue(data.weaponId, out SurvivorWeaponBehavior weapon))
            return !weapon.IsMaxStar || data.evolvesInto != null;

        return true;
    }

    public void EquipOrUpgrade(SurvivorWeaponDataSO data)
    {
        if (data == null || controller == null)
            return;

        if (equippedWeapons.TryGetValue(data.weaponId, out SurvivorWeaponBehavior weapon))
        {
            UpgradeWeapon(weapon, data);
            return;
        }

        SurvivorWeaponBehavior newWeapon = CreateWeaponBehavior(data);
        newWeapon.Initialize(controller, data, 1);
        equippedWeapons[data.weaponId] = newWeapon;
    }

    private void UpgradeWeapon(SurvivorWeaponBehavior weapon, SurvivorWeaponDataSO data)
    {
        if (!weapon.IsMaxStar)
        {
            weapon.SetStarLevel(weapon.StarLevel + 1);
            return;
        }

        if (data.evolvesInto == null)
            return;

        equippedWeapons.Remove(data.weaponId);
        Destroy(weapon.gameObject);
        EquipOrUpgrade(data.evolvesInto);
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
            case SurvivorWeaponType.Orbit:
            default:
                return weaponObject.AddComponent<SurvivorOrbitWeapon>();
        }
    }
}
