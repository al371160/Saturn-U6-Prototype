using UnityEngine;

public class CustomCamTrigger : MonoBehaviour
{
    [Tooltip("Camera target when entering this trigger")]
    public Transform enterTarget;

    [Tooltip("Pitch angle when entering (0-90)")]
    [Range(0f, 90f)]
    public float enterPitch = 45f;

    [Tooltip("Camera target when exiting this trigger")]
    public Transform exitTarget;

    [Tooltip("Pitch angle when exiting (0-90)")]
    [Range(0f, 90f)]
    public float exitPitch = 45f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enterTarget != null)
        {
            CameraManager.Instance?.SetCameraFocus(enterTarget, enterPitch);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && exitTarget != null)
        {
            CameraManager.Instance?.SetCameraFocus(exitTarget, exitPitch);
        }
    }
}
