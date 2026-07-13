using UnityEngine;

public class PlayerSwimming : MonoBehaviour
{
    public PlayerController playerController;

    [Header("Swimming Settings")]
    public float swimSpeed = 4f;

    [Header("Buoyancy")]
    public float floatDepth = 0.5f;      // Desired depth under water surface
    public float floatSmoothSpeed = 5f;  // Speed of vertical adjustment

    [Header("Physics")]
    public float waterDrag = 2f;
    public LayerMask waterMask;

    public bool isSwimming = false;
    private Vector3 swimVelocity;

    private Transform waterSurface;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            Debug.Log("Entering water");
            waterSurface = other.transform;
            EnterWater();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            if (other.transform == waterSurface)
            {
                ExitWater();
                waterSurface = null;
            }
        }
    }

    void Update()
    {
        if (!isSwimming || !playerController.canMove) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Only move in horizontal plane
        Vector3 moveDir = (playerController.transform.forward * vertical + playerController.transform.right * horizontal).normalized;
        swimVelocity = moveDir * swimSpeed;

        // Apply horizontal movement
        playerController.controller.Move(swimVelocity * Time.deltaTime);

        // Keep player at surface
        MaintainSurfaceHeight();
    }

    void EnterWater()
    {
        isSwimming = true;
        playerController.velocity = Vector3.zero;
        playerController.gravity = 0f;
        playerController.playerAnim.SetBool("isSwimming", true);
    }

    void ExitWater()
    {
        isSwimming = false;
        playerController.gravity = -20f;
        playerController.playerAnim.SetBool("isSwimming", false);
        waterSurface = null;
    }

    private void MaintainSurfaceHeight()
    {
        if (waterSurface == null) return;

        float targetY = waterSurface.position.y - floatDepth;
        float currentY = transform.position.y;
        float newY = Mathf.Lerp(currentY, targetY, floatSmoothSpeed * Time.deltaTime);

        Vector3 position = transform.position;
        position.y = newY;

        Vector3 verticalCorrection = new Vector3(0f, newY - currentY, 0f);
        playerController.controller.Move(verticalCorrection);
    }

    public bool IsSwimming()
    {
        return isSwimming;
    }
}
