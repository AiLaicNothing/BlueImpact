using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Stats_Panel : MonoBehaviour, IGamepadPanel
{
    [Header("Portrait Zone")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI availablePointsText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button returnButton;

    [Header("Stats rows")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private EventSystem _eventSystem;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    [Header("Debug")]
    [SerializeField] private bool debug;

    public event Action OnConfirmPressed;
    public event Action OnResetlPressed;
    public event Action OnReturnPressed;

    private bool inputEnabled;

    private readonly List<StatRow> entries = new();

    private void Awake()
    {
        if (_eventSystem == null) _eventSystem = EventSystem.current;

        if (joystickSensitivity <= 0) joystickSensitivity = 500;

        if (confirmButton != null) confirmButton.onClick.AddListener(() => OnConfirm());
        if (resetButton != null) resetButton.onClick.AddListener(() => OnCancel());
        if (returnButton != null) returnButton.onClick.AddListener(() => OnReturn());
    }

    #region OnAble

    private void OnEnable()
    {
        if (CharacterStatsManager.Instance != null)
        {
            CharacterStatsManager.Instance.OnStatsChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (CharacterStatsManager.Instance != null)
        {
            CharacterStatsManager.Instance.OnStatsChanged -= Refresh;
        }
    }

    #endregion

    // UPDATE CON JOYSTICK PARA MOVER CURSOR
    private void Update()
    {
        // SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || entries.Count == 0 || !inputEnabled)
        {
            if (!gameObject.activeInHierarchy) return;

            if (entries.Count == 0) return;

            if (!inputEnabled)
            {
                Debug.LogWarning("[Update] inputEnabled es FALSE - Input deshabilitado");
                return;
            }
        }

        Debug.Log("[Update] Ejecutando HandleJoystickCursorMovement");

        HandleJoystickCursorMovement();

        HandleGamepadButtonInputs();
    }

    #region Gamepad Input

    /// <summary>
    /// Mover el cursor con el Left Stick del gamepad
    /// </summary>
    private void HandleJoystickCursorMovement()
    {
        if (Gamepad.current == null)
        {
            Debug.LogWarning("[Joystick] Gamepad.current es NULL");
            return;
        }

        Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

        Debug.Log($"[Joystick] Stick Input: {stickInput}, Magnitude: {stickInput.magnitude}");
        Debug.Log($"[Joystick] joystickSensitivity: {joystickSensitivity}, Time.deltaTime: {Time.deltaTime}");

        // Solo mover si hay input significativo (deadzone reducido a 0.05f)
        if (stickInput.magnitude > 0.05f)
        {
            Debug.Log("[Joystick] Input detectado, moviendo cursor...");

            // Usar Input.mousePosition (API antigua pero funciona)
            Vector3 currentCursorPos = Input.mousePosition;
            Debug.Log($"[Joystick] Posición actual (Input.mousePosition): {currentCursorPos}");

            // Calcular movimiento CON MULTIPLICADOR MAYOR
            // USAR unscaledDeltaTime para que funcione cuando Time.timeScale = 0
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;
            Debug.Log($"[Joystick] Delta calculado: {cursorDelta}");
            Debug.Log($"[Joystick] Desglose: stickInput({stickInput.x}, {stickInput.y}) * {joystickSensitivity} * {Time.unscaledDeltaTime} * 2.0");

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a los límites de la pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            Debug.Log($"[Joystick] Nueva posición: {newCursorPos}");

            // USAR Input.mousePosition DIRECTAMENTE
#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                Mouse.current.WarpCursorPosition(newCursorPos);
                Debug.Log($"[Joystick] Cursor movido a: {newCursorPos}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Joystick] Error moviendo cursor: {e.Message}");
            }
#endif
        }
    }

    /// <summary>
    /// Manejar botones del gamepad (A, B, X, Y) para acciones rápidas
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // A Button = Simular click en el elemento bajo el cursor
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            SimulateClickAtCursor();
        }

        // X Button = Confirmar cambios
        if (Gamepad.current.xButton.wasPressedThisFrame)
        {
            OnConfirm();
        }

        // B Button = Volver
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnReturn();
        }

        // Y Button = Cancelar cambios (opcional)
        if (Gamepad.current.yButton.wasPressedThisFrame)
        {
            OnCancel();
        }
    }

    /// <summary>
    /// Simular un click del mouse en la posición actual del cursor
    /// </summary>
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
                Debug.Log($" Click simulado en: {button.gameObject.name}");
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

    private void OnConfirm()
    {
        CharacterStatsManager.Instance.ApplyStats();
        OnConfirmPressed?.Invoke();
        Debug.Log("[Gamepad] Cambios confirmados");
    }

    private void OnCancel()
    {
        CharacterStatsManager.Instance.CancelChanges();
        OnResetlPressed?.Invoke();
        Debug.Log("[Gamepad] Cambios cancelados");
    }

    private void OnReturn()
    {
        CharacterStatsManager.Instance.CancelChanges();

        if (PanelFocusManager.Instance != null) PanelFocusManager.Instance.PopPanel();

        Checkpoint_Menu.Instance.ReOpenMenu();
        OnReturnPressed?.Invoke();

        Debug.Log("[Gamepad] Volviendo al menú principal");

        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);
    }

    #endregion

    public void Open()
    {
        if (CharacterStatsManager.Instance == null)
        {
            Debug.LogError("Character stats manager is missing");
            return;
        }

        var CharacterData = Player_Manager.Instance.GetCharacterData();

        CharacterStatsManager.Instance.BeginEdit();

        characterIcon.sprite = CharacterData.portrait;

        CreateEntries();
        Refresh();

        inputEnabled = true;

        if (Time.timeScale == 0f) Time.timeScale = 1f;

        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (PanelFocusManager.Instance != null) PanelFocusManager.Instance.PushPanel(this);

    }

    private void CreateEntries()
    {
        ClearEntries();

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            GameObject statRow = Instantiate(statRowPrefab, statsContainer);

            var content = statRow.GetComponent<StatRow>();

            content.Initialize(stat);

            entries.Add(content);
        }
    }

    private void ClearEntries()
    {
        foreach (StatRow row in entries)
        {
            if (row != null) Destroy(row.gameObject);
        }

        entries.Clear();
    }

    private void Refresh()
    {
        availablePointsText.text = $"Puntos: {CharacterStatsManager.Instance.AvailablePoints}";

        confirmButton.interactable = CharacterStatsManager.Instance.GetTotalPendingPoints() > 0;
    }

    #region IGamePad Functions

    public string GetPanelName() => "Checkpoint -> Stats Menu";

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        confirmButton.interactable = interactable && CharacterStatsManager.Instance.GetTotalPendingPoints() > 0;
        resetButton.interactable = interactable;
        resetButton.interactable = interactable;

        foreach (var entry in entries)
        {
            var increaseButton = entry.GetIncreaseButton();
            var decreaseButton = entry.GetDecreaseButton();

            if (increaseButton != null) increaseButton.interactable = interactable;
            if (decreaseButton != null) decreaseButton.interactable = interactable;
        }
    }

    #endregion
}
