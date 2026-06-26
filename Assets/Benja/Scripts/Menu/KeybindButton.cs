using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// KeybindButton - Botón individual de remapeo con manejo de timing
/// 
/// ✅ Espera a que InputRebindingManager esté disponible
/// ✅ Muestra feedback si hay conflicto
/// ✅ Botón de reset individual
/// </summary>
public class KeybindButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI keybindText;
    [SerializeField] private Button resetButton;

    private string actionName;
    private int bindingIndex;
    private bool isListening = false;

    private Color normalColor = Color.white;
    private Color conflictColor = new Color(1f, 0.3f, 0.3f);

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
    }

    /// <summary>
    /// Inicializa el botón y espera a que InputRebindingManager esté listo
    /// antes de mostrar el binding.
    /// </summary>
    public void Initialize(string label, string action, int binding = 0)
    {
        if (labelText != null)
            labelText.text = label;

        actionName = action;
        bindingIndex = binding;

        // Esperar a que InputRebindingManager exista antes de mostrar el texto
        // (puede no estar listo si se inicia desde escena GAMEPLAY)
        StartCoroutine(WaitAndRefreshDisplay());
    }

    private IEnumerator WaitAndRefreshDisplay()
    {
        // Esperar hasta que InputRebindingManager esté disponible (máx 10 frames)
        int attempts = 0;
        while (InputRebindingManager.Instance == null && attempts < 10)
        {
            yield return null;
            attempts++;
        }

        if (InputRebindingManager.Instance == null)
        {
            Debug.LogError($"❌ InputRebindingManager no encontrado después de 10 frames. KeybindButton {labelText?.text} quedará vacío.");
            yield break;
        }

        RefreshDisplay();
    }

    private void OnButtonClicked()
    {
        if (!isListening)
            StartListeningForInput();
    }

    private void OnResetClicked()
    {
        if (InputRebindingManager.Instance == null)
        {
            Debug.LogWarning("⚠️ InputRebindingManager no disponible para reset");
            return;
        }

        InputRebindingManager.Instance.ResetActionBinding(actionName);
        RefreshDisplay();
        Debug.Log($"✅ Binding reseteado: {actionName}");
    }

    private async void StartListeningForInput()
    {
        if (InputRebindingManager.Instance == null)
        {
            Debug.LogError("❌ No se puede reasignar — InputRebindingManager no encontrado");
            return;
        }

        isListening = true;
        button.interactable = false;

        if (keybindText != null)
        {
            keybindText.color = normalColor;
            keybindText.text = "Presiona una tecla...";
        }

        var (success, conflictMessage) = await InputRebindingManager.Instance.RemapActionAsync(actionName, bindingIndex);

        if (success)
        {
            RefreshDisplay();
        }
        else if (conflictMessage != null)
        {
            StartCoroutine(ShowConflictFeedback(conflictMessage));
        }
        else
        {
            RefreshDisplay();
        }

        button.interactable = true;
        isListening = false;
    }

    private IEnumerator ShowConflictFeedback(string conflictActionName)
    {
        if (keybindText != null)
        {
            string shortName = conflictActionName.Contains("/")
                ? conflictActionName.Split('/')[1]
                : conflictActionName;

            keybindText.color = conflictColor;
            keybindText.text = $"Ya usado: {shortName}";
        }

        yield return new WaitForSeconds(2f);
        RefreshDisplay();

        if (keybindText != null)
            keybindText.color = normalColor;
    }

    public void RefreshDisplay()
    {
        if (InputRebindingManager.Instance == null)
        {
            Debug.LogWarning($"⚠️ RefreshDisplay: InputRebindingManager aún no listo para '{actionName}'");
            return;
        }

        string path = InputRebindingManager.Instance.GetBindingPath(actionName, bindingIndex);
        string displayName = InputRebindingManager.GetDisplayName(path);

        if (keybindText != null)
        {
            keybindText.color = normalColor;
            keybindText.text = displayName;
        }
    }
}