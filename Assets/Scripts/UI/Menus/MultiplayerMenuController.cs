using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Surviv.io-style multiplayer mode select + local/cloud test entry points.
/// </summary>
public class MultiplayerMenuController : MonoBehaviour
{
    [Header("Navigation")]
    public MainMenuController mainMenu;
    public TMP_Text statusText;
    public TMP_InputField joinCodeField;

    [Header("Buttons (optional — also callable via UnityEvents)")]
    [SerializeField] private Button playSoloButton;
    [SerializeField] private Button playDuoButton;
    [SerializeField] private Button playSquadButton;
    [SerializeField] private Button joinTeamButton;
    [SerializeField] private Button createTeamButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button localHostButton;
    [SerializeField] private Button localJoinButton;
    [SerializeField] private Button cloudQueueButton;
    [SerializeField] private Button cloudJoinButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        Wire(playSoloButton, OnPlaySolo);
        Wire(playDuoButton, OnPlayDuo);
        Wire(playSquadButton, OnPlaySquad);
        Wire(joinTeamButton, OnJoinTeam);
        Wire(createTeamButton, OnCreateTeam);
        Wire(howToPlayButton, OnHowToPlay);
        Wire(localHostButton, OnLocalHost);
        Wire(localJoinButton, OnLocalJoin);
        Wire(cloudQueueButton, OnCloudQueue);
        Wire(cloudJoinButton, OnCloudJoinByCode);
        Wire(backButton, OnBack);
    }

    public void OnPlaySolo() => BeginParty(MatchPartySize.Solo);
    public void OnPlayDuo() => BeginParty(MatchPartySize.Duo);
    public void OnPlaySquad() => BeginParty(MatchPartySize.Squad);

    public void OnJoinTeam() => SetStatus("Join Team — placeholder");
    public void OnCreateTeam() => SetStatus("Create Team — placeholder");
    public void OnHowToPlay() => SetStatus("How to Play — placeholder");

    public void OnBack()
    {
        mainMenu?.ShowMain();
    }

    public void OnLocalHost()
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        session.IsLocalTest = true;
        session.IsCloudSession = false;
        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;

        SaturnNetworkBootstrap bootstrap = SaturnNetworkBootstrap.EnsureExists();
        if (bootstrap != null)
            bootstrap.StartLocalHost();
        else
            SetStatus("Local Host — bootstrap missing, continuing as local test");

        SetStatus("Local Host — pick loadout");
        mainMenu?.ShowMatchPrep();
    }

    public void OnLocalJoin()
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        session.IsLocalTest = true;
        session.IsCloudSession = false;
        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;

        SaturnNetworkBootstrap bootstrap = SaturnNetworkBootstrap.EnsureExists();
        if (bootstrap != null)
            bootstrap.StartLocalClient();
        else
            SetStatus("Local Join — bootstrap missing, continuing as local test");

        SetStatus("Local Client — pick loadout");
        mainMenu?.ShowMatchPrep();
    }

    public async void OnCloudQueue()
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        SetStatus("Queuing via UGS…");
        bool ok = await SaturnCloudMatchmaker.EnsureExists().QueueByPartySizeAsync(session.PartySize);
        SetStatus(ok ? "Cloud session ready — pick loadout" : "Cloud failed — use Local Host");
        if (ok)
            mainMenu?.ShowMatchPrep();
    }

    public async void OnCloudJoinByCode()
    {
        string code = joinCodeField != null ? joinCodeField.text : null;
        SetStatus("Joining cloud…");
        bool ok = await SaturnCloudMatchmaker.EnsureExists().JoinCloudAsync(code);
        SetStatus(ok ? "Joined cloud — pick loadout" : "Join failed");
        if (ok)
            mainMenu?.ShowMatchPrep();
    }

    private void BeginParty(MatchPartySize size)
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        session.SetPartySize(size);
        session.IsCloudSession = true;
        session.IsLocalTest = false;
        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;
        SetStatus($"Selected {size} — choose loadout");
        mainMenu?.ShowMatchPrep();
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log("[MultiplayerMenu] " + msg);
    }

    private static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
