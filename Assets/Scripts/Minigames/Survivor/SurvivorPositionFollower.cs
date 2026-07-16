using UnityEngine;

public class SurvivorPositionFollower : MonoBehaviour
{
    private Transform target;

    public void Initialize(Transform followTarget)
    {
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (target != null)
            transform.position = target.position;
    }
}
