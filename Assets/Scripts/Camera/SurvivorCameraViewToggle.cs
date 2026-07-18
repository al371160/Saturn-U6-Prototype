using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Manual key toggle between the over-the-shoulder and top-down camera views.
/// Both CinemachineCamera instances stay active; only Priority changes, so CinemachineBrain
/// blends between them instead of hard-cutting.
/// </summary>
public class SurvivorCameraViewToggle : MonoBehaviour
{
    public CinemachineCamera shoulderView;
    public CinemachineCamera topDownView;
    public SurvivorMouseAimRig aimRig;
    public KeyCode toggleKey = KeyCode.V;

    private const int ActivePriority = 20;
    private const int InactivePriority = 5;

    private bool usingShoulderView = true;

    private void Start()
    {
        ApplyPriorities();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            usingShoulderView = !usingShoulderView;
            ApplyPriorities();
        }
    }

    /// <summary>Called once on landing after the battle bus sequence, to hand off from the
    /// freefall/shoulder view to the tactical top-down view automatically.</summary>
    public void ForceTopDownView()
    {
        usingShoulderView = false;
        ApplyPriorities();
    }

    private void ApplyPriorities()
    {
        if (shoulderView != null)
            shoulderView.Priority = new PrioritySettings { Enabled = true, Value = usingShoulderView ? ActivePriority : InactivePriority };
        if (topDownView != null)
            topDownView.Priority = new PrioritySettings { Enabled = true, Value = usingShoulderView ? InactivePriority : ActivePriority };
        if (aimRig != null)
            aimRig.SetShoulderViewActive(usingShoulderView);
    }
}
