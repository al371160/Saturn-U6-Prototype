using UnityEngine;

public enum SurvivorBuffType
{
    MaxHealth,
    MoveSpeed,
    WeaponDamage,
    WeaponAttackSpeed,
    WeaponArea,
    XPGain,
    MagnetRadius,
    HealthRegen,
    Armor,
    MaxStamina,
    StaminaRegen,
    Fortune,
    CameraZoom
}

[CreateAssetMenu(menuName = "Minigames/Survivor Buff")]
public class SurvivorBuffDataSO : ScriptableObject
{
    [Header("Identity")]
    public string buffId;
    public string displayName;
    [TextArea]
    public string description;
    public Color iconColor = Color.white;
    public Sprite icon;

    [Header("Effect")]
    public SurvivorBuffType buffType;
    [Tooltip("Meaning depends on buffType: flat amount (MaxHealth/HealthRegen/MagnetRadius/MaxStamina/Fortune) " +
        "or a fraction like 0.1 = +10% (MoveSpeed/WeaponDamage/WeaponAttackSpeed/WeaponArea/XPGain/Armor/StaminaRegen/CameraZoom).")]
    public float magnitude;

    public void Apply(SurvivorMinigameController controller)
    {
        if (controller == null)
            return;

        controller.RecordBuffAcquired(this);

        SurvivorMinigamePlayer player = controller.MinigamePlayer;
        SurvivorWeaponManager weapons = controller.WeaponManager;
        SurvivorPlayerProgression progression = controller.Progression;

        switch (buffType)
        {
            case SurvivorBuffType.MaxHealth:
                player?.ApplyMaxHealthBonus(magnitude);
                break;
            case SurvivorBuffType.MoveSpeed:
                player?.ApplyMoveSpeedBonus(magnitude);
                break;
            case SurvivorBuffType.WeaponDamage:
                weapons?.AddDamageMultiplier(magnitude);
                break;
            case SurvivorBuffType.WeaponAttackSpeed:
                weapons?.AddRateMultiplier(magnitude);
                break;
            case SurvivorBuffType.WeaponArea:
                weapons?.AddRangeMultiplier(magnitude);
                break;
            case SurvivorBuffType.XPGain:
                progression?.ApplyXPGainBonus(magnitude);
                break;
            case SurvivorBuffType.MagnetRadius:
                player?.ApplyMagnetRadiusBonus(magnitude);
                break;
            case SurvivorBuffType.HealthRegen:
                player?.ApplyHealthRegenBonus(magnitude);
                break;
            case SurvivorBuffType.Armor:
                player?.ApplyDamageReductionBonus(magnitude);
                break;
            case SurvivorBuffType.MaxStamina:
                player?.ApplyMaxStaminaBonus(magnitude);
                break;
            case SurvivorBuffType.StaminaRegen:
                player?.ApplyStaminaRegenBonus(magnitude);
                break;
            case SurvivorBuffType.Fortune:
                player?.ApplyBonusXPPerKill(Mathf.RoundToInt(magnitude));
                break;
            case SurvivorBuffType.CameraZoom:
                player?.ApplyCameraZoomBonus(magnitude);
                break;
        }
    }
}
