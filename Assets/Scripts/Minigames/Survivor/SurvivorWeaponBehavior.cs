using UnityEngine;

public abstract class SurvivorWeaponBehavior : MonoBehaviour
{
    protected SurvivorMinigameController controller;
    protected SurvivorWeaponDataSO data;
    protected int starLevel = 1;

    public SurvivorWeaponDataSO Data => data;
    public int StarLevel => starLevel;
    public bool IsMaxStar => data != null && starLevel >= data.MaxStar;

    public void Initialize(SurvivorMinigameController owner, SurvivorWeaponDataSO weaponData, int star)
    {
        controller = owner;
        data = weaponData;
        starLevel = Mathf.Clamp(star, 1, weaponData.MaxStar);
        OnInitialize();
    }

    public void SetStarLevel(int star)
    {
        if (data == null)
            return;

        starLevel = Mathf.Clamp(star, 1, data.MaxStar);
        OnStarLevelChanged();
    }

    protected abstract void OnInitialize();
    protected abstract void OnStarLevelChanged();

    /// <summary>
    /// Auto weapons (isManualWeapon = false) always fire on cooldown as before. Manual/"active"
    /// weapons only fire while left-click is held, unless the player has toggled auto-fire on (E).
    /// </summary>
    protected bool CanFire()
    {
        if (data == null || !data.isManualWeapon)
            return true;

        return SurvivorWeaponManager.AutoFireEnabled || Input.GetMouseButton(0);
    }
}
