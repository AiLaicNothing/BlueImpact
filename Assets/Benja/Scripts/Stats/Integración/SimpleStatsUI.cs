using UnityEngine;
using TMPro;

public class SimpleStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private PlayerStatsManager playerStatsManager;

    private void Start()
    {
        if (playerStatsManager == null)
        {
            playerStatsManager = FindFirstObjectByType<PlayerStatsManager>();
        }
    }

    private void Update()
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        if (statsText == null || playerStatsManager == null) return;

        // ✅ Valor actual / Máximo actual
        int healthActual = playerStatsManager.GetActualValue(StatType.Health);
        int healthMax = playerStatsManager.GetMaxValue(StatType.Health);

        int staminaActual = playerStatsManager.GetActualValue(StatType.Stamina);
        int staminaMax = playerStatsManager.GetMaxValue(StatType.Stamina);

        int manaActual = playerStatsManager.GetActualValue(StatType.Mana);
        int manaMax = playerStatsManager.GetMaxValue(StatType.Mana);

        // ✅ Estos no se consumen, son stats puras
        int physDmg = playerStatsManager.GetMaxValue(StatType.PhysicalDamage);
        int magDmg = playerStatsManager.GetMaxValue(StatType.MagicalDamage);

        statsText.text = $@"
SALUD: {healthActual}/{healthMax}
STAMINA: {staminaActual}/{staminaMax}
MANA: {manaActual}/{manaMax}
DMG FÍSICO: {physDmg}
DMG MÁGICO: {magDmg}
";
    }
}