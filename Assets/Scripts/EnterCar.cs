using UnityEngine;

public class EnterCar : MonoBehaviour
{
    /*public GameObject player;             // Reference to the player GameObject
    public GameObject car;                // Reference to the car GameObject
    private bool isPlayerInTrigger = false;
    public CameraManager cameraManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerInTrigger = false;
        }
    }

    void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            EnterTheCar();
        }
    }

    private void EnterTheCar()
    {
        player.SetActive(false);  // Disable the player
        CarController carController = car.GetComponent<CarController>();
        if (carController != null)
        {
            carController.enabled = true;  // Enable the car controller
            cameraManager.SwitchCamera(cameraManager.topDownCam);
        }
        else
        {
            Debug.LogWarning("CarController component not found on car object.");
        }
    } */
}
