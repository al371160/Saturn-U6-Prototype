using UnityEngine;

public class ActivateObjectOnStep : MonoBehaviour
{
    public GameObject objectToActivate; // Assign in Inspector
    public KeyCode activationKey = KeyCode.Q; // Key to toggle activation

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            objectToActivate.SetActive(true); // Activate object when stepping on
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(activationKey)) 
        {
            objectToActivate.SetActive(!objectToActivate.activeSelf); // Toggle object with key
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            objectToActivate.SetActive(false); // Deactivate object when stepping off
        }
    }
}
