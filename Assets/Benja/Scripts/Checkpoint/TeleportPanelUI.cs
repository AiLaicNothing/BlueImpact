using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

public class TeleportPanelUI : MonoBehaviour, IGamepadPanel
{
    [Header("List")]
    [SerializeField] private Transform content;
    [SerializeField] private CheckpointEntryUI entryPrefab;

    [Header("Preview")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text checkpointName;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private Button backButton;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    public event Action OnTravelPressed;
    public event Action OnBackPressed;
    public event Action OnCheckpointSelected;

    private readonly List<CheckpointEntryUI> entries = new();
    private Checkpoint selectedCheckpoint;
    private Button currentlySelectedButton; // ✅ TRACKEAR BOTÓN SELECCIONADO

    // ✅ CONTROL DE FOCO
    private bool inputEnabled = true;

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        travelButton.onClick.AddListener(() => OnTravelButtonClicked());
        backButton.onClick.AddListener(() => OnBackButtonClicked());
    }

    private void OnEnable()
    {
        // Gamepad ready
    }

    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || entries.Count == 0 || !inputEnabled)
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
        {
            Debug.LogWarning("[Joystick] Gamepad.current es NULL");
            return;
        }

        Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

        // Solo mover si hay input significativo (deadzone reducido a 0.05f)
        if (stickInput.magnitude > 0.05f)
        {
            // ✅ Usar Input.mousePosition (API antigua pero funciona)
            Vector3 currentCursorPos = Input.mousePosition;

            // Calcular movimiento CON MULTIPLICADOR MAYOR
            // ✅ USAR unscaledDeltaTime para que funcione cuando Time.timeScale = 0
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a los límites de la pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            // ✅ USAR WARP CURSOR POSITION DEL INPUT SYSTEM
#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                Mouse.current.WarpCursorPosition(newCursorPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Joystick] ❌ Error moviendo cursor: {e.Message}");
            }
#endif

            // Detectar qué botón está bajo el cursor y seleccionarlo automáticamente
            SimulateClickAtCursor();
        }
    }

    /// <summary>
    /// ✅ Detectar qué botón está bajo el cursor y seleccionarlo VISUALMENTE (sin invocar click)
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

        // ✅ Buscar el PRIMER BUTTON en los resultados y SOLO SELECCIONARLO
        foreach (RaycastResult hit in results)
        {
            Button button = hit.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                // ✅ GUARDAR BOTÓN SELECCIONADO Y SELECCIONAR VISUALMENTE
                currentlySelectedButton = button;
                eventSystem.SetSelectedGameObject(button.gameObject);
                return;
            }

            // Si no tiene Button, buscar en padre
            Button parentButton = hit.gameObject.GetComponentInParent<Button>();
            if (parentButton != null && parentButton.interactable)
            {
                // ✅ GUARDAR BOTÓN SELECCIONADO Y SELECCIONAR VISUALMENTE
                currentlySelectedButton = parentButton;
                eventSystem.SetSelectedGameObject(parentButton.gameObject);
                return;
            }
        }

        // ✅ SI NO HAY BOTÓN BAJO EL CURSOR, LIMPIAR SELECCIÓN
        currentlySelectedButton = null;
    }

    /// <summary>
    /// ✅ Manejar botones de sistema (A para viajar, B para retroceder)
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // ✅ A BUTTON = Invocar el botón seleccionado actualmente
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            if (currentlySelectedButton != null && currentlySelectedButton.interactable)
            {
                currentlySelectedButton.onClick.Invoke();
                Debug.Log($"✅ A Button presionado: {currentlySelectedButton.gameObject.name}");
            }
        }

        // ✅ B BUTTON = Retroceder
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnBackButtonClicked();
        }
    }



    private void OnTravelButtonClicked()
    {
        if (travelButton.interactable)
        {
            Travel();
            OnTravelPressed?.Invoke();
        }
    }

    private void OnBackButtonClicked()
    {
        Back();
        OnBackPressed?.Invoke();
    }

    private void Back()
    {
        // ✅ DESREGISTRAR ESTE PANEL DEL FOCO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PopPanel();

        CheckpointMenuUI.Instance.ShowMainPanel();
    }

    public void Open()
    {
        RefreshList();
        currentlySelectedButton = null; // ✅ LIMPIAR SELECCIÓN ANTERIOR

        Checkpoint current = CheckpointManager.Instance.GetActiveCheckpoint();

        if (current != null)
        {
            SelectCheckpoint(current);
        }
        else
        {
            previewImage.sprite = null;
            checkpointName.text = "Selecciona un destino";
            travelButton.interactable = false;
        }

        // ✅ HABILITAR INPUT DEL PANEL
        inputEnabled = true;

        // ✅ ASEGURAR QUE CURSOR ESTÉ VISIBLE Y DESBLOQUEADO PARA JOYSTICK + MOUSE
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("✅ Cursor visible y desbloqueado - Gamepad + Mouse activado");
        Debug.Log("✅ Input habilitado");

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);

        // ✅ REGISTRAR ESTE PANEL COMO ACTIVO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PushPanel(this);
    }

    public void SelectCheckpoint(Checkpoint checkpoint)
    {
        selectedCheckpoint = checkpoint;
        previewImage.sprite = checkpoint.Data.previewImage;
        checkpointName.text = checkpoint.Data.checkpointName;
        travelButton.interactable = true;
    }

    private void RefreshList()
    {
        Clear();

        Checkpoint current = CheckpointManager.Instance.GetActiveCheckpoint();

        foreach (Checkpoint checkpoint in CheckpointManager.Instance.GetDiscoveredCheckpoints())
        {
            CheckpointEntryUI entry = Instantiate(entryPrefab, content);
            bool isCurrent = checkpoint == current;

            entry.Initialize(checkpoint, this, isCurrent);
            entries.Add(entry);
        }
    }

    private void Travel()
    {
        if (selectedCheckpoint == null)
            return;

        TeleportManager.Instance.Teleport(selectedCheckpoint);
    }

    private void Clear()
    {
        foreach (CheckpointEntryUI entry in entries)
        {
            Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    public List<CheckpointEntryUI> GetEntries() => entries;

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[TeleportPanelUI] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        // Deshabilitar todos los botones de entrada
        travelButton.interactable = interactable && (selectedCheckpoint != null);
        backButton.interactable = interactable;

        // Deshabilitar raycast en los entries
        foreach (var entry in entries)
        {
            var button = entry.GetComponent<Button>();
            if (button != null)
                button.interactable = interactable && button.interactable;
        }

        Debug.Log($"[TeleportPanelUI] Interactable = {interactable}");
    }

    public string GetPanelName() => "TeleportPanelUI";
}