using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    public static OxygenSystem Instance { get; private set; }

    [Header("References")]
    public Transform player;

    [Header("Oxygen")]
    public float maxOxygen = 30f;
    public float currentOxygen = 30f;
    public float oxygenRegenRate = 5f;

    [Header("Altitude Zones")]
    public float groundLevelY = 5f;
    public float biomeZoneHeight = 50f;

    [Tooltip("Oxygen drain per second per biome above ground. Index 0 = first zone (50-100 units), etc.")]
    public float[] drainPerBiome = { 1f, 2f, 4f, 8f };

    // Applied by the equipped suit
    private float suitAltitudeBonus = 0f;

    public int CurrentBiome { get; private set; }
    public bool IsAtGroundLevel { get; private set; }
    public float OxygenFraction => currentOxygen / maxOxygen;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (player == null) return;
        Debug.Log($"Player Y: {player.position.y}, Suit Bonus: {suitAltitudeBonus}");

        float effectiveHeight = player.position.y - suitAltitudeBonus;
        IsAtGroundLevel = effectiveHeight <= groundLevelY;

        if (IsAtGroundLevel)
        {
            CurrentBiome = 0;
            currentOxygen = Mathf.Min(currentOxygen + oxygenRegenRate * Time.deltaTime, maxOxygen);
            return;
        }

        int altitudeBiome = Mathf.Max(0, Mathf.FloorToInt((effectiveHeight - groundLevelY) / biomeZoneHeight));
        CurrentBiome = altitudeBiome + 1;

        float drain = altitudeBiome < drainPerBiome.Length
            ? drainPerBiome[altitudeBiome]
            : drainPerBiome[drainPerBiome.Length - 1];

        currentOxygen = Mathf.Max(currentOxygen - drain * Time.deltaTime, 0f);
    }

    public void ApplySuit(float altitudeBonus)
    {
        suitAltitudeBonus = altitudeBonus;
    }

    public void RemoveSuit()
    {
        suitAltitudeBonus = 0f;
    }
}
