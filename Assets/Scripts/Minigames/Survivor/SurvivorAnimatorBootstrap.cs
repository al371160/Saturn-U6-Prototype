using UnityEngine;

/// <summary>
/// Zeros intermediate Animator layers that fight locomotion (common cause of broken Survivor
/// player anims) and disables upper-body aim during sprint if it compounds lean.
/// </summary>
public class SurvivorAnimatorBootstrap : MonoBehaviour
{
    public Animator animator;
    public PlayerController playerController;
    public SurvivorUpperBodyAimRig upperBodyAimRig;

    private void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
        if (upperBodyAimRig == null)
            upperBodyAimRig = GetComponentInChildren<SurvivorUpperBodyAimRig>();

        ZeroIntermediateLayers();
    }

    private void LateUpdate()
    {
        if (upperBodyAimRig == null || playerController == null || playerController.playerAnim == null)
            return;

        // Avoid aim twist fighting locomotion during sprint.
        bool suppressAim = playerController.playerAnim.GetBool("isSprinting");
        upperBodyAimRig.enabled = !suppressAim;
    }

    private void ZeroIntermediateLayers()
    {
        if (animator == null || animator.layerCount <= 1)
            return;

        // Keep base layer (0); zero every intermediate overlay layer.
        for (int i = 1; i < animator.layerCount; i++)
            animator.SetLayerWeight(i, 0f);
    }
}
