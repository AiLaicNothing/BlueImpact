using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingToggle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Toggle toggle;

    private System.Action<bool> onValueChanged;

    private void Awake()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    public void Initialize(string label, bool currentValue, System.Action<bool> callback)
    {
        if (labelText != null)
            labelText.text = label;

        toggle.isOn = currentValue;
        onValueChanged = callback;
    }

    private void OnToggleChanged(bool value)
    {
        onValueChanged?.Invoke(value);
    }
}