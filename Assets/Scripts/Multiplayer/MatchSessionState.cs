using System.Collections.Generic;
using UnityEngine;

public enum MatchPartySize
{
    Solo = 1,
    Duo = 2,
    Squad = 4
}

/// <summary>
/// Survives scene loads so Main Menu loadout / party choices reach Survivor Level 1.
/// </summary>
public class MatchSessionState : MonoBehaviour
{
    public static MatchSessionState Instance { get; private set; }

    public MatchPartySize PartySize = MatchPartySize.Solo;
    public string CharacterId = "default";
    public string SkinId = "default";
    public SurvivorWeaponDataSO SelectedWeapon;
    public List<SurvivorBuffDataSO> SelectedBuffs = new List<SurvivorBuffDataSO>();
    public bool IsLocalTest;
    public bool IsCloudSession;
    public bool HasLoadout;
    public string CombatSceneName = "Level 1";

    public static MatchSessionState EnsureExists()
    {
        if (Instance != null)
            return Instance;

        MatchSessionState existing = FindFirstObjectByType<MatchSessionState>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return Instance;
        }

        GameObject go = new GameObject(nameof(MatchSessionState));
        Instance = go.AddComponent<MatchSessionState>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (SelectedBuffs == null)
            SelectedBuffs = new List<SurvivorBuffDataSO>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetPartySize(MatchPartySize partySize)
    {
        PartySize = partySize;
    }

    public void ClearLoadout()
    {
        SelectedWeapon = null;
        SelectedBuffs?.Clear();
        HasLoadout = false;
        CharacterId = "default";
        SkinId = "default";
    }

    public void SetLoadout(SurvivorWeaponDataSO weapon, IList<SurvivorBuffDataSO> buffs, string characterId, string skinId)
    {
        SelectedWeapon = weapon;
        if (SelectedBuffs == null)
            SelectedBuffs = new List<SurvivorBuffDataSO>();
        else
            SelectedBuffs.Clear();

        if (buffs != null)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i] != null)
                    SelectedBuffs.Add(buffs[i]);
            }
        }

        CharacterId = string.IsNullOrEmpty(characterId) ? "default" : characterId;
        SkinId = string.IsNullOrEmpty(skinId) ? "default" : skinId;
        HasLoadout = SelectedWeapon != null || SelectedBuffs.Count > 0;
    }
}
