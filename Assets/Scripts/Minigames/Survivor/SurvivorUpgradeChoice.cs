using System;
using System.Collections.Generic;
using UnityEngine;

public class SurvivorUpgradeChoice
{
    public readonly string Title;
    public readonly string Description;
    public readonly Color IconColor;
    public readonly Action<SurvivorMinigameController> Apply;

    public SurvivorUpgradeChoice(string title, string description, Color iconColor, Action<SurvivorMinigameController> apply)
    {
        Title = title;
        Description = description;
        IconColor = iconColor;
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
                if (weapon == null || !controller.WeaponManager.CanUpgrade(weapon))
                    continue;

                bool isNew = !controller.WeaponManager.IsEquipped(weapon);
                string title = isNew ? $"New: {weapon.displayName}" : $"Upgrade: {weapon.displayName}";
                SurvivorWeaponDataSO capturedWeapon = weapon;

                pool.Add(new SurvivorUpgradeChoice(title, weapon.description, weapon.weaponColor, c => c.WeaponManager.EquipOrUpgrade(capturedWeapon)));
            }
        }

        if (config.availableBuffs != null)
        {
            foreach (SurvivorBuffDataSO buff in config.availableBuffs)
            {
                if (buff == null)
                    continue;

                SurvivorBuffDataSO capturedBuff = buff;
                pool.Add(new SurvivorUpgradeChoice(buff.displayName, buff.description, buff.iconColor, c => capturedBuff.Apply(c)));
            }
        }

        Shuffle(pool);

        int count = Mathf.Min(choiceCount, pool.Count);
        return pool.GetRange(0, count);
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
