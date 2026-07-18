using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Skeleton only. Marks where Multiplayer round/lobby logic will live once real networking
/// (client-server combat sync, matchmaking, PvP state replication) gets its own dedicated
/// design pass — this does not implement any of that yet.
/// </summary>
public class MultiplayerGameMode : MonoBehaviour
{
    public NetworkManager networkManager;

    private void Awake()
    {
        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;

        if (networkManager == null)
            networkManager = GetComponent<NetworkManager>();
    }
}
