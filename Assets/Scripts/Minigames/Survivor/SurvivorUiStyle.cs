using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared contrast system for Survivor UI cards/prompts:
/// dark cards use white text; light cards use black text.
/// </summary>
public static class SurvivorUiStyle
{
    public static readonly Color DarkPanel = new Color(0.08f, 0.09f, 0.11f, 0.94f);
    public static readonly Color LightPanel = new Color(0.96f, 0.97f, 0.99f, 0.96f);
    public static readonly Color DarkPanelBorder = new Color(1f, 1f, 1f, 0.18f);
    public static readonly Color LightPanelBorder = new Color(0f, 0f, 0f, 0.22f);

    public static readonly Color TextOnDark = Color.white;
    public static readonly Color TextOnDarkMuted = new Color(0.88f, 0.9f, 0.94f, 1f);
    public static readonly Color TextOnLight = new Color(0.06f, 0.07f, 0.09f, 1f);
    public static readonly Color TextOnLightMuted = new Color(0.18f, 0.2f, 0.24f, 1f);

    public static void ApplyDarkCard(Image panel, params TMP_Text[] labels)
    {
        if (panel != null)
            panel.color = DarkPanel;

        for (int i = 0; i < labels.Length; i++)
            ApplyTextOnDark(labels[i], muted: i > 0);
    }

    public static void ApplyLightCard(Image panel, params TMP_Text[] labels)
    {
        if (panel != null)
            panel.color = LightPanel;

        for (int i = 0; i < labels.Length; i++)
            ApplyTextOnLight(labels[i], muted: i > 0);
    }

    public static void ApplyTextOnDark(TMP_Text label, bool muted = false)
    {
        if (label == null)
            return;
        label.color = muted ? TextOnDarkMuted : TextOnDark;
        label.outlineWidth = 0f;
    }

    public static void ApplyTextOnLight(TMP_Text label, bool muted = false)
    {
        if (label == null)
            return;
        label.color = muted ? TextOnLightMuted : TextOnLight;
        label.outlineWidth = 0f;
    }

    public static void ApplyFont(TMP_Text label, TMP_FontAsset font)
    {
        if (label != null && font != null)
            label.font = font;
    }
}
