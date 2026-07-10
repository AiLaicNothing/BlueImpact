using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CheckpointMenuUI : MonoBehaviour, IGamepadPanel
{
    public static CheckpointMenuUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainPanelCanvasGroup;
    [SerializeField] private GameObject mainPanel;

    [SerializeField] private TeleportPanelUI teleportPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private CheckpointStatsPanel checkpointStatsPanel;

    [SerializeField] private GameObject skillsPanel;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button closeButton;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    // ✅ CALLBACKS PARA EVENTOS
    public event Action OnTravelPressed;
    public event Action OnStatsPressed;
    public event Action OnSkillsPressed;
    public event Action OnClosePressed;
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;

    private Checkpoint currentCheckpoint;
    private bool isOpen = false;
    public bool IsOpen() => isOpen;

    // ✅ CONTROL DE FOCO
    private bool inputEnabled = true;

    private void Awake()
    {
        Instance = this;

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (mainPanelCanvasGroup == null)
            mainPanelCanvasGroup = mainPanel.GetComponent<CanvasGroup>();

        CloseAllPanels();

        travelButton.onClick.AddListener(() => OnTravelButtonClicked());
        statsButton.onClick.AddListener(() => OnStatsButtonClicked());
        skillsButton.onClick.AddListener(() => OnSkillsButtonClicked());
        closeButton.onClick.AddListener(() => OnCloseButtonClicked());
    }

    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!isOpen || mainPanelCanvasGroup.interactable == false || !inputEnabled)
            return;

        HandleGamepadInput();
    }

    private void HandleGamepadInput()
    {
        // ✅ MOVER CURSOR CON JOYSTICK (Solo en este panel)
        HandleJoystickCursorMovement();

        // ✅ BOTONES DE SISTEMA (A, B)
        HandleGamepadButtonInputs();
    }

    /// <summary>
    /// ✅ Mover el cursor con el Left Stick del gamepad
    /// </summary>
    private void HandleJoystickCursorMovement()
    {
        if (Gamepad.current == null)
            return;

        Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

        // Solo mover si hay input significativo
        if (stickInput.magnitude > 0.05f)
        {
            // ✅ Usar Input.mousePosition
            Vector3 currentCursorPos = Input.mousePosition;

            // Calcular movimiento CON unscaledDeltaTime
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            // ✅ MOVER CURSOR
#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                Mouse.current.WarpCursorPosition(newCursorPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Joystick] Error moviendo cursor: {e.Message}");
            }
#endif
        }
    }

    /// <summary>
    /// ✅ Manejar botones del gamepad (A, B)
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // ✅ A Button = Click en botón bajo cursor
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            SimulateClickAtCursor();
        }

        // ✅ B Button = Cerrar menú
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnCloseButtonClicked();
            return;
        }
    }

    /// <summary>
    /// ✅ Simular click en elemento bajo cursor
    /// </summary>
    private void SimulateClickAtCursor()
    {
        if (eventSystem == null)
        {
            Debug.LogError("[SimulateClick] EventSystem es NULL");
            return;
        }

        // Raycast desde posición del cursor
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        Debug.Log($"[SimulateClick] Raycast encontró {results.Count} objetos");

        // ✅ Buscar el PRIMER BUTTON en los resultados (no el primer objeto)
        foreach (RaycastResult hit in results)
        {
            Button button = hit.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                Debug.Log($"✅ Click simulado en: {button.gameObject.name}");
                return;
            }

            // Si no tiene Button, buscar en padre
            Button parentButton = hit.gameObject.GetComponentInParent<Button>();
            if (parentButton != null && parentButton.interactable)
            {
                parentButton.onClick.Invoke();
                Debug.Log($"✅ Click simulado en padre: {parentButton.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("[SimulateClick] No se encontró ningún Button clickeable");
    }

    public void ShowMainPanel()
    {
        CloseAllPanels();
        mainPanel.SetActive(true);
        isOpen = true;

        if (mainPanelCanvasGroup != null)
            mainPanelCanvasGroup.interactable = true;

        // ✅ Deseleccionar para modo mouse/joystick (sin selección por default)
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);

        // ✅ REGISTRAR ESTE PANEL COMO ACTIVO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PushPanel(this);
    }

    public void Open(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.UI);

        CloseAllPanels();
        mainPanel.SetActive(true);
        isOpen = true;

        if (mainPanelCanvasGroup != null)
            mainPanelCanvasGroup.interactable = true;

        Time.timeScale = 0f;

        // ✅ Deseleccionar para modo mouse/joystick (sin selección por default)
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);

        // ✅ REGISTRAR ESTE PANEL COMO ACTIVO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PushPanel(this);

        OnMenuOpened?.Invoke();
        Debug.Log("[CheckpointMenuUI] Menú abierto");
    }

    public void CloseMenu()
    {
        CloseAllPanels();
        isOpen = false;

        Time.timeScale = 1f;

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);

        // ✅ DESREGISTRAR ESTE PANEL DEL FOCO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PopPanel();

        OnMenuClosed?.Invoke();
        Debug.Log("[CheckpointMenuUI] Menú cerrado");
    }

    private void OnTravelButtonClicked()
    {
        OpenTravelPanel();
        OnTravelPressed?.Invoke();
        Debug.Log("[Gamepad] Travel presionado");
    }

    private void OnStatsButtonClicked()
    {
        OpenStatsPanel();
        OnStatsPressed?.Invoke();
        Debug.Log("[Gamepad] Stats presionado");
    }

    private void OnSkillsButtonClicked()
    {
        OpenSkillsPanel();
        OnSkillsPressed?.Invoke();
        Debug.Log("[Gamepad] Skills presionado");
    }

    private void OnCloseButtonClicked()
    {
        OnClosePressed?.Invoke();
        CloseMenu();
        Debug.Log("[Gamepad] Close presionado");
    }

    private void OpenTravelPanel()
    {
        CloseAllPanels();
        teleportPanel.gameObject.SetActive(true);
        teleportPanel.Open();

        // ✅ TeleportPanel maneja su propia selección en Open()
    }

    private void OpenStatsPanel()
    {
        CloseAllPanels();
        statsPanel.SetActive(true);
        checkpointStatsPanel.OpenSession();

        // ✅ CheckpointStatsPanel maneja su propia selección en OpenSession()
    }

    private void OpenSkillsPanel()
    {
        CloseAllPanels();
        skillsPanel.SetActive(true);

        var skillPanel = skillsPanel.GetComponent<SkillManagementPanel>();
        if (skillPanel != null)
            skillPanel.Open();

        // ✅ SkillManagementPanel maneja su propia selección en Open()
    }

    private void CloseAllPanels()
    {
        mainPanel.SetActive(false);
        teleportPanel.gameObject.SetActive(false);
        statsPanel.SetActive(false);
        skillsPanel.SetActive(false);

        if (mainPanelCanvasGroup != null)
            mainPanelCanvasGroup.interactable = false;

        // ✅ Desabilitar input en CheckpointStatsPanel al cerrar
        if (checkpointStatsPanel != null)
            checkpointStatsPanel.SetInputEnabled(false);
    }

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[CheckpointMenuUI] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        if (mainPanelCanvasGroup != null)
            mainPanelCanvasGroup.interactable = interactable;

        Debug.Log($"[CheckpointMenuUI] CanvasGroup.interactable = {interactable}");
    }

    public string GetPanelName() => "CheckpointMenuUI (Main)";
}