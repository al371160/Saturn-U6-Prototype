using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Creates or finds a NetworkManager and starts local Host / Client / Server for testing.
/// </summary>
public class SaturnNetworkBootstrap : MonoBehaviour
{
    public const ushort DefaultPort = 7777;

    public static SaturnNetworkBootstrap Instance { get; private set; }

    public ushort defaultPort = DefaultPort;
    public string defaultAddress = "127.0.0.1";

    public NetworkManager NetworkManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureNetworkManager();
    }

    public static SaturnNetworkBootstrap EnsureExists()
    {
        if (Instance != null)
            return Instance;

        SaturnNetworkBootstrap existing = FindFirstObjectByType<SaturnNetworkBootstrap>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject(nameof(SaturnNetworkBootstrap));
        return go.AddComponent<SaturnNetworkBootstrap>();
    }

    public NetworkManager EnsureNetworkManager()
    {
        if (NetworkManager != null)
        {
            EnsureUnityTransport(NetworkManager);
            return NetworkManager;
        }

        NetworkManager existing = Unity.Netcode.NetworkManager.Singleton;
        if (existing == null)
            existing = FindFirstObjectByType<NetworkManager>();

        if (existing == null)
        {
            GameObject nmObject = new GameObject("NetworkManager");
            DontDestroyOnLoad(nmObject);
            existing = nmObject.AddComponent<NetworkManager>();
            nmObject.AddComponent<UnityTransport>();
        }

        NetworkManager = existing;
        EnsureUnityTransport(NetworkManager);
        PartyLobbyBootstrap.RegisterPrefab(NetworkManager);
        return NetworkManager;
    }

    public static UnityTransport EnsureUnityTransport(NetworkManager networkManager)
    {
        if (networkManager == null)
            return null;

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
            transport = networkManager.gameObject.AddComponent<UnityTransport>();

        if (networkManager.NetworkConfig == null)
            networkManager.NetworkConfig = new NetworkConfig();

        if (networkManager.NetworkConfig.NetworkTransport == null)
            networkManager.NetworkConfig.NetworkTransport = transport;

        return transport;
    }

    /// <summary>Requirement alias for StartLocalHost.</summary>
    public bool LocalHost(ushort port = 0) => StartLocalHost(port);

    /// <summary>Requirement alias for StartLocalClient.</summary>
    public bool LocalJoin(string address, ushort port = 0) => StartLocalClient(address, port);

    public bool StartLocalHost(ushort port = 0)
    {
        NetworkManager nm = EnsureNetworkManager();
        UnityTransport transport = EnsureUnityTransport(nm);
        ushort usePort = port == 0 ? defaultPort : port;
        transport.SetConnectionData(defaultAddress, usePort, "0.0.0.0");

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();

        PartyLobbyBootstrap.RegisterPrefab(nm);
        bool ok = nm.StartHost();
        Debug.Log(ok
            ? $"[SaturnNetwork] Local Host on {defaultAddress}:{usePort}"
            : "[SaturnNetwork] StartHost failed");
        return ok;
    }

    public bool StartLocalClient(string address = null, ushort port = 0)
    {
        NetworkManager nm = EnsureNetworkManager();
        UnityTransport transport = EnsureUnityTransport(nm);
        string useAddress = string.IsNullOrEmpty(address) ? defaultAddress : address;
        ushort usePort = port == 0 ? defaultPort : port;
        transport.SetConnectionData(useAddress, usePort);

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();

        PartyLobbyBootstrap.RegisterPrefab(nm);
        bool ok = nm.StartClient();
        Debug.Log(ok
            ? $"[SaturnNetwork] Local Client → {useAddress}:{usePort}"
            : "[SaturnNetwork] StartClient failed");
        return ok;
    }

    public bool StartDedicatedServer(ushort port = 0)
    {
        NetworkManager nm = EnsureNetworkManager();
        UnityTransport transport = EnsureUnityTransport(nm);
        ushort usePort = port == 0 ? defaultPort : port;
        transport.SetConnectionData("0.0.0.0", usePort, "0.0.0.0");

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();

        bool ok = nm.StartServer();
        Debug.Log(ok
            ? $"[SaturnNetwork] Dedicated Server on port {usePort}"
            : "[SaturnNetwork] StartServer failed");
        return ok;
    }

    public void Shutdown()
    {
        if (NetworkManager != null && NetworkManager.IsListening)
            NetworkManager.Shutdown();
    }
}
