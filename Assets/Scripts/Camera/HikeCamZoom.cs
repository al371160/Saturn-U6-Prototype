using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class HikeCamZoom : MonoBehaviour
{
    public CinemachineCamera cinemachineCam;
    public Transform player;

    [Header("Camera Distances")]
    public float normalDistance = 8f;
    public float glideDistance = 15f;
    public float climbDistance = 12f;
    public float sprintDistance = 10f;
    public float dialogueDistance = 5f;
    public float zoomSpeed = 3f;

    [Header("Celebration Settings")]
    public float celebrationDistance = 4.5f;
    public float celebrationZoomSpeed = 4f;
    public float celebrationHoldTime = 3f; // How long to stay zoomed in
    private bool isCelebrating = false;
    private float celebrationTimer = 0f;



    [Header("Target Z Offsets")]
    public float normalZOffset = 0f;
    public float glideZOffset = -1f;
    public float climbZOffset = 1f;
    public float sprintZOffset = -0.5f;
    public float dialogueZOffset = 2f;
    public float offsetLerpSpeed = 3f;

    [Header("Height Zoom Settings")]
    public float maxHeight = 20f;
    public float heightZoomMultiplier = 1.5f;

    private CinemachineComponentBase positionComponent;

    void Start()
    {
        positionComponent = cinemachineCam.GetComponent<CinemachineComponentBase>();

        if (positionComponent == null || positionComponent.Stage != CinemachineCore.Stage.Body)
        {
            Debug.LogError("Cinemachine Body component not found on the CinemachineCamera.");
        }
    }

    void Update()
    {
        if (positionComponent == null || player == null) return;

        // Countdown the celebration timer
        if (isCelebrating)
        {
            celebrationTimer -= Time.deltaTime;
            if (celebrationTimer <= 0f)
            {
                isCelebrating = false;
            }
        }

        // Handle CameraDistance
        float targetDistance = GetTargetDistance();
        var composer = positionComponent as CinemachinePositionComposer;
        if (composer != null)
        {
            composer.CameraDistance = Mathf.Lerp(
                composer.CameraDistance,
                targetDistance,
                Time.deltaTime * zoomSpeed
            );

            // Handle TargetOffset Z
            Vector3 offset = composer.TargetOffset;
            float targetZ = GetTargetZOffset();
            offset.z = Mathf.Lerp(offset.z, targetZ, Time.deltaTime * offsetLerpSpeed);
            composer.TargetOffset = offset;
        }
    }

    private Coroutine zoomRoutine;

    public void ZoomForCelebration()
    {
        Debug.Log("celebrate");
        isCelebrating = true;
        celebrationTimer = celebrationHoldTime;
    }


    float GetTargetDistance()
    {
        if (isCelebrating)
            return celebrationDistance;

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return dialogueDistance;

        float baseDistance = normalDistance;

        if (IsGliding())
            baseDistance = glideDistance;
        else if (IsClimbing())
            baseDistance = climbDistance;
        else if (IsSprinting())
            baseDistance = sprintDistance;

        float heightFactor = Mathf.Clamp01(player.position.y / maxHeight);
        float zoomMultiplier = Mathf.Lerp(1f, heightZoomMultiplier, heightFactor);

        return baseDistance * zoomMultiplier;
    }


    float GetTargetZOffset()
    {
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return dialogueZOffset;

        if (IsGliding())
            return glideZOffset;
        if (IsClimbing())
            return climbZOffset;
        if (IsSprinting())
            return sprintZOffset;

        return normalZOffset;
    }

    bool IsGliding() => Input.GetKey(KeyCode.Space);
    bool IsClimbing() => Input.GetKey(KeyCode.LeftShift);
    bool IsSprinting() => Input.GetKey(KeyCode.LeftControl);
}
