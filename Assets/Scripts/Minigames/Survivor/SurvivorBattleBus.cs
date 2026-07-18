using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Flies a straight, randomized path across a configurable square region centered on world origin,
/// at a safe altitude above the terrain. The rider can jump off at any point (or is pushed off at
/// the end of the path), entering the player's skydive state with the bus's current travel velocity.
/// </summary>
public class SurvivorBattleBus : MonoBehaviour
{
    public float pathHalfExtent = 900f;
    public float flightAltitude = 160f;
    public float flightDuration = 25f;
    public KeyCode jumpKey = KeyCode.Space;

    [Tooltip("The flight line's closest approach to the map center (0,0) is randomized within this distance, so the bus always passes reasonably near the middle of the map without always cutting exactly through it.")]
    public float centerPassDistance = 80f;

    private const int BusCameraPriority = 30;
    private const int BusCameraInactivePriority = 0;

    private Transform rider;
    private PlayerController riderController;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float elapsed;
    private bool riderAboard;
    private System.Action onRiderJumped;
    private CinemachineCamera busCamera;

    private void Awake()
    {
        BuildMockVisual();
        BuildTrackingCamera();
    }

    /// <summary>Dedicated chase-cam for the flight, parented to the bus so it's destroyed alongside it.</summary>
    private void BuildTrackingCamera()
    {
        GameObject camObject = new GameObject("SurvivorBusCamera");
        camObject.transform.SetParent(transform, false);
        busCamera = camObject.AddComponent<CinemachineCamera>();
        busCamera.Priority = new PrioritySettings { Enabled = true, Value = BusCameraPriority };
        camObject.AddComponent<CinemachineImpulseListener>();

        SurvivorBusCameraRig rig = camObject.AddComponent<SurvivorBusCameraRig>();
        rig.bus = transform;
        rig.aimRig = FindFirstObjectByType<SurvivorMouseAimRig>();
    }

    /// <summary>Placeholder primitive-block silhouette so the bus is actually visible in-flight; swap for a real model later.</summary>
    private void BuildMockVisual()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
            litShader = Shader.Find("Standard");

        GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuselage.name = "MockFuselage";
        fuselage.transform.SetParent(transform, false);
        fuselage.transform.localScale = new Vector3(6f, 4f, 20f);
        Destroy(fuselage.GetComponent<Collider>());
        fuselage.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.22f, 0.25f, 0.3f) };

        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = "MockWing";
        wing.transform.SetParent(transform, false);
        wing.transform.localScale = new Vector3(24f, 0.6f, 3f);
        wing.transform.localPosition = new Vector3(0f, -0.5f, 1f);
        Destroy(wing.GetComponent<Collider>());
        wing.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.65f, 0.12f, 0.1f) };

        GameObject tailFin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailFin.name = "MockTailFin";
        tailFin.transform.SetParent(transform, false);
        tailFin.transform.localScale = new Vector3(0.6f, 5f, 4f);
        tailFin.transform.localPosition = new Vector3(0f, 2f, -9f);
        Destroy(tailFin.GetComponent<Collider>());
        tailFin.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.65f, 0.12f, 0.1f) };

        // Real solid floor the rider actually stands on (top surface at local y=0, matching where
        // the rider is parented) — without this the rider's CharacterController just falls straight
        // through empty air to the real ground far below while nominally parented to the bus.
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "MockPlatform";
        platform.transform.SetParent(transform, false);
        platform.transform.localScale = new Vector3(5f, 0.4f, 8f);
        platform.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        platform.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.3f, 0.32f, 0.35f) };
    }

    public void Launch(Transform player, System.Action onJumped)
    {
        rider = player;
        riderController = player != null ? player.GetComponent<PlayerController>() : null;
        onRiderJumped = onJumped;

        float travelAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 travelDir = new Vector3(Mathf.Sin(travelAngle), 0f, Mathf.Cos(travelAngle));
        Vector3 perpendicular = new Vector3(travelDir.z, 0f, -travelDir.x);

        float centerOffset = Random.Range(-centerPassDistance, centerPassDistance);
        Vector3 lineOrigin = perpendicular * centerOffset + Vector3.up * flightAltitude;

        startPoint = lineOrigin - travelDir * pathHalfExtent;
        endPoint = lineOrigin + travelDir * pathHalfExtent;

        transform.position = startPoint;
        Vector3 travelDirection = (endPoint - startPoint).normalized;
        if (travelDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(travelDirection);

        elapsed = 0f;
        riderAboard = true;

        if (rider != null)
        {
            rider.SetParent(transform, false);
            rider.localPosition = Vector3.zero;
            rider.localRotation = Quaternion.identity;
        }

        if (riderController != null)
        {
            riderController.canMove = false;
            riderController.canLook = false;
            riderController.isRidingBus = true;
        }
    }

    private void Update()
    {
        if (!riderAboard)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);
        transform.position = Vector3.Lerp(startPoint, endPoint, t);

        if (Input.GetKeyDown(jumpKey) || t >= 1f)
            JumpOff();
    }

    private void JumpOff()
    {
        if (!riderAboard)
            return;

        riderAboard = false;

        float travelDistance = Vector3.Distance(startPoint, endPoint);
        Vector3 busVelocity = flightDuration > 0f
            ? (endPoint - startPoint).normalized * (travelDistance / flightDuration)
            : Vector3.zero;

        if (rider != null)
            rider.SetParent(null, true);

        if (riderController != null)
        {
            riderController.isRidingBus = false;
            riderController.canMove = true;
            riderController.BeginSkydive(busVelocity);
        }

        if (busCamera != null)
            busCamera.Priority = new PrioritySettings { Enabled = true, Value = BusCameraInactivePriority };

        onRiderJumped?.Invoke();
    }
}
