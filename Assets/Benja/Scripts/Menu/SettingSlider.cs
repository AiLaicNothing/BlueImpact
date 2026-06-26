using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SettingSlider - Wrapper de Slider con label, valor visible y formato configurable
/// </summary>
public class SettingSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;

    public enum DisplayFormat
    {
        Percentage,         // 0-1   → "75%"
        Percentage0to200,   // 0-2   → "100%" (brillo/contraste)
        Decimal,            // x.x   → "0.8"  (sensibilidad)
        DecimalTwoPlaces    // x.xx  → "1.50"
    }

    private DisplayFormat displayFormat = DisplayFormat.Percentage;
    private System.Action<float> onValueChanged;

    // Listener registrado UNA sola vez en Awake, nunca se borra.
    private void Awake()
    {
        if (slider != null)
            slider.onValueChanged.AddListener(OnSliderChanged);
    }

    // ================================================================
    //  INITIALIZE
    // ================================================================

    public void Initialize(string label, float minValue, float maxValue,
                           float currentValue, System.Action<float> callback)
        => Initialize(label, minValue, maxValue, currentValue, callback, DisplayFormat.Percentage);

    public void Initialize(string label, float minValue, float maxValue,
                           float currentValue, System.Action<float> callback,
                           DisplayFormat format)
    {
        if (labelText != null)
            labelText.text = label;

        displayFormat = format;
        onValueChanged = callback;

        if (slider != null)
        {
            slider.wholeNumbers = false;

            // Asignar rango ANTES del valor para que el handle se posicione correctamente
            slider.minValue = minValue;
            slider.maxValue = maxValue;

            // SetValueWithoutNotify: no dispara OnSliderChanged durante la inicialización
            float clamped = Mathf.Clamp(currentValue, minValue, maxValue);
            slider.SetValueWithoutNotify(clamped);
        }

        // Mostrar el texto correcto desde el primer frame sin esperar input del usuario
        UpdateValueDisplay();
    }

    // ================================================================
    //  CALLBACKS
    // ================================================================

    private void OnSliderChanged(float value)
    {
        UpdateValueDisplay();
        onValueChanged?.Invoke(value);
    }

    // ================================================================
    //  DISPLAY
    // ================================================================

    private void UpdateValueDisplay()
    {
        if (valueText == null || slider == null) return;

        valueText.text = displayFormat switch
        {
            DisplayFormat.Percentage => $"{slider.value * 100f:F0}%",
            DisplayFormat.Percentage0to200 => $"{slider.value * 100f:F0}%",
            DisplayFormat.Decimal => slider.value.ToString("F1"),
            DisplayFormat.DecimalTwoPlaces => slider.value.ToString("F2"),
            _ => slider.value.ToString("F1")
        };
    }

    // ================================================================
    //  ACCESO EXTERNO
    // ================================================================

    public void SetValueWithoutNotify(float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
        UpdateValueDisplay();
    }

    public float Value => slider != null ? slider.value : 0f;
}