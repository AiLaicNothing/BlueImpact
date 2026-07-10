using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

public class CheckpointStatsPanel : MonoBehaviour, IGamepadPanel
{
    [Header("Header")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TMP_Text availablePointsText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private StatEntryUI statEntryPrefab;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    public event Action OnConfirmPressed;
    public event Action OnCancelPressed;
    public event Action OnBackPressed;
    public event Action<StatDefinition> OnStatModified;

    private PlayerStatsManager playerStats;
    private readonly List<StatEntryUI> entries = new();
    public StatsModificationSession Session { get; private set; }

    // ✅ CONTROL DE FOCO
    private bool inputEnabled = true;

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();

        // ✅ ASEGURAR QUE JOYSTICK SENSITIVITY TIENE UN VALOR
        if (joystickSensitivity <= 0)
        {
            joystickSensitivity = 500f;
            Debug.LogWarning($"⚠️ joystickSensitivity era {joystickSensitivity}, establecido a 500f");
        }

        confirmButton.onClick.AddListener(() => OnConfirmButtonClicked());
        backButton.onClick.AddListener(() => OnBackButtonClicked());
        cancelButton.onClick.AddListener(() => OnCancelButtonClicked());
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        Session = null;
        ClearEntries();
    }

    private void OnEnable()
    {
        Debug.Log("✅ CheckpointStatsPanel.OnEnable - Suscribiendo al evento");
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    // ✅ UPDATE CON JOYSTICK PARA MOVER CURSOR
    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || entries.Count == 0 || !inputEnabled)
        {
            if (!gameObject.activeInHierarchy)
                return;
            if (entries.Count == 0)
                return;
            if (!inputEnabled)
            {
                Debug.LogWarning("[Update] inputEnabled es FALSE - Input deshabilitado");
                return;
            }
        }

        Debug.Log("[Update] Ejecutando HandleJoystickCursorMovement");

        // ✅ MOVER CURSOR CON JOYSTICK (Solo en este panel)
        HandleJoystickCursorMovement();

        // ✅ BOTONES DE SISTEMA (A, B, X, Y)
        HandleGamepadButtonInputs();
    }

    /// <summary>
    /// ✅ Mover el cursor con el Left Stick del gamepad
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

            // ✅ Usar Input.mousePosition (API antigua pero funciona)
            Vector3 currentCursorPos = Input.mousePosition;
            Debug.Log($"[Joystick] Posición actual (Input.mousePosition): {currentCursorPos}");

            // Calcular movimiento CON MULTIPLICADOR MAYOR
            // ✅ USAR unscaledDeltaTime para que funcione cuando Time.timeScale = 0
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;
            Debug.Log($"[Joystick] Delta calculado: {cursorDelta}");
            Debug.Log($"[Joystick] Desglose: stickInput({stickInput.x}, {stickInput.y}) * {joystickSensitivity} * {Time.unscaledDeltaTime} * 2.0");

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a los límites de la pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            Debug.Log($"[Joystick] Nueva posición: {newCursorPos}");

            // ✅ USAR Input.mousePosition DIRECTAMENTE
#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                Mouse.current.WarpCursorPosition(newCursorPos);
                Debug.Log($"[Joystick] ✅ Cursor movido a: {newCursorPos}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Joystick] ❌ Error moviendo cursor: {e.Message}");
            }
#endif
        }
    }

    /// <summary>
    /// ✅ Manejar botones del gamepad (A, B, X, Y) para acciones rápidas
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // ✅ A Button = Simular click en el elemento bajo el cursor
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            SimulateClickAtCursor();
        }

        // ✅ X Button = Confirmar cambios
        if (Gamepad.current.xButton.wasPressedThisFrame)
        {
            OnConfirmButtonClicked();
        }

        // ✅ B Button = Volver
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnBackButtonClicked();
        }

        // ✅ Y Button = Cancelar cambios (opcional)
        if (Gamepad.current.yButton.wasPressedThisFrame)
        {
            OnCancelButtonClicked();
        }
    }

    /// <summary>
    /// ✅ Simular un click del mouse en la posición actual del cursor
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

    private void OnPlayerSpawned(PlayerControl player)
    {
        Debug.Log("✅ CheckpointStatsPanel.OnPlayerSpawned - Recibí el evento");
        playerStats = player.GetComponent<PlayerStatsManager>();
        Debug.Log($"playerStats guardado: {(playerStats != null ? "✅" : "❌")}");
    }

    private void OnConfirmButtonClicked()
    {
        ConfirmChanges();
        OnConfirmPressed?.Invoke();
        Debug.Log("[Gamepad] Cambios confirmados");
    }

    private void OnCancelButtonClicked()
    {
        CancelChanges();
        OnCancelPressed?.Invoke();
        Debug.Log("[Gamepad] Cambios cancelados");
    }

    private void OnBackButtonClicked()
    {
        if (Session != null)
            Session.CancelChanges();

        // ✅ DESREGISTRAR ESTE PANEL DEL FOCO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PopPanel();

        CheckpointMenuUI.Instance.ShowMainPanel();
        OnBackPressed?.Invoke();
        Debug.Log("[Gamepad] Volviendo al menú principal");

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }

    public void OpenSession()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStatsManager>();
            Debug.LogWarning("⚠️ playerStats era null, lo busqué dinámicamente");
        }

        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStatsManager no encontrado");
            return;
        }

        Debug.Log("OpenSession");

        playerStats.EnsureInitialized();
        Debug.Log($"Stats encontradas: {playerStats.GetAllStats().Count}");

        Session = new StatsModificationSession(playerStats);

        characterIcon.sprite = playerStats.CharacterDefinition.characterIcon;

        CreateEntries();
        Refresh();

        // ✅ HABILITAR INPUT DEL PANEL
        inputEnabled = true;

        // ✅ ASEGURAR QUE TIME.TIMESCALE ES 1 (NO PAUSADO)
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("⚠️ Time.timeScale era 0, establecido a 1");
            Time.timeScale = 1f;
        }

        // ✅ ASEGURAR QUE EL CURSOR ESTÉ VISIBLE Y DESBLOQUEADO PARA JOYSTICK + MOUSE
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        // ✅ ASEGURAR QUE CURSOR ESTÁ VISIBLE Y ACTIVO PARA GAMEPAD
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("✅ Cursor visible y desbloqueado - Gamepad + Mouse activado");
        Debug.Log("✅ Input habilitado");

        // ✅ REGISTRAR ESTE PANEL COMO ACTIVO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PushPanel(this);
    }

    private void CreateEntries()
    {
        ClearEntries();

        foreach (var runtimeStat in playerStats.GetAllStats().Values)
        {
            StatEntryUI entry = Instantiate(statEntryPrefab, statsContainer);
            entry.Initialize(runtimeStat.definition, this);
            entries.Add(entry);
        }
    }

    private void ClearEntries()
    {
        foreach (StatEntryUI entry in entries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    public void TryIncrease(StatDefinition stat)
    {
        if (Session.IncreaseStat(stat))
        {
            Refresh();
            OnStatModified?.Invoke(stat);
            Debug.Log($"[Gamepad] {stat.statName} aumentado");
        }
    }

    public void TryDecrease(StatDefinition stat)
    {
        if (Session.UndoIncrease(stat))
        {
            Refresh();
            OnStatModified?.Invoke(stat);
            Debug.Log($"[Gamepad] {stat.statName} disminuido");
        }
    }

    private void ConfirmChanges()
    {
        Session.ConfirmChanges();
        Refresh();
    }

    private void CancelChanges()
    {
        Session.CancelChanges();
        Refresh();
    }

    private void Refresh()
    {
        availablePointsText.text = $"Puntos: {Session.RemainingPoints}";
        confirmButton.interactable = Session.GetUsedPoints() > 0;

        foreach (StatEntryUI entry in entries)
        {
            entry.Refresh();
        }
    }

    public List<StatEntryUI> GetEntries() => entries;

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[CheckpointStatsPanel] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        // ✅ Usar CanvasGroup para control global
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = interactable;
            panelCanvasGroup.blocksRaycasts = interactable;
        }

        confirmButton.interactable = interactable && (Session != null && Session.GetUsedPoints() > 0);
        cancelButton.interactable = interactable;
        backButton.interactable = interactable;

        foreach (var entry in entries)
        {
            var decreaseBtn = entry.GetDecreaseButton();
            var increaseBtn = entry.GetIncreaseButton();

            if (decreaseBtn != null)
                decreaseBtn.interactable = interactable;
            if (increaseBtn != null)
                increaseBtn.interactable = interactable;
        }

        Debug.Log($"[CheckpointStatsPanel] Interactable = {interactable}, CanvasGroup = {(panelCanvasGroup != null ? "✅" : "❌")}");
    }

    public string GetPanelName() => "CheckpointStatsPanel";
}