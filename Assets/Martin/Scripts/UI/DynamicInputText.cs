using TMPro;
using UnityEngine;

public class DynamicInputText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;

    private string original_Text;
    private InputTextFormatter formatter;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshText();
    }

    private void OnDisable()
    {
        if (formatter != null) formatter.OnBindingChanged -= RefreshText;

        formatter = null;
    }

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();

        original_Text = textComponent != null ? textComponent.text : "";
    }

    private void TrySubscribe()
    {
        InputTextFormatter current = InputTextFormatter.Instance;

        if (formatter == current) return;

        if (formatter != null) formatter.OnBindingChanged -= RefreshText;

        formatter = current;

        if (formatter != null) formatter.OnBindingChanged += RefreshText;
    }

    public void SetTemplate(string text)
    {
        original_Text = text ?? "";

        if (isActiveAndEnabled) TrySubscribe();

        RefreshText();
    }

    public void RefreshText()
    {
        if (textComponent == null) return;

        InputTextFormatter current = InputTextFormatter.Instance;

        textComponent.text = current != null? current.Format(original_Text) : original_Text;
    }
}
