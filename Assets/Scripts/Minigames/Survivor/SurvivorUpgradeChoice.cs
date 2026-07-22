using System;
using System.Collections.Generic;
using UnityEngine;

public class SurvivorUpgradeChoice
{
    public readonly string Title;
    public readonly string Description;
    public readonly Color IconColor;
    public readonly Sprite Icon;
    public readonly Action<SurvivorMinigameController> Apply;

    public SurvivorUpgradeChoice(string title, string description, Color iconColor, Sprite icon, Action<SurvivorMinigameController> apply)
    {
        Title = title;
        Description = description;
        IconColor = iconColor;
        Icon = icon;
        Apply = apply;
    }
}

public static class SurvivorUpgradePool
{
    public static List<SurvivorUpgradeChoice> BuildChoices(SurvivorMinigameController controller, SurvivorMinigameConfig config, int choiceCount)
    {
        List<SurvivorUpgradeChoice> pool = new List<SurvivorUpgradeChoice>();

        if (config.availableWeapons != null)
        {
            foreach (SurvivorWeaponDataSO weapon in config.availableWeapons)
            {
                if (weapon == null)
                    continue;

                int currentStar = controller.WeaponManager.GetStarLevel(weapon);
                bool isOwned = currentStar > 0;
                bool isMaxStar = isOwned && currentStar >= weapon.MaxStar;

                // New weapons only come from loot boxes — upgrades offer star-ups for owned weapons.
                if (isOwned && !isMaxStar)
                {
                    int nextStar = Mathf.Min(weapon.MaxStar, currentStar + 1);
                    string title = $"Upgrade: {weapon.displayName}";
                    string levelDescription = weapon.GetLevelDescription(nextStar);
                    SurvivorWeaponDataSO capturedWeapon = weapon;

                    pool.Add(new SurvivorUpgradeChoice(title, levelDescription, weapon.weaponColor, weapon.icon, c => c.WeaponManager.EquipOrUpgrade(capturedWeapon)));
                }

                // Diep.io-style branch choices — offered alongside the flat upgrade once equipped
                // and past a branch's requiredStar (which can be below max star).
                if (isOwned && weapon.evolutionOptions != null)
                {
                    foreach (SurvivorWeaponEvolutionOption option in weapon.evolutionOptions)
                    {
                        if (option.targetWeapon == null || currentStar < option.requiredStar)
                            continue;

                        SurvivorWeaponDataSO capturedFrom = weapon;
                        SurvivorWeaponDataSO capturedTarget = option.targetWeapon;
                        string branchTitle = $"Evolve: {option.branchName}";
                        string branchDescription = string.IsNullOrEmpty(option.branchDescription)
                            ? capturedTarget.GetLevelDescription(1)
                            : option.branchDescription;

                        pool.Add(new SurvivorUpgradeChoice(branchTitle, branchDescription, capturedTarget.weaponColor, capturedTarget.icon,
                            c => c.WeaponManager.EvolveWeapon(capturedFrom, capturedTarget)));
                    }
                }
            }
        }

        if (config.availableBuffs != null)
        {
            foreach (SurvivorBuffDataSO buff in config.availableBuffs)
            {
                // Scope/CameraZoom buffs are reserved for SurvivorCraneLoot / SurvivorScopeStructure /
                // boss drops (SurvivorUpgradePickup) — never offered on the level-up screen.
                if (buff == null || buff.buffType == SurvivorBuffType.CameraZoom)
                    continue;

                SurvivorBuffDataSO capturedBuff = buff;
                pool.Add(new SurvivorUpgradeChoice(buff.displayName, buff.description, buff.iconColor, buff.icon, c => capturedBuff.Apply(c)));
            }
        }

        AddInstantItems(pool);
        AddStatChoices(pool);

        Shuffle(pool);

        int count = Mathf.Min(choiceCount, pool.Count);
        return pool.GetRange(0, count);
    }

    /// <summary>One-shot consumable choices — apply an instant effect rather than a persistent stat.
    /// Full Heal and Nuke moved to ground-only SurvivorConsumablePickup finds; the level-up screen
    /// keeps only the utility pick that doesn't trivialize a level-up.</summary>
    private static void AddInstantItems(List<SurvivorUpgradeChoice> pool)
    {
        pool.Add(new SurvivorUpgradeChoice(
            "Loot Magnet",
            "Instantly collect every XP gem on the field.",
            new Color(0.9f, 0.75f, 0.3f), null,
            c => c.CollectAllGems()));
    }

    /// <summary>Always-available flat stat picks so every level-up has a reliable, boring-but-solid
    /// fallback alongside weapon upgrades and buffs.</summary>
    private static void AddStatChoices(List<SurvivorUpgradeChoice> pool)
    {
        pool.Add(new SurvivorUpgradeChoice(
            "Vitality",
            "+20 Max HP.",
            new Color(0.35f, 0.95f, 0.45f), null,
            c => c.MinigamePlayer?.ApplyMaxHealthBonus(20f)));

        pool.Add(new SurvivorUpgradeChoice(
            "Might",
            "+10% weapon damage.",
            new Color(0.95f, 0.35f, 0.3f), null,
            c => c.WeaponManager?.AddDamageMultiplier(0.10f)));

        pool.Add(new SurvivorUpgradeChoice(
            "Swiftness",
            "+8% move speed.",
            new Color(0.4f, 0.75f, 1f), null,
            c => c.MinigamePlayer?.ApplyMoveSpeedBonus(0.08f)));

        pool.Add(new SurvivorUpgradeChoice(
            "Alacrity",
            "+10% attack speed.",
            new Color(0.85f, 0.6f, 1f), null,
            c => c.WeaponManager?.AddRateMultiplier(0.10f)));
    }

    private static void Shuffle(List<SurvivorUpgradeChoice> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
