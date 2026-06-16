using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUIDisplay : MonoBehaviour
{
    [SerializeField] private StatDisplayItem healthDisplay;
    [SerializeField] private StatDisplayItem staminaDisplay;
    [SerializeField] private StatDisplayItem manaDisplay;
    [SerializeField] private StatDisplayItem physicalDamageDisplay;
    [SerializeField] private StatDisplayItem magicalDamageDisplay;

    [SerializeField] private float updateInterval = 0.1f;
    private float updateTimer;

    private void Start()
    {
        // Inicializar displays
        healthDisplay.Initialize(StatType.Vida, "Salud");
        staminaDisplay.Initialize(StatType.Estamina, "Resistencia");
        manaDisplay.Initialize(StatType.Maná, "Maná");
        physicalDamageDisplay.Initialize(StatType.DañoFísico, "Daño Físico");
        magicalDamageDisplay.Initialize(StatType.DañoMágico, "Daño Mágico");
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            RefreshAllStats();
            updateTimer = 0f;
        }
    }

    private void RefreshAllStats()
    {
        healthDisplay.UpdateDisplay();
        staminaDisplay.UpdateDisplay();
        manaDisplay.UpdateDisplay();
        physicalDamageDisplay.UpdateDisplay();
        magicalDamageDisplay.UpdateDisplay();
    }
}