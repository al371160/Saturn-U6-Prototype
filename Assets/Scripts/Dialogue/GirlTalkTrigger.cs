using UnityEngine;

public class GirlTalkTrigger : MonoBehaviour
{

    [Tooltip("Optional: Another GameObject to activate on trigger.")]
    public GameObject objectToActivate;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            if (objectToActivate != null)
            {
                objectToActivate.SetActive(false);
            }
        }
    }
}
