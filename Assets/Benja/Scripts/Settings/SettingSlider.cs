using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private bool showPercentage = true;

    private System.Action<float> onValueChanged;

    private void Awake()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    public void Initialize(string label, float minValue, float maxValue, float currentValue, System.Action<float> callback)
    {
        if (labelText != null)
            labelText.text = label;

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = currentValue;

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
        if (valueText != null)
        {
            if (showPercentage)
                valueText.text = $"{(slider.value * 100):F0}%";
            else
                valueText.text = slider.value.ToString("F1");
        }
    }
}

// ==================== DROPDOWN SETTING ====================

public class SettingDropdown : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private TextMeshProUGUI valueText;

    private System.Action<int> onValueChanged;

    private void Awake()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    public void Initialize(string label, string[] options, int currentIndex, System.Action<int> callback)
    {
        if (labelText != null)
            labelText.text = label;

        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
        dropdown.value = currentIndex;

        onValueChanged = callback;
    }

    private void OnDropdownChanged(int index)
    {
        onValueChanged?.Invoke(index);
    }
}

// ==================== TOGGLE SETTING ====================

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

// ==================== KEYBIND BUTTON ====================

public class KeybindButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI keybindText;

    private string actionName;
    private int bindingIndex;
    private bool isListening = false;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void Initialize(string label, string action, int binding = 0)
    {
        if (labelText != null)
            labelText.text = label;

        actionName = action;
        bindingIndex = binding;
        RefreshDisplay();
    }

    private void OnButtonClicked()
    {
        if (!isListening)
        {
            StartListeningForInput();
        }
    }

    private async void StartListeningForInput()
    {
        isListening = true;
        if (keybindText != null)
            keybindText.text = "Presiona una tecla...";

        button.interactable = false;

        bool success = await InputRebindingManager.Instance.RemapActionAsync(actionName, bindingIndex);

        if (success)
        {
            RefreshDisplay();
        }

        button.interactable = true;
        isListening = false;
    }

    public void RefreshDisplay()
    {
        if (InputRebindingManager.Instance == null) return;

        string path = InputRebindingManager.Instance.GetBindingPath(actionName, bindingIndex);
        string displayName = InputRebindingManager.GetDisplayName(path);

        if (keybindText != null)
            keybindText.text = displayName;
    }
}