using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    //====ITEM DATA====//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;
    public string itemDescription;
    public Sprite emptySprite;

    [SerializeField] public int maxNumberOfItems;

    //====ITEM SLOT====//
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image itemImage;

    //====ITEM IMAGE====//
    public Image itemDescriptionImage;
    public TMP_Text itemDescriptionNameText;
    public TMP_Text itemDescriptionText;

    public GameObject selectedShader;
    public bool thisItemSelected;

    private InventoryManager inventoryManager;

    //====AUDIO====//
    public AudioClip hoverSound;
    public AudioClip clickSound;
    private AudioSource audioSource;

    //====SCALING====//
    private Vector3 normalScale;
    private Vector3 targetScale;
    private Vector3 hoverScale;
    private float scaleSpeed = 10f;
    private bool isHovered = false;

    private bool isTypingTitle = false;

    private Coroutine typingTitleCoroutine;

    public float titleTypingSpeed = 0.02f;




    void Start()
    {
        inventoryManager = GameObject.Find("InventoryCanvas").GetComponent<InventoryManager>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        normalScale = transform.localScale;
        hoverScale = normalScale * 1.1f;
        targetScale = normalScale;
    }

    void Update()
    {
        // Smoothly scale towards target
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        if (!thisItemSelected)
        {
            targetScale = hoverScale;
        }
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!thisItemSelected)
        {
            targetScale = normalScale;
        }
        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void OnLeftClick()
    {
        if (thisItemSelected)
        {
            ItemSO itemSO = inventoryManager.GetItemSO(itemName);
            if (itemSO != null && itemSO.usable)
            {
                inventoryManager.UseItem(itemName);
                Debug.Log("item used");
                RemoveItem(1);
            }
            else if (itemSO != null && !itemSO.usable)
            {
                Debug.Log("item equipped");
                inventoryManager.EquipItem(itemName);
            }
            else if (itemSO == null)
            {
                Debug.Log(itemName + " is not usable or itemSO doesn't exist.");
            }
        }
        else
        {
            inventoryManager.DeselectAllSlots();

            if (selectedShader != null)
                selectedShader.SetActive(true);

            thisItemSelected = true;

            // Stop any ongoing title typing
            isTypingTitle = false;

            if (typingTitleCoroutine != null)
            {
                StopCoroutine(typingTitleCoroutine);
            }

            // Immediately clear both texts
            itemDescriptionNameText.text = "";
            itemDescriptionText.text = ""; // This will now instantly display the full description.

            // Start new title typing coroutine
            typingTitleCoroutine = StartCoroutine(TypeItemTitle(itemName));

            // Instantly show description (no coroutine)
            itemDescriptionText.text = itemDescription;

            // Update image
            itemDescriptionImage.sprite = itemSprite;

            if (itemDescriptionImage.sprite == null)
            {
                itemDescriptionImage.sprite = emptySprite;
            }

            // Stay scaled when selected
            targetScale = hoverScale;

        }
    }

    private IEnumerator TypeItemTitle(string title)
    {
        isTypingTitle = true;
        itemDescriptionNameText.text = "";

        foreach (char letter in title.ToCharArray())
        {
            if (!isTypingTitle)
            {
                yield break;
            }

            itemDescriptionNameText.text += letter;
            yield return new WaitForSecondsRealtime(titleTypingSpeed);
        }

        isTypingTitle = false;
    }



 




    public void OnRightClick()
    {
        // Optional: Add right-click logic
    }

    public int AddItem(string itemName, int quantity, Sprite itemSprite, string itemDescription)
    {
        if (isFull)
        {
            return quantity;
        }

        this.itemName = itemName;
        this.itemSprite = itemSprite;
        this.itemDescription = itemDescription;

        itemImage.sprite = itemSprite;

        this.quantity += quantity;

        if (this.quantity >= maxNumberOfItems)
        {
            quantityText.text = maxNumberOfItems.ToString();
            quantityText.enabled = true;
            isFull = true;

            int extraItems = this.quantity - maxNumberOfItems;
            this.quantity = maxNumberOfItems;
            return extraItems;
        }

        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;

        return 0;
    }

    public void RemoveItem(int amount)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("Tried to remove item, but slot is already empty.");
            return;
        }

        quantity -= amount;
        if (quantity <= 0)
        {
            EmptySlot();
        }
        else
        {
            quantityText.text = quantity.ToString();
        }
    }

    private void EmptySlot()
    {
        quantity = 0;
        itemName = "";
        itemDescription = "";
        itemSprite = null;
        isFull = false;
        thisItemSelected = false;

        quantityText.enabled = false;
        itemImage.sprite = emptySprite;
        itemDescriptionNameText.text = "";
        itemDescriptionText.text = "";
        itemDescriptionImage.sprite = emptySprite;

        if (selectedShader != null)
            selectedShader.SetActive(false);

        targetScale = normalScale;
    }

    public void Deselect()
    {
        thisItemSelected = false;
        isTypingTitle = false;

        if (typingTitleCoroutine != null)
        {
            StopCoroutine(typingTitleCoroutine);
            typingTitleCoroutine = null;
        }

        if (selectedShader != null)
            selectedShader.SetActive(false);

        if (isHovered)
            targetScale = hoverScale;
        else
            targetScale = normalScale;
    }
}
