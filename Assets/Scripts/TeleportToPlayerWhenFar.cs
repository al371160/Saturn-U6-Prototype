using UnityEngine;

public class TeleportToPlayerWhenFar : MonoBehaviour
{
    [Tooltip("Reference to the player GameObject.")]
    public Transform player;

    [Tooltip("Maximum allowed distance before teleporting.")]
    public float maxDistance = 20f;

    [Tooltip("Optional offset from the player’s position when teleporting.")]
    public Vector3 teleportOffset = Vector3.zero;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference not set.");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > maxDistance)
        {
            // Disable CharacterController before teleporting (if present)
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = player.position + teleportOffset;

            // Re-enable CharacterController after teleporting
            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }
    }
}
