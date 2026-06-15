using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;

    // Formato de visualización
    public enum DisplayFormat
    {
        Percentage,      // 0-100%
        Decimal,         // 0.1, 0.2, 0.3
        DecimalTwoPlaces // 0.10, 0.20, 0.30
    }

    private DisplayFormat displayFormat = DisplayFormat.Percentage;
    private System.Action<float> onValueChanged;

    private void Awake()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    /// <summary>
    /// Inicializa el slider con formato de porcentaje
    /// </summary>
    public void Initialize(string label, float minValue, float maxValue, float currentValue, System.Action<float> callback)
    {
        Initialize(label, minValue, maxValue, currentValue, callback, DisplayFormat.Percentage);
    }

    /// <summary>
    /// Inicializa el slider con formato especificado
    /// </summary>
    public void Initialize(string label, float minValue, float maxValue, float currentValue, System.Action<float> callback, DisplayFormat format)
    {
        if (labelText != null)
            labelText.text = label;

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = currentValue;

        displayFormat = format;
        onValueChanged = callback;
        UpdateValueDisplay();
    }

    private void OnSliderChanged(float value)
    {
        UpdateValueDisplay();
        onValueChanged?.Invoke(value);
    }

    private void UpdateValueDisplay()
    {
        if (valueText == null) return;

        string displayValue = "";

        switch (displayFormat)
        {
            case DisplayFormat.Percentage:
                // Muestra como porcentaje (0-100%)
                displayValue = $"{(slider.value * 100):F0}%";
                break;

            case DisplayFormat.Decimal:
                // Muestra con 1 decimal (0.1, 0.2, 0.3)
                displayValue = slider.value.ToString("F1");
                break;

            case DisplayFormat.DecimalTwoPlaces:
                // Muestra con 2 decimales (0.10, 0.20, 0.30)
                displayValue = slider.value.ToString("F2");
                break;

            default:
                displayValue = slider.value.ToString("F1");
                break;
        }

        valueText.text = displayValue;
    }
}