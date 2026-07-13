using UnityEngine;

public class StaminaRegenZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetInRegenZone(true);
            Debug.Log("in regen zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetInRegenZone(false);
            Debug.Log("out of regen zone");
        }
    }
}
