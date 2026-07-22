using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Multiplayer mode entry — wires NetworkManager bootstrap and marks SaturnGameMode.Multiplayer.
/// </summary>
public class MultiplayerGameMode : MonoBehaviour
{
    public NetworkManager networkManager;
    public SaturnNetworkBootstrap networkBootstrap;

    private void Awake()
    {
        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;

        networkBootstrap = SaturnNetworkBootstrap.EnsureExists();
        networkManager = networkBootstrap.EnsureNetworkManager();
        SaturnCloudMatchmaker.EnsureExists();
        MatchSessionState.EnsureExists();
    }
}
