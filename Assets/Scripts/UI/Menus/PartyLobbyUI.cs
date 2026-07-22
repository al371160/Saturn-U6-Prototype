using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fortnite-style party lobby: joined player list, Ready toggle, group deploy when all ready.
/// Works networked (NGO) or offline solo.
/// </summary>
public class PartyLobbyUI : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private MainMenuController mainMenu;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text readyButtonLabel;

    [Header("Display")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text modeLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text joinFeedLabel;
    [SerializeField] private Transform playerListRoot;
    [SerializeField] private TMP_FontAsset uiFont;

    private readonly List<GameObject> rowObjects = new List<GameObject>();
    private readonly List<string> joinFeed = new List<string>();
    private readonly HashSet<ulong> knownMemberIds = new HashSet<ulong>();

    private bool offlineReady;
    private bool deployStarted;
    private bool subscribedToSession;
    private bool displayNameSent;
    private static PartyLobbyUI activeInstance;

    private void Awake()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyClicked);
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnEnable()
    {
        activeInstance = this;
        deployStarted = false;
        offlineReady = false;
        subscribedToSession = false;
        displayNameSent = false;
        joinFeed.Clear();
        knownMemberIds.Clear();

        MatchSessionState.EnsureExists();
        RefreshModeLabel();
        AppendFeed("Lobby opened.");

        PartyLobbyBootstrap.EnsureLobbySession();
        TryBindSession();
        RefreshPlayerList();
    }

    private void OnDisable()
    {
        UnsubscribeSession();
        subscribedToSession = false;
        displayNameSent = false;
        if (activeInstance == this)
            activeInstance = null;
    }

    private void Update()
    {
        // Clients may receive the session a frame or two after enabling the panel.
        if (!subscribedToSession)
            TryBindSession();
    }

    private void TryBindSession()
    {
        PartyLobbySession session = PartyLobbySession.Instance;
        if (session == null || !session.IsSpawned)
            return;

        SubscribeSession();
        if (!displayNameSent)
        {
            session.RegisterLocalDisplayName();
            displayNameSent = true;
        }

        RefreshPlayerList();
    }

    private void SubscribeSession()
    {
        PartyLobbySession session = PartyLobbySession.Instance;
        if (session == null || subscribedToSession)
            return;

        session.MembersChanged += OnMembersChanged;
        session.DeployRequested += OnDeployRequested;
        subscribedToSession = true;
    }

    private void UnsubscribeSession()
    {
        PartyLobbySession session = PartyLobbySession.Instance;
        if (session == null)
            return;

        session.MembersChanged -= OnMembersChanged;
        session.DeployRequested -= OnDeployRequested;
    }

    private void OnMembersChanged()
    {
        PartyLobbySession session = PartyLobbySession.Instance;
        if (session != null)
        {
            for (int i = 0; i < session.Members.Count; i++)
            {
                ulong id = session.Members[i].ClientId;
                if (knownMemberIds.Add(id))
                    AppendFeed($"{session.Members[i].DisplayName} has joined.");
            }
        }

        RefreshPlayerList();
    }

    private void OnDeployRequested()
    {
        TriggerGroupDeploy();
    }

    public static void TriggerGroupDeploy()
    {
        if (activeInstance != null)
            activeInstance.BeginDeployInternal();
        else
        {
            // Fallback if UI was disabled mid-rpc.
            DeployScreenController deploy = FindFirstObjectByType<DeployScreenController>(FindObjectsInactive.Include);
            MainMenuController menu = FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            menu?.ShowDeploy();
            deploy?.BeginDeploy();
        }
    }

    private void BeginDeployInternal()
    {
        if (deployStarted)
            return;
        deployStarted = true;

        if (statusLabel != null)
            statusLabel.text = "Team ready — deploying!";

        if (mainMenu != null)
            mainMenu.ShowDeploy();

        DeployScreenController deploy = FindFirstObjectByType<DeployScreenController>(FindObjectsInactive.Include);
        deploy?.BeginDeploy();
    }

    private void OnReadyClicked()
    {
        PartyLobbySession session = PartyLobbySession.Instance;
        NetworkManager nm = NetworkManager.Singleton;

        if (session != null && session.IsSpawned && nm != null && nm.IsListening)
        {
            session.ToggleLocalReady();
            RefreshPlayerList();
            return;
        }

        // Offline / solo path (no active NGO session).
        offlineReady = !offlineReady;
        RefreshPlayerList();
        if (offlineReady)
            BeginDeployInternal();
    }

    private void OnBackClicked()
    {
        offlineReady = false;
        deployStarted = false;
        mainMenu?.ShowMatchPrep();
    }

    private void RefreshModeLabel()
    {
        MatchSessionState session = MatchSessionState.EnsureExists();
        if (modeLabel != null)
            modeLabel.text = $"{session.PartySize}  ·  {(session.IsLocalTest ? "LOCAL" : session.IsCloudSession ? "CLOUD" : "PARTY")}";
        if (titleLabel != null)
            titleLabel.text = "PARTY LOBBY";
    }

    private void RefreshPlayerList()
    {
        ClearRows();

        PartyLobbySession session = PartyLobbySession.Instance;
        NetworkManager nm = NetworkManager.Singleton;
        int required = MatchSessionState.Instance != null
            ? Mathf.Max(1, (int)MatchSessionState.Instance.PartySize)
            : 1;

        bool networked = session != null && session.IsSpawned && nm != null && nm.IsListening;
        bool localReady = networked ? session.IsLocalReady() : offlineReady;

        if (readyButtonLabel != null)
            readyButtonLabel.text = localReady ? "UNREADY" : "READY";

        if (networked)
        {
            ulong localId = nm.LocalClientId;
            for (int i = 0; i < session.Members.Count; i++)
            {
                LobbyMemberState member = session.Members[i];
                string label = member.DisplayName.ToString();
                if (member.ClientId == localId)
                    label += " (You)";
                CreateRow(label, member.IsReady);
            }

            int connected = session.Members.Count;
            int readyCount = 0;
            for (int i = 0; i < session.Members.Count; i++)
            {
                if (session.Members[i].IsReady)
                    readyCount++;
            }

            for (int i = connected; i < required; i++)
                CreateRow($"Waiting for player…", false, muted: true);

            if (statusLabel != null)
            {
                if (connected < required)
                    statusLabel.text = $"Waiting for party ({connected}/{required})";
                else if (readyCount < required)
                    statusLabel.text = $"Ready up ({readyCount}/{required})";
                else
                    statusLabel.text = "All ready — deploying…";
            }
        }
        else
        {
            string name = MatchSessionState.Instance != null
                ? $"{MatchSessionState.Instance.CharacterId} (You)"
                : "You";
            CreateRow(name, offlineReady);

            for (int i = 1; i < required; i++)
                CreateRow("Waiting for player…", false, muted: true);

            if (statusLabel != null)
            {
                statusLabel.text = required <= 1
                    ? (offlineReady ? "Deploying…" : "Press READY to deploy")
                    : $"Offline lobby — need network for {required} players. Use Local Host / Join.";
            }
        }
    }

    private void CreateRow(string displayName, bool ready, bool muted = false)
    {
        if (playerListRoot == null)
            return;

        GameObject row = new GameObject("PlayerRow", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(playerListRoot, false);
        Image bg = row.GetComponent<Image>();
        bg.color = muted
            ? new Color(0.12f, 0.12f, 0.14f, 0.6f)
            : new Color(0.1f, 0.12f, 0.16f, 0.9f);

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 44f;

        GameObject nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(row.transform, false);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.04f, 0f);
        nameRt.anchorMax = new Vector2(0.62f, 1f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = displayName;
        nameTmp.fontSize = 22;
        nameTmp.color = muted ? new Color(0.7f, 0.7f, 0.75f) : Color.white;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (uiFont != null)
            nameTmp.font = uiFont;

        GameObject statusGo = new GameObject("ReadyStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(row.transform, false);
        RectTransform statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.64f, 0f);
        statusRt.anchorMax = new Vector2(0.98f, 1f);
        statusRt.offsetMin = Vector2.zero;
        statusRt.offsetMax = Vector2.zero;
        TextMeshProUGUI statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
        if (muted)
        {
            statusTmp.text = "—";
            statusTmp.color = new Color(0.5f, 0.5f, 0.55f);
        }
        else
        {
            statusTmp.text = ready ? "READY" : "NOT READY";
            statusTmp.color = ready ? new Color(0.35f, 0.9f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        statusTmp.fontSize = 20;
        statusTmp.alignment = TextAlignmentOptions.MidlineRight;
        if (uiFont != null)
            statusTmp.font = uiFont;

        rowObjects.Add(row);
    }

    private void ClearRows()
    {
        for (int i = 0; i < rowObjects.Count; i++)
        {
            if (rowObjects[i] != null)
                Destroy(rowObjects[i]);
        }

        rowObjects.Clear();

        if (playerListRoot == null)
            return;

        for (int i = playerListRoot.childCount - 1; i >= 0; i--)
            Destroy(playerListRoot.GetChild(i).gameObject);
    }

    private void AppendFeed(string message)
    {
        joinFeed.Add(message);
        while (joinFeed.Count > 6)
            joinFeed.RemoveAt(0);

        if (joinFeedLabel != null)
            joinFeedLabel.text = string.Join("\n", joinFeed);
    }
}
