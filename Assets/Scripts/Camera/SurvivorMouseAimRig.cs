using UnityEngine;

/// <summary>
/// Owns a camera-yaw and camera-pitch value driven purely by mouse movement, independent of the
/// player's own facing (which PlayerController now turns to match movement/arrow-key input instead).
/// All three SurvivorFollowCameraRig views read CameraYaw; only the freefall glide camera reads
/// CameraPitch (via its useDynamicPitch flag) since the shoulder/top-down views use a fixed pitch.
/// Also manages cursor lock/visibility per active view: locked+hidden for the over-the-shoulder
/// view (classic FPS mouselook), free+visible for the top-down view — and always free+visible
/// whenever an upgrade/level-up menu is open, regardless of view, so its buttons are clickable.
/// </summary>
public class SurvivorMouseAimRig : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public bool shoulderViewActive = true;

    [Tooltip("Clamp range for CameraPitch, in degrees. Positive pitch looks down (steep dive while gliding); negative allows looking up. Kept away from exactly 90 so the glide never drops perfectly straight down.")]
    public float minPitch = -40f;
    public float maxPitch = 80f;

    public float CameraYaw { get; private set; }
    public float CameraPitch { get; private set; } = 35f;

    private bool menuOpen;

    private void Start()
    {
        CameraYaw = transform.eulerAngles.y;
        ApplyCursorState();
    }

    private void Update()
    {
        if (menuOpen)
            return;

        CameraYaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        CameraPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        CameraPitch = Mathf.Clamp(CameraPitch, minPitch, maxPitch);
    }

    public void SetShoulderViewActive(bool active)
    {
        shoulderViewActive = active;
        ApplyCursorState();
    }

    /// <summary>Called by SurvivorMinigameController's Freeze/UnfreezeGameplay — always unlocks the
    /// cursor while an upgrade/weapon-choice menu is open, regardless of which camera view is active.</summary>
    public void SetMenuOpen(bool open)
    {
        menuOpen = open;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        if (!menuOpen && shoulderViewActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
