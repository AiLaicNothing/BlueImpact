using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private bool _isGrounded;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // Controles del New Input System por código
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private void Awake()    
    {
        _controller = GetComponent<CharacterController>();

        // Configuración de las acciones de movimiento (WASD / Stick izquierdo)
        _moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        _moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Configuración de la acción de salto (Espacio / Botón Sur del Gamepad)
        _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
    }

    private void Update()
    {
        // 1. Verificar si está en el suelo
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Mantiene al jugador pegado al suelo
        }

        // 2. Leer valores de movimiento
        _moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        _controller.Move(move * moveSpeed * Time.deltaTime);

        // 3. Detectar el Salto
        // Usamos WasPressedThisFrame para que solo actúe una vez por pulsación
        if (_jumpAction.WasPressedThisFrame() && _isGrounded)
        {
            // Fórmula física clásica para calcular la fuerza del salto: v = sqrt(h * -2 * g)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Aplicar Gravedad
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}