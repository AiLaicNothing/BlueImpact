using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatDisplayItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image statBar;
    [SerializeField] private Image statBarBackground;
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private TextMeshProUGUI statPercentText;

    [Header("Colors")]
    [SerializeField] private Color healthColor = new Color(0.2f, 0.8f, 0.2f); // Verde
    [SerializeField] private Color staminaColor = new Color(1f, 0.84f, 0f); // Amarillo
    [SerializeField] private Color manaColor = new Color(0.2f, 0.6f, 1f); // Azul
    [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f); // Rojo

    private StatType statType;
    private string displayName;

    public void Initialize(StatType type, string name)
    {
        statType = type;
        displayName = name;

        // Configurar nombre
        if (statNameText != null)
            statNameText.text = displayName;

        // Configurar color según tipo
        Color color = GetColorForStatType(type);
        if (statBar != null)
            statBar.color = color;
    }

    public void UpdateDisplay()
    {
        StatInfo info = StatsManager.Instance.GetStatInfo(statType);

        // Actualizar barra de progreso
        if (statBar != null)
        {
            statBar.fillAmount = info.percentage;
        }

        // Actualizar texto de valores
        if (statValueText != null)
        {
            // Si es un stat regenerable (Health, Stamina, Mana)
            if (statType == StatType.Health || statType == StatType.Stamina || statType == StatType.Mana)
            {
                statValueText.text = $"{info.currentValue}/{info.maxValue}";
            }
            else
            {
                // Stats fijos como Damage
                statValueText.text = $"{info.currentValue}";
            }
        }

        // Actualizar porcentaje
        if (statPercentText != null)
        {
            if (statType == StatType.Health || statType == StatType.Stamina || statType == StatType.Mana)
            {
                statPercentText.text = $"{info.percentage * 100:F0}%";
            }
            else
            {
                statPercentText.text = "";
            }
        }
    }

    private Color GetColorForStatType(StatType type)
    {
        return type switch
        {
            StatType.Health => healthColor,
            StatType.Stamina => staminaColor,
            StatType.Mana => manaColor,
            StatType.PhysicalDamage => damageColor,
            StatType.MagicalDamage => damageColor,
            _ => Color.white
        };
    }
}