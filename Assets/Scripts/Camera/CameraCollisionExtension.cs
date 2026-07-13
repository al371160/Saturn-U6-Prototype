using Unity.Cinemachine;
using UnityEngine;

[AddComponentMenu("Cinemachine/Extensions/Camera Collision Extension")]
public class CameraCollisionExtension : CinemachineExtension
{
    public Transform player;
    public LayerMask collisionMask;
    public float collisionRadius = 0.3f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || player == null) return;

        Vector3 camPos = state.RawPosition;
        Vector3 dir = camPos - player.position;
        float dist = dir.magnitude;
        if (dist < 0.01f) return;

        LayerMask mask = collisionMask != 0 ? collisionMask : ~0;
        Vector3 dirNorm = dir / dist;

        if (Physics.SphereCast(player.position, collisionRadius, dirNorm, out RaycastHit hit, dist, mask))
        {
            float safeDistance = Mathf.Max(hit.distance - collisionRadius, 0f);
            Vector3 safePos = player.position + dirNorm * safeDistance;
            state.PositionCorrection += safePos - camPos;
        }
    }
}
