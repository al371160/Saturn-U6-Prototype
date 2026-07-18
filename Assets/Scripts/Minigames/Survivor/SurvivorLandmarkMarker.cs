using UnityEngine;

/// <summary>
/// Drop on any POI root to make it show up on SurvivorMinimapUI. Self-registers/unregisters so the
/// minimap doesn't need a hand-maintained list — just tag whatever should read as a landmark.
/// </summary>
public class SurvivorLandmarkMarker : MonoBehaviour
{
    public string displayName = "Landmark";
    public Color mapColor = Color.white;

    private void OnEnable()
    {
        SurvivorMinimapUI.RegisterLandmark(this);
    }

    private void OnDisable()
    {
        SurvivorMinimapUI.UnregisterLandmark(this);
    }
}
