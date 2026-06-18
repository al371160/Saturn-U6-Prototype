using UnityEngine;

public class CanvasBillboard : MonoBehaviour
{
    public Camera mainCamera;

    void Start()
    {
        // Cache the main camera reference for performance

    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Make the canvas face the camera
        transform.forward = mainCamera.transform.forward;

    }
}
