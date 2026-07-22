using UnityEngine;

[CreateAssetMenu(menuName = "Multiplayer/Match Prep Catalog", fileName = "MatchPrepCatalog")]
public class MatchPrepCatalog : ScriptableObject
{
    public SurvivorWeaponDataSO[] weaponOptions;
    public SurvivorBuffDataSO[] buffOptions;
}
