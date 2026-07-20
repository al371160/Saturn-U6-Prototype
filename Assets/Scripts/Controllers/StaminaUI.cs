using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StaminaUI : MonoBehaviour
{
    public PlayerController player;
    public GameObject staminaUnitPrefab;
    public int staminaPerUnit = 10;

    private List<Image> staminaUnits = new List<Image>();
    private int previousUnitCount = -1;

    void Start()
    {
        GenerateStaminaUnits();
    }

    void Update()
    {
        if (player == null || staminaUnitPrefab == null)
            return;

        if (Mathf.CeilToInt(player.maxStamina / staminaPerUnit) != previousUnitCount)
        {
            GenerateStaminaUnits();
        }

        UpdateStaminaUI();
    }

    void GenerateStaminaUnits()
    {
        if (player == null || staminaUnitPrefab == null)
            return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        staminaUnits.Clear();

        int unitCount = Mathf.CeilToInt(player.maxStamina / staminaPerUnit);
        previousUnitCount = unitCount;

        for (int i = 0; i < unitCount; i++)
        {
            GameObject unit = Instantiate(staminaUnitPrefab, transform);
            Image img = unit.GetComponent<Image>();
            staminaUnits.Add(img);
        }
    }

    void UpdateStaminaUI()
    {
        if (player == null)
            return;

        int fullUnits = Mathf.FloorToInt(player.currentStamina / staminaPerUnit);

        for (int i = 0; i < staminaUnits.Count; i++)
        {
            float targetAlpha = i < fullUnits ? 1f : 0.2f;
            Image img = staminaUnits[i];
            Color c = img.color;

            // Smooth fade
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 10f);
            img.color = c;
        }
    }
}
