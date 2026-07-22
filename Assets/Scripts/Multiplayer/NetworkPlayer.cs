using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal networked player stub — owner writes position; others read it.
/// Full Survivor combat replication is a later pass.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    private readonly NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
            networkPosition.Value = transform.position;
        else
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 12f);
    }
}
