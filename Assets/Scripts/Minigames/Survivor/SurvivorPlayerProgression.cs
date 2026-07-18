using System;
using UnityEngine;

public class SurvivorPlayerProgression : MonoBehaviour
{
    public event Action<int> OnLevelUp;

    public int Level { get; private set; } = 1;
    public int CurrentXP { get; private set; }
    public int XPToNextLevel { get; private set; }
    public float XPGainMultiplier { get; private set; } = 1f;

    private SurvivorMinigameConfig config;

    public void Initialize(SurvivorMinigameConfig levelConfig)
    {
        config = levelConfig;
        Level = 1;
        CurrentXP = 0;
        XPGainMultiplier = 1f;
        XPToNextLevel = ComputeXPRequirement(Level);
    }

    public void ApplyXPGainBonus(float percent)
    {
        XPGainMultiplier += percent;
    }

    public void AddXP(int amount)
    {
        if (config == null || amount <= 0)
            return;

        CurrentXP += Mathf.RoundToInt(amount * XPGainMultiplier);

        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            Level++;
            XPToNextLevel = ComputeXPRequirement(Level);
            OnLevelUp?.Invoke(Level);
        }
    }

    private int ComputeXPRequirement(int level)
    {
        int[] steps = config.xpStepTable;
        int stepIndex = level - 1;

        if (steps != null && stepIndex < steps.Length)
            return steps[stepIndex];

        int lastStep = (steps != null && steps.Length > 0) ? steps[steps.Length - 1] : 20;
        int overflowLevels = stepIndex - (steps?.Length ?? 0) + 1;
        return lastStep + Mathf.RoundToInt(config.xpPerLevelGrowth * overflowLevels);
    }
}
