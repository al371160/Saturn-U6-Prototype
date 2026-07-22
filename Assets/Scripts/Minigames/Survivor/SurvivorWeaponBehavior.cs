using UnityEngine;

public abstract class SurvivorWeaponBehavior : MonoBehaviour
{
    /// <summary>Height above player feet. Matches loot-crate center (~0.55) so shots connect.</summary>
    protected const float ProjectileSpawnHeight = 0.55f;

    protected SurvivorMinigameController controller;
    protected SurvivorWeaponDataSO data;
    protected int starLevel = 1;

    public SurvivorWeaponDataSO Data => data;
    public int StarLevel => starLevel;
    public bool IsMaxStar => data != null && starLevel >= data.MaxStar;

    public void Initialize(SurvivorMinigameController owner, SurvivorWeaponDataSO weaponData, int star)
    {
        controller = owner;
        data = weaponData;
        starLevel = Mathf.Clamp(star, 1, weaponData.MaxStar);
        OnInitialize();
    }

    public void SetStarLevel(int star)
    {
        if (data == null)
            return;

        starLevel = Mathf.Clamp(star, 1, data.MaxStar);
        OnStarLevelChanged();
    }

    protected abstract void OnInitialize();
    protected abstract void OnStarLevelChanged();

    /// <summary>
    /// Auto-fire ON: all weapons fire continuously. Auto-fire OFF: hold LMB to fire.
    /// Toggle with Q (sticky across pickups). Pause forcefield is separate and never auto-fires.
    /// </summary>
    protected bool CanFire()
    {
        if (SurvivorWeaponManager.AutoFireEnabled)
            return true;

        return Input.GetMouseButton(0);
    }

    /// <summary>Public gate for companions (drones) that fire on behalf of this weapon.</summary>
    public bool AllowsFiring => CanFire();

    /// <summary>LMB redirects fire toward the cursor (manual aim) in both auto and manual modes.</summary>
    protected bool IsCursorAimHeld => Input.GetMouseButton(0);

    protected void PlayFireSfx()
    {
        SurvivorAudio.PlayWeaponFire(data);
    }

    /// <summary>
    /// Spawn at mid-crate / torso height relative to the player's feet. Weapon roots track the
    /// CharacterController transform (often its center), so adding a small offset to that Y flies
    /// over short crates.
    /// </summary>
    protected Vector3 GetProjectileSpawnPosition()
    {
        float feetY = transform.position.y;
        CharacterController cc = ResolvePlayerCharacterController();
        if (cc != null)
            feetY = cc.transform.position.y + cc.center.y - cc.height * 0.5f;

        return new Vector3(transform.position.x, feetY + ProjectileSpawnHeight, transform.position.z);
    }

    protected CharacterController ResolvePlayerCharacterController()
    {
        CharacterController local = GetComponentInParent<CharacterController>();
        if (local != null)
            return local;

        if (controller != null && controller.MinigamePlayer != null)
            return controller.MinigamePlayer.GetComponent<CharacterController>();

        return null;
    }

    /// <summary>True when LMB is held and a cursor aim direction can be resolved (allows firing with no enemies).</summary>
    protected bool HasCursorAim(out Vector3 flatDirection)
    {
        flatDirection = default;
        return IsCursorAimHeld && TryGetCursorAimDirection(out flatDirection);
    }

    protected bool HasAimSolution(Transform fallbackTarget)
    {
        if (HasCursorAim(out _))
            return true;
        return fallbackTarget != null;
    }

    /// <summary>LMB → cursor direction; otherwise nearest-target / forward fallback.</summary>
    protected Vector3 ResolveFlatAimDirection(Transform fallbackTarget)
    {
        if (HasCursorAim(out Vector3 cursorDir))
            return cursorDir;

        if (fallbackTarget != null)
        {
            Vector3 toTarget = fallbackTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                return toTarget.normalized;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
    }

    protected bool TryGetCursorAimDirection(out Vector3 flatDirection)
    {
        flatDirection = default;
        if (!TryGetCursorWorldPoint(out Vector3 worldPoint))
            return false;

        flatDirection = worldPoint - transform.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f)
            return false;

        flatDirection.Normalize();
        return true;
    }

    protected bool TryGetCursorWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Vector3 screenPoint = Input.mousePosition;
        if (Cursor.lockState == CursorLockMode.Locked)
            screenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Ray ray = cam.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            worldPoint = hit.point;
            return true;
        }

        float planeY = GetProjectileSpawnPosition().y;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (!aimPlane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    /// <summary>Shared projectile contact test — transform-moved kinematic triggers miss static crates.</summary>
    public static bool TryGetDamageableHit(Vector3 position, float radius, out Collider hitCollider)
    {
        hitCollider = null;
        Collider[] hits = Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Collide);
        float bestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null || col.GetComponentInParent<ISurvivorDamageable>() == null)
                continue;

            float sqr = (col.ClosestPoint(position) - position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                hitCollider = col;
            }
        }

        return hitCollider != null;
    }
}
