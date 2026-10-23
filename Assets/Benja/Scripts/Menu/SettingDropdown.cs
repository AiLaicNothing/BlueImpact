using UnityEngine;
using TMPro;

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
        if (valueText != null)
            valueText.text = dropdown.options[index].text;

        onValueChanged?.Invoke(index);
    }
}