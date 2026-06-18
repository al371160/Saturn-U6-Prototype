using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class SpeechBubbleResizer : MonoBehaviour
{
    public TextMeshProUGUI textLabel;           // Assign in Inspector
    public RectTransform bubblePanel;           // The background panel (can be same as this object)
    public Vector2 padding = new Vector2(40f, 20f);  // Horizontal and vertical padding
    public bool animateResize = false;
    public float animationSpeed = 10f;

    private Vector2 targetSize;

    void Awake()
    {
        if (bubblePanel == null)
            bubblePanel = GetComponent<RectTransform>();
    }

    public void ResizeToFitText()
    {
        // Force text update to get accurate size
        textLabel.ForceMeshUpdate();
        Vector2 textSize = textLabel.GetRenderedValues(false);

        // Calculate new size with padding
        targetSize = textSize + padding;

        if (!animateResize)
        {
            bubblePanel.sizeDelta = targetSize;
        }
    }

    void Update()
    {
        if (animateResize)
        {
            bubblePanel.sizeDelta = Vector2.Lerp(bubblePanel.sizeDelta, targetSize, Time.deltaTime * animationSpeed);
        }
    }
}
