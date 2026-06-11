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
        if (interactable == null)
        {
            Clear();
            return;
        }

        root.SetActive(true);

        string inputLabel = GetInputLabel();

        interactionText.text =
            $"[{inputLabel}] {interactable.GetInteractionText()}";
    }

    public void Clear()
    {
        root.SetActive(false);
    }

    private string GetInputLabel()
    {
        // Luego puedes detectar automáticamente teclado/mando.
        return "E";
    }
}