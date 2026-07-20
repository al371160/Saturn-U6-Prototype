using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Boosts the freefall glide camera's priority above the normal gameplay views (shoulder/top-down)
/// while the player is in freefall, actively gliding, or mid-skydive (post-bus-jump), and drops it
/// back down once they land so CinemachineBrain blends back to whichever gameplay view was active.
/// Sits below the battle bus's own chase-cam priority, so the bus camera still wins during the ride.
/// </summary>
public class SurvivorFreefallCameraActivator : MonoBehaviour
{
    public CinemachineCamera freefallCamera;
    public PlayerController player;

    private const int ActivePriority = 25;
    private const int InactivePriority = 0;

    private void Update()
    {
        if (freefallCamera == null || player == null)
            return;

        bool freefallActive = player.isSkydiving || player.isInFreefall || player.IsGliding;
        int desiredValue = freefallActive ? ActivePriority : InactivePriority;
        if (freefallCamera.Priority.Value != desiredValue)
            freefallCamera.Priority = new PrioritySettings { Enabled = true, Value = desiredValue };
    }
}
