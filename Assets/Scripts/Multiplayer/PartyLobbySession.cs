using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Synced party lobby: joined players, ready flags, and group deploy when the full team is ready.
/// </summary>
public class PartyLobbySession : NetworkBehaviour
{
    public static PartyLobbySession Instance { get; private set; }

    public event Action MembersChanged;
    public event Action DeployRequested;

    public NetworkList<LobbyMemberState> Members;

    private bool deployFired;

    private void Awake()
    {
        Members = new NetworkList<LobbyMemberState>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Members.OnListChanged += OnMembersListChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Seed everyone already connected (host + any early clients).
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
                TryAddMember(clientId);
        }

        MembersChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        Members.OnListChanged -= OnMembersListChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
            Instance = null;
    }

    private void OnMembersListChanged(NetworkListEvent<LobbyMemberState> changeEvent)
    {
        MembersChanged?.Invoke();
        if (IsServer)
            TryDeployIfReady();
    }

    private void OnClientConnected(ulong clientId)
    {
        TryAddMember(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        for (int i = Members.Count - 1; i >= 0; i--)
        {
            if (Members[i].ClientId == clientId)
                Members.RemoveAt(i);
        }
    }

    private void TryAddMember(ulong clientId)
    {
        if (!IsServer)
            return;

        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].ClientId == clientId)
                return;
        }

        string displayName = BuildDisplayName(clientId);
        Members.Add(new LobbyMemberState
        {
            ClientId = clientId,
            DisplayName = displayName,
            IsReady = false
        });
    }

    private static string BuildDisplayName(ulong clientId)
    {
        return $"Player {clientId}";
    }

    /// <summary>Clients call this after loadout so the lobby shows character names.</summary>
    public void RegisterLocalDisplayName()
    {
        if (!IsSpawned)
            return;

        MatchSessionState session = MatchSessionState.Instance;
        string name = session != null && !string.IsNullOrEmpty(session.CharacterId)
            ? session.CharacterId
            : $"Player {NetworkManager.LocalClientId}";

        if (IsServer)
            ApplyDisplayName(NetworkManager.LocalClientId, name);
        else
            RegisterDisplayNameServerRpc(name);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterDisplayNameServerRpc(FixedString64Bytes displayName, ServerRpcParams rpcParams = default)
    {
        ApplyDisplayName(rpcParams.Receive.SenderClientId, displayName.ToString());
    }

    private void ApplyDisplayName(ulong clientId, string displayName)
    {
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].ClientId != clientId)
                continue;

            LobbyMemberState updated = Members[i];
            updated.DisplayName = displayName;
            Members[i] = updated;
            return;
        }

        Members.Add(new LobbyMemberState
        {
            ClientId = clientId,
            DisplayName = displayName,
            IsReady = false
        });
    }

    public void SetLocalReady(bool ready)
    {
        if (!IsSpawned)
            return;

        if (IsServer)
            ApplyReady(NetworkManager.LocalClientId, ready);
        else
            SetReadyServerRpc(ready);
    }

    public void ToggleLocalReady()
    {
        bool currentlyReady = IsLocalReady();
        SetLocalReady(!currentlyReady);
    }

    public bool IsLocalReady()
    {
        ulong localId = NetworkManager != null ? NetworkManager.LocalClientId : 0UL;
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].ClientId == localId)
                return Members[i].IsReady;
        }

        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        ApplyReady(rpcParams.Receive.SenderClientId, ready);
    }

    private void ApplyReady(ulong clientId, bool ready)
    {
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].ClientId != clientId)
                continue;

            LobbyMemberState updated = Members[i];
            updated.IsReady = ready;
            Members[i] = updated;
            break;
        }

        TryDeployIfReady();
    }

    private void TryDeployIfReady()
    {
        if (!IsServer || deployFired)
            return;

        int required = MatchSessionState.Instance != null
            ? Mathf.Max(1, (int)MatchSessionState.Instance.PartySize)
            : 1;

        if (Members.Count < required)
            return;

        for (int i = 0; i < Members.Count; i++)
        {
            if (!Members[i].IsReady)
                return;
        }

        deployFired = true;
        Debug.Log($"[PartyLobby] Entire team ready ({Members.Count}/{required}) — deploying");
        DeployClientRpc();
    }

    [ClientRpc]
    private void DeployClientRpc()
    {
        DeployRequested?.Invoke();
        PartyLobbyUI.TriggerGroupDeploy();
    }

    public int RequiredPartySize
    {
        get
        {
            return MatchSessionState.Instance != null
                ? Mathf.Max(1, (int)MatchSessionState.Instance.PartySize)
                : 1;
        }
    }

    public static GameObject CreateRuntimePrefab()
    {
        GameObject go = new GameObject("PartyLobbySession");
        go.AddComponent<NetworkObject>();
        go.AddComponent<PartyLobbySession>();
        return go;
    }
}

public struct LobbyMemberState : INetworkSerializable, IEquatable<LobbyMemberState>
{
    public ulong ClientId;
    public FixedString64Bytes DisplayName;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(LobbyMemberState other)
    {
        return ClientId == other.ClientId
               && IsReady == other.IsReady
               && DisplayName.Equals(other.DisplayName);
    }
}
