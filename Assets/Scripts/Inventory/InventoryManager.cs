using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public UIManager uiManager;
    public GameObject inventoryMenu;
    public bool menuActivated = false;
    public ItemSlot[] itemSlot;
    public ItemSO[] itemSOs;

    public BubblePopAnimation bubblePop1;
    public BubblePopAnimation bubblePop2;
    public GaussianBlurController blurController;

    public CanvasGroup darkBackground;

    public PlayerController playerController;

    [Header("Item Banner")]
    public GameObject itemBanner;
    public TMPro.TMP_Text bannerTitle;
    public TMPro.TMP_Text bannerDescription;
    public CanvasGroup itemBannerCanvasGroup;
    public float bannerDisplayDuration = 4f; // Duration the banner stays visible
    public BubblePopAnimation bannerPopAnimation;

    [Header("Small Item Banner")]
    public GameObject smallItemBanner; // Reference to a pre-existing small banner in the scene
    public TMPro.TMP_Text smallBannerText;
    public CanvasGroup smallBannerCanvasGroup;

    private Coroutine backgroundFadeCoroutine;

    public float smallBannerDisplayDuration = 2f;

    private Coroutine smallBannerCoroutine;


    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Inventory"))
        {
            if (uiManager != null)
                uiManager.ClearUI();
            if (!menuActivated)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }
    }

    private void OpenInventory()
    {
        inventoryMenu.SetActive(true);
        if (bubblePop1 != null)
            bubblePop1.PlayPop();
        if (bubblePop2 != null)
            bubblePop2.PlayPop();
        menuActivated = true;

        // Slow down time if you want
        // Time.timeScale = 0.2f;

        // Single open SFX — prefer hub, else local clip (never both).
        if (PlayerAudioHub.Instance != null)
            PlayerAudioHub.Instance.PlayLibrary(lib => lib.inventoryOpen != null ? lib.inventoryOpen : lib.uiSelect);
        else if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);

        // Dark overlay only — Global Volume owns depth of field.
        if (darkBackground != null)
        {
            if (backgroundFadeCoroutine != null)
                StopCoroutine(backgroundFadeCoroutine);

            backgroundFadeCoroutine = StartCoroutine(FadeBackground(0f, 0.5f, 0.2f));
        }

    }

    public void CloseInventory()
    {
        inventoryMenu.SetActive(false);
        menuActivated = false;

        // Single close SFX — prefer hub, else local clip (never both).
        if (PlayerAudioHub.Instance != null)
            PlayerAudioHub.Instance.PlayLibrary(lib => lib.inventoryClose != null ? lib.inventoryClose : lib.uiHover);
        else if (closeSound != null && audioSource != null)
            audioSource.PlayOneShot(closeSound);

        // Dark overlay only — Global Volume owns depth of field.
        if (darkBackground != null)
        {
            if (backgroundFadeCoroutine != null)
                StopCoroutine(backgroundFadeCoroutine);

            backgroundFadeCoroutine = StartCoroutine(FadeBackground(0.5f, 0f, 0.2f));
        }

        // Ensure banner is hidden when inventory closes
        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
            bannerCoroutine = null;
        }

        if (itemBanner != null)
            itemBanner.SetActive(false);
        if (itemBannerCanvasGroup != null)
            itemBannerCanvasGroup.alpha = 0f;
        if (itemBanner != null)
            itemBanner.transform.localScale = Vector3.zero;
    }


    private IEnumerator FadeBackground(float startAlpha, float endAlpha, float duration)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // Unscaled so the fade ignores slow time
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            darkBackground.alpha = alpha;
            yield return null;
        }

        darkBackground.alpha = endAlpha;
    }

    private Coroutine bannerCoroutine;

    public void ShowItemBanner(string itemName, string itemDescription)
    {
        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
        }

        itemBanner.SetActive(true);
        itemBannerCanvasGroup.alpha = 1f;
        //itemBanner.transform.localScale = Vector3.one;

        bannerTitle.text = $"'{itemName}'!";
        bannerDescription.text = itemDescription;

        if (bannerPopAnimation != null)
        {
            bannerPopAnimation.PlayPop(); // Play pop-in when banner appears
        }

        bannerCoroutine = StartCoroutine(FadeOutBannerWithPop(bannerDisplayDuration, 0.5f)); // 0.5s fade duration
    }


    private IEnumerator FadeOutBannerWithPop(float waitTime, float fadeDuration)
    {
        yield return new WaitForSeconds(waitTime);

        float t = 0f;
        Vector3 initialScale = itemBanner.transform.localScale;

        while (t < fadeDuration)
        {
            float progress = t / fadeDuration;

            // Shrink using the pop curve in reverse
            float scaleX = bannerPopAnimation.popCurveX.Evaluate(1f - progress);
            float scaleY = bannerPopAnimation.popCurveY.Evaluate(1f - progress);
            itemBanner.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Fade out
            itemBannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure final state
        itemBannerCanvasGroup.alpha = 0f;
        itemBanner.transform.localScale = Vector3.zero;
        itemBanner.SetActive(false);
    }

    public void ShowSmallItemBanner(string itemName, int quantityGained, int startQuantity)
    {
        if (smallBannerCoroutine != null)
        {
            StopCoroutine(smallBannerCoroutine);
        }

        smallItemBanner.SetActive(true);
        smallBannerCanvasGroup.alpha = 1f;

        // Start the counting coroutine
        smallBannerCoroutine = StartCoroutine(AnimateItemCount(itemName, startQuantity, startQuantity + quantityGained, smallBannerDisplayDuration));
    }

    private IEnumerator AnimateItemCount(string itemName, int startQuantity, int targetQuantity, float displayTime)
    {
        float countDuration = 0.5f; // Time to count up
        float t = 0f;

        while (t < countDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / countDuration);
            int currentCount = Mathf.RoundToInt(Mathf.Lerp(startQuantity, targetQuantity, progress));
            smallBannerText.text = $"{itemName} {currentCount}";
            yield return null;
        }

        // Ensure the final value is shown
        smallBannerText.text = $"{itemName} {targetQuantity}";

        // Wait before starting the fade out
        yield return new WaitForSeconds(displayTime);

        float fadeDuration = 0.5f;
        t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            smallBannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        smallItemBanner.SetActive(false);
    }





    public void UseItem(string itemName)
    {
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if (itemSOs[i].itemName == itemName && itemSOs[i].usable == true)
            {
                itemSOs[i].UseItem();
            }
        }
    }

    public void EquipItem(string itemName)
    {
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if (itemSOs[i].itemName == itemName && itemSOs[i].usable == false)
            {
                itemSOs[i].EquipItem();
            }
        }
    }

    public int AddItem(string itemName, int quantity, Sprite itemSprite, string itemDescription)
    {
        Debug.Log(itemName + " " + quantity + " " + itemSprite);
        int remaining = quantity;

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if ((itemSlot[i].itemName == itemName && !itemSlot[i].isFull) || itemSlot[i].quantity == 0)
            {
                remaining = itemSlot[i].AddItem(itemName, remaining, itemSprite, itemDescription);

                ItemSO item = GetItemSO(itemName);

                if (item != null)
                {
                    // If item is important, play animation
                    if (item.important)
                    {
                        playerController.PlayImportantItemAnimation();
                        ShowItemBanner(itemName, itemDescription);
                    }

                    // Specific case for Power Core
                    if (item.itemName == "Power Core" && item.usable == false)
                    {
                        item.UseItem();
                    }
                }

                if (remaining <= 0)
                {
                    // Calculate total quantity BEFORE adding this batch
                    int totalQuantityBefore = 0;
                    foreach (var slot in itemSlot)
                    {
                        if (slot.itemName == itemName)
                        {
                            totalQuantityBefore += slot.quantity;
                        }
                    }

                    // Show smooth counter banner
                    ShowSmallItemBanner(itemName, quantity, totalQuantityBefore - quantity); // Start from BEFORE current batch

                    return 0;
                }

            }
        }

        return remaining;
    }


    public bool HasItem(string itemName, int quantity)
    {
        foreach (var slot in itemSlot)
        {
            if (slot.itemName == itemName && slot.quantity >= quantity)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets or updates a slot by name with no banners, toasts, or Power Core side effects.
    /// Returns false if inventory is full and the item was not already present.
    /// </summary>
    public bool UpsertItemQuiet(string itemName, int quantity, Sprite itemSprite, string itemDescription)
    {
        if (string.IsNullOrEmpty(itemName) || itemSlot == null)
            return false;

        quantity = Mathf.Max(0, quantity);
        if (quantity == 0)
        {
            RemoveItemCompletely(itemName);
            return true;
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].itemName == itemName)
            {
                itemSlot[i].SetContents(itemName, quantity, itemSprite, itemDescription);
                return true;
            }
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].quantity == 0)
            {
                itemSlot[i].SetContents(itemName, quantity, itemSprite, itemDescription);
                return true;
            }
        }

        Debug.LogWarning($"InventoryManager.UpsertItemQuiet: no free slot for '{itemName}'.");
        return false;
    }

    public void RemoveItemCompletely(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || itemSlot == null)
            return;

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].itemName == itemName)
                itemSlot[i].EmptySlot();
        }
    }

    public void RemoveItems(IEnumerable<string> itemNames)
    {
        if (itemNames == null)
            return;

        foreach (string itemName in itemNames)
            RemoveItemCompletely(itemName);
    }

    public void DeselectAllSlots()
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].Deselect();
        }
    }

    /// <summary>Selects the first populated slot if nothing is already selected, so the shared
    /// description panel isn't left blank after a quiet batch sync (e.g. Survivor loadout via
    /// UpsertItemQuiet) that never routes through a slot's OnPointerClick.</summary>
    public void EnsureSlotSelected()
    {
        if (itemSlot == null)
            return;

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && itemSlot[i].thisItemSelected)
                return;
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && itemSlot[i].quantity > 0)
            {
                itemSlot[i].SelectSlot();
                return;
            }
        }
    }


    public ItemSO GetItemSO(string itemName)
    {
        foreach (var item in itemSOs)
        {
            if (item.itemName == itemName)
                return item;
        }
        return null;
    }
}
