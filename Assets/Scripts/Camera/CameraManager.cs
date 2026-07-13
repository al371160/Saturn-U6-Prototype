using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public CameraYawToTarget mainCameraController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void SetCameraFocus(Transform newFocusTarget, float pitchAngle)
    {
        if (mainCameraController != null && newFocusTarget != null)
        {
            mainCameraController.SetMountainCenter(newFocusTarget, pitchAngle);
        }
    }

}
