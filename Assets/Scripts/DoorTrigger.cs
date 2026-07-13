using UnityEngine;
using System.Collections;
using TMPro;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openBoolName = "Open";
    [SerializeField] private bool canOpen = false;
    [SerializeField] private float autoCloseDelay = 3f; // ✅ how long door stays open

    [System.Serializable]
    public class PopupObject
    {
        public GameObject target;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    [Header("Popup Settings")]
    [SerializeField] private PopupObject[] popupObjects = new PopupObject[3];
    [SerializeField] private float popupHeight = 2f;
    [SerializeField] private float popupDuration = 1f;

    [Header("Text Message Settings")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string messageIfOpen = "The door opens.";
    [SerializeField] private string messageIfLocked = "The door is locked...";


    private Vector3[] startPositions;
    private Coroutine autoCloseCoroutine;
    private bool popupShown = false;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (messageText != null)
        {
            messageText.text = canOpen ? messageIfOpen : messageIfLocked;
            messageText.gameObject.SetActive(true);
        }

        StartPopup();
        if (canOpen && doorAnimator != null)
        {
            doorAnimator.SetBool(openBoolName, true);

            // ✅ Start auto-close coroutine
            if (autoCloseCoroutine != null)
                StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseDoorAfterDelay());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (messageText != null) 
        {
            messageText.gameObject.SetActive(false);

            ReversePopup();
        }
        
        // Close door if allowed
        if (canOpen && doorAnimator != null)
        {
            doorAnimator.SetBool(openBoolName, false);
        }
    }

    private IEnumerator AutoCloseDoorAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        ReversePopup();
    }

    public void AllowOpening()
    {
        canOpen = true;
    }

    private void StartPopup()
    {
        if (popupShown) return; // Don't show again if already shown
        popupShown = true;

        startPositions = new Vector3[popupObjects.Length];
        for (int i = 0; i < popupObjects.Length; i++)
        {
            if (popupObjects[i].target != null)
            {
                startPositions[i] = popupObjects[i].target.transform.position;
                StartCoroutine(PopupRoutine(popupObjects[i], startPositions[i], false));
            }
        }
    }

    private void ReversePopup()
    {
        if (!popupShown) return; // Don't reverse if nothing was shown
        popupShown = false;

        for (int i = 0; i < popupObjects.Length; i++)
        {
            if (popupObjects[i].target != null && startPositions != null && i < startPositions.Length)
            {
                StartCoroutine(PopupRoutine(popupObjects[i], startPositions[i], true));
            }
        }
    }



    private IEnumerator PopupRoutine(PopupObject popup, Vector3 basePosition, bool reverse)
    {
        float t = 0f;
        while (t < popupDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popupDuration);
            float evalTime = reverse ? 1f - normalized : normalized;
            float offsetY = popup.curve.Evaluate(evalTime) * popupHeight;

            if (popup.target != null)
            {
                popup.target.transform.position = basePosition + Vector3.up * offsetY;
            }

            yield return null;
        }

        if (popup.target != null)
        {
            popup.target.transform.position = reverse
                ? basePosition
                : basePosition + Vector3.up * popupHeight;
        }
    }

    
}
