using UnityEngine;
using UnityEngine.UI;
using TMPro;

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