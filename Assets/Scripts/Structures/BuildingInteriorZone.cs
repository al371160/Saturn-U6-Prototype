using UnityEngine;

/// <summary>
/// Trigger volume for enterable buildings. Notifies BuildingCutawayController when the local
/// player enters/exits so roof/walls can fade for that client only.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BuildingInteriorZone : MonoBehaviour
{
    public BuildingCutawayController cutaway;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        cutaway = GetComponentInParent<BuildingCutawayController>();
    }

    private void Awake()
    {
        if (cutaway == null)
            cutaway = GetComponentInParent<BuildingCutawayController>();

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other))
            return;

        cutaway?.NotifyLocalPlayerEntered();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other))
            return;

        cutaway?.NotifyLocalPlayerExited();
    }

    private static bool IsLocalPlayer(Collider other)
    {
        if (other == null)
            return false;

        // CharacterController root or child collider on the player.
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return false;

        // Future multiplayer: only the owning client should react.
        // For now every PlayerController in-scene is treated as local.
        return player.isActiveAndEnabled;
    }
}
