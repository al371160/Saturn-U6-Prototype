using UnityEngine;

/// <summary>
/// Drives a camera's transform directly each frame from the shared mouse-look yaw
/// (<see cref="SurvivorMouseAimRig"/>), independent of the player's own facing — the player's body
/// turns to face movement (arrow keys) while the camera orbits per mouse input. Used for all three
/// switchable views — over-the-shoulder (short distance, low pitch, shoulder offset), top-down
/// "Zomboid" (long distance, steep pitch, no shoulder offset), and the freefall glide cam (dynamic
/// mouse-controlled pitch) — same rule, different tuning/flags per instance.
/// </summary>
public class SurvivorFollowCameraRig : MonoBehaviour
{
    public Transform player;
    public SurvivorMouseAimRig aimRig;
    public float distance = 6f;
    public float pitchAngle = 20f;
    public float heightOffset = 1.6f;
    public float shoulderOffset = 0f;
    public float positionLerpSpeed = 10f;
    public float zoomLerpSpeed = 6f;

    [Tooltip("If true, pitch is read live from aimRig.CameraPitch (mouse Y) instead of the fixed pitchAngle above — used by the freefall glide camera.")]
    public bool useDynamicPitch = false;

    [Tooltip("Zoom multiplier forced during dialogue/sign interaction, bypassing whatever scope buff is active.")]
    public float interactionZoomMultiplier = 0.4f;

    [Tooltip("Zoom forced while inside an enterable building (1 = default / 1x scope).")]
    public float buildingInteriorZoomMultiplier = 1f;

    private float zoomMultiplier = 1f;
    private float targetZoomMultiplier = 1f;
    private float scopeZoomMultiplier = 1f;
    private bool interactionZoomActive;
    private bool buildingInteriorZoomActive;

    /// <summary>Scopes/zoom items call this to pull the camera further back (>1) or push it in (<1).</summary>
    public void SetZoomMultiplier(float multiplier)
    {
        scopeZoomMultiplier = Mathf.Max(0.1f, multiplier);
        RefreshTargetZoom();
    }

    /// <summary>Dialogue/sign interaction calls this to force a close zoom regardless of the current
    /// scope buff, then restore whatever scope zoom was active when interaction ends.</summary>
    public void SetInteractionZoom(bool active)
    {
        interactionZoomActive = active;
        RefreshTargetZoom();
    }

    /// <summary>Enterable buildings force 1x zoom so interiors stay readable.</summary>
    public void SetBuildingInteriorZoom(bool active)
    {
        buildingInteriorZoomActive = active;
        RefreshTargetZoom();
    }

    private void RefreshTargetZoom()
    {
        if (interactionZoomActive)
            targetZoomMultiplier = interactionZoomMultiplier;
        else if (buildingInteriorZoomActive)
            targetZoomMultiplier = buildingInteriorZoomMultiplier;
        else
            targetZoomMultiplier = scopeZoomMultiplier;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        zoomMultiplier = Mathf.Lerp(zoomMultiplier, targetZoomMultiplier, Time.deltaTime * zoomLerpSpeed);

        float yaw = aimRig != null ? aimRig.CameraYaw : player.eulerAngles.y;
        float pitch = (useDynamicPitch && aimRig != null) ? aimRig.CameraPitch : pitchAngle;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 lookTarget = player.position + Vector3.up * heightOffset + (rotation * Vector3.right) * shoulderOffset;
        Vector3 desiredPosition = lookTarget - rotation * Vector3.forward * (distance * zoomMultiplier);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionLerpSpeed);
        transform.rotation = rotation;
    }
}
