using UnityEngine;

/// <summary>
/// Starts a dedicated NGO server when launched with -dedicatedServer or SATURN_DEDICATED define.
/// </summary>
public class DedicatedServerBootstrap : MonoBehaviour
{
    public ushort port = 7777;

    private void Awake()
    {
        if (!ShouldRunDedicated())
            return;

        SaturnGameModeState.CurrentMode = SaturnGameMode.Multiplayer;
        MatchSessionState.EnsureExists().IsLocalTest = false;
        SaturnNetworkBootstrap.EnsureExists().StartDedicatedServer(port);
        Debug.Log("[Saturn] Dedicated server bootstrap active");
    }

    private static bool ShouldRunDedicated()
    {
#if SATURN_DEDICATED
        return true;
#else
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "-dedicatedServer", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
#endif
    }
}
