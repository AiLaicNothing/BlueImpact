using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text interactionText;

    private PlayerInputHandler playerInputHandler;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);

        // ✅ OBTENER REFERENCE AL INPUT HANDLER
        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
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

        interactionText.text = $"[{inputLabel}] {interactable.GetInteractionText()}";
    }

    public void Clear()
    {
        root.SetActive(false);
    }

    private bool isGamepadActive = false;

    private void Update()
    {
        // Detectar qué input se usó más recientemente
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            isGamepadActive = true;
        }
        else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
        {
            isGamepadActive = false;
        }
    }

    private string GetInputLabel()
    {
        if (isGamepadActive && Gamepad.current != null)
            return "△"; // Botón Y/Triangle

        return "E"; // Teclado
    }
}