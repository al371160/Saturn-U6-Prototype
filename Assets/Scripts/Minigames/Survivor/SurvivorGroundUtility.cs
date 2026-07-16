using UnityEngine;

public static class SurvivorGroundUtility
{
    public static Vector3 SnapToGround(Vector3 position, LayerMask groundMask, float rayHeight, float heightOffset = 0f)
    {
        Vector3 origin = position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask))
            return hit.point + Vector3.up * heightOffset;

        return position;
    }
}
