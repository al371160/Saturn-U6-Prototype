using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemFloating : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    public float buoyancyForce = 10f;
    public float waterDrag = 2f;
    public float floatSmoothness = 0.5f;

    private Rigidbody rb;
    private float waterHeight = 0f;
    private bool isInWater = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isInWater)
        {
            float objectY = transform.position.y;
            float depth = waterHeight - objectY;

            if (depth > 0f)
            {
                float force = buoyancyForce * depth;
                Vector3 upwardForce = Vector3.up * force * floatSmoothness;
                rb.AddForce(upwardForce, ForceMode.Acceleration);

                rb.linearVelocity *= 1f / (1f + waterDrag * Time.fixedDeltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            waterHeight = other.bounds.max.y; // Top surface of the water collider
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = false;
        }
    }
}
