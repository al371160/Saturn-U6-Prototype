using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chest : MonoBehaviour
{
    [Header("Chest Settings")]
    public bool isDiggableOnly = false;
    public List<GameObject> itemsToSpawn;
    public Transform spawnPoint;
    public float launchForce = 5f;
    public Animator lidAnimator;

    private bool isPlayerInTrigger = false;
    private bool isOpened = false;

    private GameObject alertPanel;
    private ParticleSystem alertParticles;

    void Start()
    {
        alertPanel = GameObject.Find("ItemAlertPanelParent");
        if (alertPanel == null)
        {
            Debug.LogWarning("ItemAlertPanelParent not found in scene.");
        }
        else
        {
            alertParticles = alertPanel.GetComponentInChildren<ParticleSystem>();
            SetAlertChildrenActive(false);
        }
    }

    void Update()
    {
        if (isDiggableOnly) return;

        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E) && !isOpened)
        {
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (isOpened) return;
        isOpened = true;

        if (lidAnimator != null)
            lidAnimator.SetTrigger("Open");

        foreach (GameObject itemPrefab in itemsToSpawn)
        {
            GameObject spawnedItem = Instantiate(itemPrefab, spawnPoint.position, Quaternion.identity);

            // Give it a randomized upward force
            Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
                rb.AddForce(randomDirection * launchForce, ForceMode.Impulse);
            }

            // Optional: disable interaction for a moment to prevent instant pickup
            Item itemScript = spawnedItem.GetComponent<Item>();
            if (itemScript != null)
            {
                itemScript.enabled = false;
                StartCoroutine(EnableItemAfterDelay(itemScript, 0.5f)); // optional delay
            }
        }
    }

    private IEnumerator EnableItemAfterDelay(Item item, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (item != null)
            item.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            SetAlertChildrenActive(!isDiggableOnly);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            SetAlertChildrenActive(false);
        }
    }

    private void SetAlertChildrenActive(bool isActive)
    {
        if (alertPanel == null) return;

        foreach (Transform child in alertPanel.transform)
        {
            child.gameObject.SetActive(isActive);
        }

        if (alertParticles != null)
        {
            if (isActive) alertParticles.Play();
            else alertParticles.Stop();
        }
    }
}
