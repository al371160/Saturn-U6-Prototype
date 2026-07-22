using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public bool usable;
    public bool important;

    [Header("Visuals")]
    public Sprite icon;
    [TextArea]
    public string description;

    [Header("Audio")]
    public AudioClip pickupSound;

    [Header("Stat Effect")]
    public StatToChange statToChange = StatToChange.none;
    public int amountToChangeStat = 10;

    [Header("Suit Stats")]
    [Tooltip("Only relevant when StatToChange is suitEquip.")]
    public float suitMaxWeight = 10f;
    public float suitMaxSpeed = 8f;

    [Header("Minigame Settings")]
    public MinigameType requiredMinigame = MinigameType.None;

    [Header("Survivor")]
    [Tooltip("When set, this inventory item represents a Survivor weapon (Power Core–style wiring).")]
    public SurvivorWeaponDataSO survivorWeapon;

    public void UseItem()
    {
        ItemEffectManager.Instance.ApplyEffect(this);
    }

    public void EquipItem()
    {
        ItemEffectManager.Instance.EquipEffect(this);
    }

    public enum StatToChange
    {
        none,
        maxStamina,
        shovelEquip,
        axeEquip,
        pickaxeEquip,
        stickEquip,
        suitEquip,
    };

    public enum MinigameType
    {
        None,
        QuickTime,
        RockBreaking,
        Survivor
    }
}
