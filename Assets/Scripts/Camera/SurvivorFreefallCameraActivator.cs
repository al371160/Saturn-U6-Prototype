using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Boosts the freefall glide camera's priority above the normal gameplay views (shoulder/top-down)
/// while the player is mid-skydive (post-bus-jump freefall), and drops it back down once they land
/// so CinemachineBrain blends back to whichever gameplay view was active before the jump. Sits
/// below the battle bus's own chase-cam priority, so the bus camera still wins during the ride itself.
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

        int desiredValue = player.isSkydiving ? ActivePriority : InactivePriority;
        if (freefallCamera.Priority.Value != desiredValue)
            freefallCamera.Priority = new PrioritySettings { Enabled = true, Value = desiredValue };
    }
}
