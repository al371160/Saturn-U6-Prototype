using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Cloud path: UGS Auth → Lobby → Relay → NGO. Falls back to local host if UGS is unavailable.
/// </summary>
public class SaturnCloudMatchmaker : MonoBehaviour
{
    public static SaturnCloudMatchmaker Instance { get; private set; }

    public Lobby CurrentLobby { get; private set; }
    public string RelayJoinCode { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static SaturnCloudMatchmaker EnsureExists()
    {
        if (Instance != null)
            return Instance;

        SaturnCloudMatchmaker existing = FindFirstObjectByType<SaturnCloudMatchmaker>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("SaturnCloudMatchmaker");
        return go.AddComponent<SaturnCloudMatchmaker>();
    }

    public async Task<bool> EnsureAuthAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"[SaturnCloud] Signed in as {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaturnCloud] Auth failed (link Unity project in Dashboard?): {e.Message}");
            return false;
        }
    }

    private static void ApplyHostRelay(UnityTransport transport, Allocation allocation)
    {
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData,
            true);
    }

    private static void ApplyClientRelay(UnityTransport transport, JoinAllocation join)
    {
        transport.SetRelayServerData(
            join.RelayServer.IpV4,
            (ushort)join.RelayServer.Port,
            join.AllocationIdBytes,
            join.Key,
            join.ConnectionData,
            join.HostConnectionData,
            true);
    }

    public async Task<bool> HostCloudAsync(int maxPlayers)
    {
        maxPlayers = Mathf.Clamp(maxPlayers, 1, 8);
        if (!await EnsureAuthAsync())
            return false;

        try
        {
            SaturnNetworkBootstrap bootstrap = SaturnNetworkBootstrap.EnsureExists();
            NetworkManager nm = bootstrap.EnsureNetworkManager();
            UnityTransport transport = nm.GetComponent<UnityTransport>();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
            RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            ApplyHostRelay(transport, allocation);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, RelayJoinCode) }
                }
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
                $"Saturn_{MatchSessionState.EnsureExists().PartySize}",
                maxPlayers,
                options);

            if (!nm.StartHost())
            {
                Debug.LogWarning("[SaturnCloud] NGO StartHost failed after Relay allocate");
                return false;
            }

            MatchSessionState session = MatchSessionState.EnsureExists();
            session.IsCloudSession = true;
            session.IsLocalTest = false;
            Debug.Log($"[SaturnCloud] Host lobby {CurrentLobby.Id} relay {RelayJoinCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaturnCloud] HostCloud failed: {e.Message}");
            return false;
        }
    }

    public async Task<bool> JoinCloudAsync(string lobbyIdOrRelayCode)
    {
        if (string.IsNullOrWhiteSpace(lobbyIdOrRelayCode))
            return false;
        if (!await EnsureAuthAsync())
            return false;

        try
        {
            SaturnNetworkBootstrap bootstrap = SaturnNetworkBootstrap.EnsureExists();
            NetworkManager nm = bootstrap.EnsureNetworkManager();
            UnityTransport transport = nm.GetComponent<UnityTransport>();

            string relayCode = lobbyIdOrRelayCode.Trim();

            try
            {
                // Prefer lobby short code; fall back to lobby id; else treat as Relay join code.
                try
                {
                    CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(relayCode);
                }
                catch
                {
                    CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(relayCode);
                }

                if (CurrentLobby.Data != null &&
                    CurrentLobby.Data.TryGetValue("RelayJoinCode", out DataObject codeData) &&
                    !string.IsNullOrEmpty(codeData?.Value))
                {
                    relayCode = codeData.Value;
                }
            }
            catch
            {
                // Treat input as a raw relay join code.
            }

            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(relayCode);
            RelayJoinCode = relayCode;
            ApplyClientRelay(transport, join);

            if (!nm.StartClient())
            {
                Debug.LogWarning("[SaturnCloud] NGO StartClient failed after Relay join");
                return false;
            }

            MatchSessionState session = MatchSessionState.EnsureExists();
            session.IsCloudSession = true;
            session.IsLocalTest = false;
            Debug.Log($"[SaturnCloud] Joined via relay {relayCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaturnCloud] JoinCloud failed: {e.Message}");
            return false;
        }
    }

    /// <summary>Placeholder matchmaking: join an open lobby or host one for the party size.</summary>
    public async Task<bool> QueueByPartySizeAsync(MatchPartySize partySize)
    {
        int maxPlayers = (int)partySize;
        MatchSessionState.EnsureExists().SetPartySize(partySize);

        try
        {
            if (!await EnsureAuthAsync())
            {
                MatchSessionState.EnsureExists().IsLocalTest = true;
                return SaturnNetworkBootstrap.EnsureExists().StartLocalHost();
            }

            QueryResponse query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            });

            if (query.Results != null && query.Results.Count > 0)
                return await JoinCloudAsync(query.Results[0].Id);

            return await HostCloudAsync(maxPlayers);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaturnCloud] Queue failed, falling back to local host: {e.Message}");
            MatchSessionState session = MatchSessionState.EnsureExists();
            session.IsLocalTest = true;
            session.IsCloudSession = false;
            return SaturnNetworkBootstrap.EnsureExists().StartLocalHost();
        }
    }
}
