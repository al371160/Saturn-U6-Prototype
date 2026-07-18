using UnityEngine;

/// <summary>
/// Procedural aim layer: twists the upper spine/chest bone toward the camera's mouse-aim yaw
/// each LateUpdate. The root (and legs) keep facing movement direction via PlayerController —
/// only the torso twists to aim, clamped so it doesn't snap the character into an anatomically
/// absurd pose when aim and movement point in opposite directions.
/// Sets an absolute rotation (captured baseline pose * twist offset) rather than an additive
/// Transform.Rotate — this rig's animations don't drive the chest bone every frame, so an
/// additive rotate would silently compound frame over frame into a runaway permanent lean.
/// </summary>
public class SurvivorUpperBodyAimRig : MonoBehaviour
{
    public Animator animator;
    public SurvivorMouseAimRig aimRig;
    public float maxTwistAngle = 80f;
    public float twistLerpSpeed = 10f;

    private Transform chestBone;
    private Quaternion baseChestLocalRotation = Quaternion.identity;
    private float currentTwist;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            chestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chestBone == null)
                chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone == null)
                chestBone = animator.GetBoneTransform(HumanBodyBones.Spine);
        }

        if (chestBone != null)
            baseChestLocalRotation = chestBone.localRotation;
    }

    private void LateUpdate()
    {
        if (chestBone == null || aimRig == null)
            return;

        float bodyYaw = transform.eulerAngles.y;
        float targetTwist = Mathf.Clamp(Mathf.DeltaAngle(bodyYaw, aimRig.CameraYaw), -maxTwistAngle, maxTwistAngle);

        currentTwist = Mathf.LerpAngle(currentTwist, targetTwist, Time.deltaTime * twistLerpSpeed);
        chestBone.localRotation = baseChestLocalRotation * Quaternion.Euler(0f, currentTwist, 0f);
    }
}
