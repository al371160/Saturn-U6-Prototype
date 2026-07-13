using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OxygenUI : MonoBehaviour
{
    [Header("References")]
    public OxygenSystem oxygenSystem;

    [Header("UI Elements")]
    public Image oxygenIcon;
    public Image oxygenBarFill;
    public TextMeshProUGUI oxygenText;

    [Header("Bar Colors")]
    public Color fullColor = new Color(0.3f, 0.8f, 1f);
    public Color lowColor = Color.red;

    [Tooltip("Fraction below which the bar starts turning red.")]
    public float lowThreshold = 0.35f;

    void Update()
    {
        if (oxygenSystem == null) return;

        float fraction = oxygenSystem.OxygenFraction;

        if (oxygenBarFill != null)
        {
            oxygenBarFill.fillAmount = fraction;
            float t = Mathf.InverseLerp(0f, lowThreshold, fraction);
            oxygenBarFill.color = Color.Lerp(lowColor, fullColor, t);
        }

        if (oxygenText != null)
            oxygenText.text = Mathf.CeilToInt(oxygenSystem.currentOxygen).ToString();

        // Hide the whole panel when at full oxygen and at ground level to keep the screen clean
        bool shouldShow = !oxygenSystem.IsAtGroundLevel || oxygenSystem.currentOxygen < oxygenSystem.maxOxygen;
        gameObject.SetActive(shouldShow);
    }
}
