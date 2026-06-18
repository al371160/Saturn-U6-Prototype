using UnityEngine;

public class CameraYawToTarget : MonoBehaviour
{
    public Transform player;
    public float rotationDamping = 3f;

    [Range(0f, 90f)]
    public float pitchAngle = 45f;

    [Tooltip("How quickly the camera interpolates between targets")]
    public float focusLerpSpeed = 2f;

    private Transform currentTarget; // The actual target camera uses
    private Transform desiredTarget; // Where the camera is trying to focus

    public float pitchLerpSpeed = 2f;

    private float currentPitch;
    private float desiredPitch;

    private void Start()
    {
        currentPitch = pitchAngle;
        desiredPitch = pitchAngle;
    }

    private void LateUpdate()
    {
        if (player == null || desiredTarget == null) return;

        // Smoothly interpolate target position
        if (currentTarget == null)
        {
            currentTarget = desiredTarget;
        }
        else if (currentTarget != desiredTarget)
        {
            Vector3 smoothedPosition = Vector3.Lerp(currentTarget.position, desiredTarget.position, Time.deltaTime * focusLerpSpeed);
            DummyTransform.position = smoothedPosition;
            currentTarget = DummyTransform;
        }

        // Smoothly interpolate pitch
        currentPitch = Mathf.Lerp(currentPitch, desiredPitch, Time.deltaTime * pitchLerpSpeed);

        Vector3 toCenter = currentTarget.position - player.position;
        toCenter.y = 0f;

        if (toCenter.sqrMagnitude > 0.01f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(toCenter.normalized);
            Quaternion currentRot = transform.rotation;

            float smoothedYaw = Quaternion.Slerp(currentRot, targetYaw, Time.deltaTime * rotationDamping).eulerAngles.y;
            transform.rotation = Quaternion.Euler(currentPitch, smoothedYaw, 0f);
        }
    }

    public void SetMountainCenter(Transform newTarget, float newPitchAngle)
    {
        if (newTarget != null)
        {
            desiredTarget = newTarget;
            desiredPitch = Mathf.Clamp(newPitchAngle, 0f, 90f);
        }
    }


    
    // Internal dummy used for interpolation between target positions
    private Transform _dummyTransform;
    private Transform DummyTransform
    {
        get
        {
            if (_dummyTransform == null)
            {
                GameObject dummy = new GameObject("Camera Dummy Target");
                dummy.hideFlags = HideFlags.HideAndDontSave;
                _dummyTransform = dummy.transform;
                _dummyTransform.position = desiredTarget != null ? desiredTarget.position : Vector3.zero;
            }
            return _dummyTransform;
        }
    }
}
