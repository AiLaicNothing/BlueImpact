using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] private GameObject root;

    [SerializeField] private TMP_Text interactionText;

    private void Awake()
    {
        Instance = this;

        root.SetActive(false);
    }

    public void SetInteractable(IInteractable interactable)
    {
        bool show = interactable != null;

        root.SetActive(show);

        if (!show)
            return;

        string inputLabel = GetInputLabel();

        interactionText.text =
            $"[{inputLabel}] {interactable.GetInteractionText()}";
    }

    private string GetInputLabel()
    {
        // Por ahora fijo.
        // Luego detectaremos mando automáticamente.
        return "E";
    }
}