using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Checkpoint_Menu : MonoBehaviour, IGamepadPanel
{
    public static Checkpoint_Menu Instance;

    [Header("Main panel")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject panel;

    [Header("Secondary panels")]
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject skillPanel;

    [Header("Buttons")]
    [SerializeField] private Button teleport;
    [SerializeField] private Button skill;
    [SerializeField] private Button stats;
    [SerializeField] private Button close;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    public event Action OnTravelPressed;
    public event Action OnStatsPressed;
    public event Action OnSkillsPressed;
    public event Action OnClosePressed;
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;

    private EventSystem _eventSystem;
    private Checkpoint currentCheckpoint;

    private bool isOpen = false;
    public bool IsOpen() => isOpen;

    private bool inputEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ClosePanels();

        teleport.onClick.AddListener(() => OnOpenTeleport());
        stats.onClick.AddListener(() => OnOpenStats());
        skill.onClick.AddListener(() => OnOpenSkills());
        close.onClick.AddListener(() => OnClose());

    }

    private void Update()
    {
        if (!isOpen || canvasGroup.interactable == false || !inputEnabled) return;

        HandleGamepadInput();
    }

    #region GamepadInput

    private void HandleGamepadInput()
    {
        HandleJoystickCursorMovement();

        HandleGamepadButtonInputs();
    }

    private void HandleJoystickCursorMovement()
    {
        if (Gamepad.current == null) return;

        Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

        // Solo mover si hay input significativo
        if (stickInput.magnitude > 0.05f)
        {
            // Usar Input.mousePosition
            Vector3 currentCursorPos = Input.mousePosition;

            // Calcular movimiento CON unscaledDeltaTime
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            // MOVER CURSOR
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
    /// Manejar botones del gamepad (A, B)
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // A Button = Click en botón bajo cursor
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            SimulateClickAtCursor();
        }

        // B Button = Cerrar menú
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnClose();
            return;
        }
    }

    private void SimulateClickAtCursor()
    {
        if (_eventSystem == null)
        {
            Debug.LogError("[SimulateClick] EventSystem es NULL");
            return;
        }

        // Raycast desde posición del cursor
        PointerEventData pointerData = new PointerEventData(_eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();

        _eventSystem.RaycastAll(pointerData, results);

        Debug.Log($"[SimulateClick] Raycast encontró {results.Count} objetos");

        // Buscar el PRIMER BUTTON en los resultados (no el primer objeto)
        foreach (RaycastResult hit in results)
        {
            Button button = hit.gameObject.GetComponent<Button>();

            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                Debug.Log($"Click simulado en: {button.gameObject.name}");
                return;
            }

            // Si no tiene Button, buscar en padre
            Button parentButton = hit.gameObject.GetComponentInParent<Button>();

            if (parentButton != null && parentButton.interactable)
            {
                parentButton.onClick.Invoke();
                Debug.Log($"Click simulado en padre: {parentButton.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("[SimulateClick] No se encontró ningún Button clickeable");
    }

    #endregion

    #region Buttons

    private void OnOpenTeleport()
    {
        ClosePanels();

        var _TravelPanel = teleportPanel.GetComponent<Travel_Panel>();

        _TravelPanel.Open();

        teleportPanel.SetActive(true);

        OnTravelPressed?.Invoke();
    }

    private void OnOpenStats()
    {
        ClosePanels();

        var _statPanel = statsPanel.GetComponent<Stats_Panel>();

        _statPanel.Open();

        statsPanel.SetActive(true);

        OnStatsPressed?.Invoke();
    }

    private void OnOpenSkills()
    {
        ClosePanels();
        
        skillPanel.SetActive(true);

        var _skillPanel = skillPanel.GetComponent<SkillManagementPanel>();

        if (_skillPanel != null ) _skillPanel.Open();

        OnSkillsPressed?.Invoke();
    }

    private void OnClose()
    {
        OnClosePressed?.Invoke();
        Close();
    }

    #endregion

    // This is to be called by other panels when they need to return to the main panel
    public void ReOpenMenu()
    {
        ClosePanels();
        panel.SetActive(true);
        isOpen = true;

        if (canvasGroup != null) canvasGroup.interactable = true;

        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);

        if (PanelFocusManager.Instance != null) PanelFocusManager.Instance.PushPanel(this);
    }

    public void Open(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;

        if (GameModeManager.Instance != null) GameModeManager.Instance.SetMode(GameMode.UI);

        ClosePanels();
        panel.SetActive(true);
        isOpen = true;

        if (canvasGroup != null) canvasGroup.interactable = true;

        Time.timeScale = 0f;

        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);

        if (PanelFocusManager.Instance != null) PanelFocusManager.Instance.PushPanel(this);

        OnMenuOpened?.Invoke();
    }

    public void Close()
    {
        ClosePanels();
        isOpen = false;

        Time.timeScale = 1f;

        if (GameModeManager.Instance != null) GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);

        if (PanelFocusManager.Instance != null) PanelFocusManager.Instance.PopPanel();

        OnMenuClosed?.Invoke();
    }

    private void ClosePanels()
    {
        panel.SetActive(false);
        teleportPanel.SetActive(false);
        skillPanel.SetActive(false);
        statsPanel.SetActive(false);

        if (canvasGroup != null) canvasGroup.interactable = false;
    }

    #region IGamePad Functions

    public string GetPanelName() => "Checkpoint -> Main Menu";

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null) canvasGroup.interactable = interactable;
    }

    #endregion
}
