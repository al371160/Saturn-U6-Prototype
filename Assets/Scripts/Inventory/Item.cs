using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    public ItemSO itemData;
    public int quantity = 1;

    public float pickupDelay = 0.5f;

    private GameObject alertPanel;
    private BubblePopAnimation bubblePop;
    private InventoryManager inventoryManager;
    private MinigameManager minigameManager;
    private AudioSource audioSource;

    private bool isPlayerInTrigger = false;
    private bool bitPickupReady = false;
    private Coroutine bitPickupCoroutine;

    void Awake()
    {
        inventoryManager = GameObject.Find("InventoryCanvas")?.GetComponent<InventoryManager>();
        minigameManager = FindFirstObjectByType<MinigameManager>();

        alertPanel = GameObject.Find("ItemAlertPanelParent");
        if (alertPanel != null)
        {
            SetAlertChildrenActive(false);
            bubblePop = alertPanel.GetComponent<BubblePopAnimation>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isPlayerInTrigger && itemData.itemName == "Bit" && bitPickupReady)
        {
            HandleItemInteraction();
            bitPickupReady = false;
        }
        else if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E) && itemData.itemName != "Bit")
        {
            HandleItemInteraction();
        }
    }

    private void HandleItemInteraction()
    {
        if (itemData.requiredMinigame != ItemSO.MinigameType.None)
        {
            minigameManager.StartMinigame(itemData, () => { CollectItem(); }, () => { Debug.Log("Minigame failed."); });
            alertPanel?.SetActive(false);
        }
        else
        {
            CollectItem();
        }
    }

    public void CollectItem()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager is null on CollectItem(). Make sure it's initialized.");
            return;
        }

        int leftoverItems = inventoryManager.AddItem(itemData.itemName, quantity, itemData.icon, itemData.description);

        if (leftoverItems <= 0)
        {
            SetAlertChildrenActive(false);
            DisableMeshRenderers();

            if (itemData.pickupSound != null)
            {
                audioSource.PlayOneShot(itemData.pickupSound);
            }

            Destroy(gameObject, itemData.pickupSound != null ? itemData.pickupSound.length : 0f);
        }
        else
        {
            quantity = leftoverItems;
        }
    }

    private void DisableMeshRenderers()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            SetAlertChildrenActive(true);

            if (bubblePop != null)
                bubblePop.PlayPop();

            if (itemData.itemName == "Bit")
            {
                if (bitPickupCoroutine != null)
                    StopCoroutine(bitPickupCoroutine);

                bitPickupCoroutine = StartCoroutine(DelayedBitPickup());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            bitPickupReady = false;

            if (bitPickupCoroutine != null)
            {
                StopCoroutine(bitPickupCoroutine);
                bitPickupCoroutine = null;
            }

            SetAlertChildrenActive(false);
        }
    }

    private IEnumerator DelayedBitPickup()
    {
        yield return new WaitForSeconds(pickupDelay);
        bitPickupReady = true;
    }

    private void SetAlertChildrenActive(bool isActive)
    {
        if (alertPanel == null) return;

        foreach (Transform child in alertPanel.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }
}
