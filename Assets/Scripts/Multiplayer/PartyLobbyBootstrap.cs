using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Registers and spawns the party lobby NetworkObject for Host; clients receive it via NGO.
/// </summary>
public static class PartyLobbyBootstrap
{
    private const string PrefabResourcePath = "PartyLobbySession";
    private static GameObject registeredPrefab;

    public static void RegisterPrefab(NetworkManager networkManager)
    {
        if (networkManager == null)
            return;

        GameObject prefab = GetOrCreatePrefab();
        if (prefab == null)
            return;

        bool already = false;
        foreach (NetworkPrefab entry in networkManager.NetworkConfig.Prefabs.Prefabs)
        {
            if (entry.Prefab == prefab)
            {
                already = true;
                break;
            }
        }

        if (!already)
            networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
    }

    public static void EnsureLobbySession()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            Debug.Log("[PartyLobby] No active network — offline lobby UI only.");
            return;
        }

        RegisterPrefab(nm);

        if (PartyLobbySession.Instance != null && PartyLobbySession.Instance.IsSpawned)
            return;

        if (!nm.IsServer)
            return;

        GameObject prefab = GetOrCreatePrefab();
        GameObject instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        Debug.Log("[PartyLobby] Session spawned on server");
    }

    private static GameObject GetOrCreatePrefab()
    {
        if (registeredPrefab != null)
            return registeredPrefab;

        registeredPrefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (registeredPrefab != null)
            return registeredPrefab;

        // Runtime fallback prefab (not an asset) — registered before host/client start.
        registeredPrefab = PartyLobbySession.CreateRuntimePrefab();
        Object.DontDestroyOnLoad(registeredPrefab);
        registeredPrefab.SetActive(false);
        return registeredPrefab;
    }
}
