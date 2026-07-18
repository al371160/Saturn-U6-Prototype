using TMPro;
using UnityEngine;

public class SurvivorDamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.6f;
    private const float RiseSpeed = 1.4f;

    /// <summary>Set once by SurvivorMinigameController from config.levelUpFont so popups match the upgrade UI.</summary>
    public static TMP_FontAsset SharedFont;

    private TextMeshPro label;
    private float age;
    private Color baseColor;

    public static SurvivorDamagePopup Create(Vector3 worldPosition, float amount, Color color)
    {
        GameObject popupObject = new GameObject("DamagePopup");
        popupObject.transform.position = worldPosition;

        SurvivorDamagePopup popup = popupObject.AddComponent<SurvivorDamagePopup>();
        popup.Initialize(amount, color);
        return popup;
    }

    private void Initialize(float amount, Color color)
    {
        label = gameObject.AddComponent<TextMeshPro>();
        label.text = Mathf.RoundToInt(amount).ToString();
        label.fontSize = 3.5f;
        if (SharedFont != null)
            label.font = SharedFont;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        baseColor = color;

        // Was localScale 0.5 — doubling the on-screen size per user request.
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        if (label != null)
        {
            float alpha = 1f - Mathf.Clamp01(age / Lifetime);
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (age >= Lifetime)
            Destroy(gameObject);
    }
}
