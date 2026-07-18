using UnityEngine;

/// <summary>
/// Free-look chase-cam for the battle bus intro: orbits around the bus at a mouse-controlled
/// yaw/pitch (same SurvivorMouseAimRig driving every other view) while it flies, so the player can
/// look around freely before jumping instead of being locked to a fixed angle. Lives only as long
/// as the bus GameObject (created alongside it, destroyed with it) — its CinemachineCamera priority
/// is raised while riding and dropped the moment the rider jumps, so CinemachineBrain blends back
/// to whichever gameplay view (shoulder/top-down) was active before the bus sequence started.
/// </summary>
public class SurvivorBusCameraRig : MonoBehaviour
{
    public Transform bus;
    public SurvivorMouseAimRig aimRig;
    public float distance = 18f;
    public float height = 6f;
    public float positionLerpSpeed = 6f;

    private void LateUpdate()
    {
        if (bus == null)
            return;

        float yaw = aimRig != null ? aimRig.CameraYaw : bus.eulerAngles.y;
        float pitch = aimRig != null ? aimRig.CameraPitch : 20f;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPosition = bus.position - rotation * Vector3.forward * distance + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionLerpSpeed);

        Vector3 toBus = bus.position - transform.position;
        if (toBus.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toBus.normalized);
    }
}
