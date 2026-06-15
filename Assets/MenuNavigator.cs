using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuNavigator : MonoBehaviour
{
    [SerializeField] private Button firstButton;
    [SerializeField] private EventSystem eventSystem;

    [SerializeField] private float navigationCooldown = 0.2f;
    private float lastNavigationTime = 0f;

    private void Start()
    {
        // Asegurar que el primer botón está seleccionado
        if (firstButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(firstButton.gameObject);
        }
        else if (firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private void Update()
    {
        // Detectar entrada de navegación
        DetectNavigationInput();
    }

    private void DetectNavigationInput()
    {
        if (Time.time - lastNavigationTime < navigationCooldown)
            return;

        Vector2 input = Vector2.zero;

        // Teclado
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            input = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            input = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            input = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            input = Vector2.right;

        // Gamepad
        if (Gamepad.current != null)
        {
            var leftStick = Gamepad.current.leftStick.ReadValue();
            if (leftStick.y > 0.5f) input = Vector2.up;
            else if (leftStick.y < -0.5f) input = Vector2.down;
            else if (leftStick.x < -0.5f) input = Vector2.left;
            else if (leftStick.x > 0.5f) input = Vector2.right;

            // D-Pad
            if (Gamepad.current.dpad.up.isPressed) input = Vector2.up;
            else if (Gamepad.current.dpad.down.isPressed) input = Vector2.down;
            else if (Gamepad.current.dpad.left.isPressed) input = Vector2.left;
            else if (Gamepad.current.dpad.right.isPressed) input = Vector2.right;
        }

        if (input != Vector2.zero)
        {
            Navigate(input);
            lastNavigationTime = Time.time;
        }
    }

    private void Navigate(Vector2 direction)
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem.currentSelectedGameObject;

        if (selected == null) return;

        Selectable selectable = selected.GetComponent<Selectable>();
        if (selectable == null) return;

        Selectable next = null;

        // Navegar según la dirección
        if (direction == Vector2.up)
            next = selectable.FindSelectableOnUp();
        else if (direction == Vector2.down)
            next = selectable.FindSelectableOnDown();
        else if (direction == Vector2.left)
            next = selectable.FindSelectableOnLeft();
        else if (direction == Vector2.right)
            next = selectable.FindSelectableOnRight();

        // Si no hay siguiente, no hacer nada (permitir loops si se configura en Navigation)
        if (next != null)
        {
            eventSystem.SetSelectedGameObject(next.gameObject);
        }
    }

    /// <summary>
    /// Forzar selección de un botón específico
    /// </summary>
    public void SelectButton(Button button)
    {
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    /// <summary>
    /// Resetear a selección inicial
    /// </summary>
    public void ResetSelection()
    {
        if (firstButton != null)
        {
            SelectButton(firstButton);
        }
    }
}
