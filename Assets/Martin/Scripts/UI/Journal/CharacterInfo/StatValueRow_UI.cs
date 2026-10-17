using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatValueRow_UI : MonoBehaviour
{
    [SerializeField] private Image statIcon;
    [SerializeField] private TextMeshProUGUI statName;
    [SerializeField] private TextMeshProUGUI statValue;

    public void Initialize(Image icon, StatsType type, int value)
    {
        statIcon = icon;
        statName.text = GetStatNameByType(type);
        statValue.text = value.ToString();
    }

    public string GetStatNameByType(StatsType type)
    {
        return type switch
        {
            StatsType.Health => "vida",
            StatsType.Stamina => "Estamina",
            StatsType.Mana => "Mana",
            StatsType.Physical_Damage => "Físico",
            StatsType.Magical_Damage => "Mágico",

            _ => null
        };
    }
}
