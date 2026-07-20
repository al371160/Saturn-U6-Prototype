using UnityEngine;

/// <summary>3D magnet pickup that equips or star-upgrades a Survivor weapon (Power Core–style world drop).</summary>
[RequireComponent(typeof(Collider))]
public class SurvivorWeaponPickup : MonoBehaviour
{
    private const float CollectDistance = 0.65f;

    private SurvivorMinigameController controller;
    private Transform playerTarget;
    private SurvivorWeaponDataSO weapon;
    private float magnetRadius = 7f;
    private float moveSpeed = 8f;
    private GameObject visual;

    public void Initialize(SurvivorMinigameController owner, Transform target, SurvivorWeaponDataSO weaponData, float pickupMagnetRadius = 7f)
    {
        controller = owner;
        playerTarget = target;
        weapon = weaponData;
        magnetRadius = pickupMagnetRadius;
        BuildVisual();
    }

    private void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "WeaponPickupVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
        Object.Destroy(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null && weapon != null)
            renderer.material.color = weapon.weaponColor;
    }

    private void Update()
    {
        if (controller == null || !controller.IsRunning || playerTarget == null || weapon == null)
            return;

        // Soft-pause (upgrade menu) still allows collection — only hard pause blocks.
        if (controller.IsPaused && !controller.IsUpgradeMenuOpen)
            return;

        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

        Vector3 toPlayer = playerTarget.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance <= CollectDistance * CollectDistance)
        {
            controller.WeaponManager?.EquipOrUpgrade(weapon);
            controller.NotifyLoadoutChanged();
            Destroy(gameObject);
            return;
        }

        if (sqrDistance <= magnetRadius * magnetRadius)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
    }
}
