using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Travel_Panel : MonoBehaviour, IGamepadPanel
{
    [Header("lIST")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject entryPrefab;

    [Header("Preview")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI checkpointName;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private Button returnButton;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    private bool isTraveling;

    private EventSystem _eventSystem;

    public event Action OnTravelPressed;
    public event Action OnBackPressed;
    public event Action OnCheckpointSelected;

    private readonly List<CheckpointRow> entries = new();
    private DiscoveredCheckpoint selectedCheckpoint;
    private Button currentlySelectedButton; // RACKEAR BOTÓN SELECCIONADO

    private bool inputEnabled = true;

    private void Awake()
    {
        if (_eventSystem == null) _eventSystem = EventSystem.current;

        travelButton.onClick.AddListener(() => OnTravel());
        returnButton.onClick.AddListener(() => OnReturn());
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || entries.Count == 0 || !inputEnabled) return;

        HandleGamepadInput();
    }

    #region Gamepad

    private void HandleGamepadInput()
    {
        // MOVER CURSOR CON JOYSTICK (Solo en este panel)
        HandleJoystickCursorMovement();

        // BOTONES DE SISTEMA (A, B)
        HandleGamepadButtonInputs();
    }

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

        // Solo mover si hay input significativo (deadzone reducido a 0.05f)
        if (stickInput.magnitude > 0.05f)
        {
            // Usar Input.mousePosition (API antigua pero funciona)
            Vector3 currentCursorPos = Input.mousePosition;

            // Calcular movimiento CON MULTIPLICADOR MAYOR
            // USAR unscaledDeltaTime para que funcione cuando Time.timeScale = 0
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a los límites de la pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            // SAR WARP CURSOR POSITION DEL INPUT SYSTEM
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
    /// Detectar qué botón está bajo el cursor y seleccionarlo VISUALMENTE (sin invocar click)
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

        // Buscar el PRIMER BUTTON en los resultados y SOLO SELECCIONARLO
        foreach (RaycastResult hit in results)
        {
            Button button = hit.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                // GUARDAR BOTÓN SELECCIONADO Y SELECCIONAR VISUALMENTE
                currentlySelectedButton = button;
                _eventSystem.SetSelectedGameObject(button.gameObject);
                return;
            }

            // Si no tiene Button, buscar en padre
            Button parentButton = hit.gameObject.GetComponentInParent<Button>();
            if (parentButton != null && parentButton.interactable)
            {
                // GUARDAR BOTÓN SELECCIONADO Y SELECCIONAR VISUALMENTE
                currentlySelectedButton = parentButton;
                _eventSystem.SetSelectedGameObject(parentButton.gameObject);
                return;
            }
        }

        // SI NO HAY BOTÓN BAJO EL CURSOR, LIMPIAR SELECCIÓN
        currentlySelectedButton = null;
    }

    /// <summary>
    /// Manejar botones de sistema (A para viajar, B para retroceder)
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // A BUTTON = Invocar el botón seleccionado actualmente
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            if (currentlySelectedButton != null && currentlySelectedButton.interactable)
            {
                currentlySelectedButton.onClick.Invoke();
                Debug.Log($"A Button presionado: {currentlySelectedButton.gameObject.name}");
            }
        }

        // B BUTTON = Retroceder
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnReturn();
        }
    }

    #endregion

    private void OnTravel()
    {
        Debug.Log("[Travel Panel] Travel button clicked.");

        if (!travelButton.interactable)
        {
            Debug.LogWarning("[Travel Panel] Travel button is not interactable.");
            return;
        }

        Travel();
    }

    private void OnReturn()
    {
        if(PanelFocusManager.Instance != null) PanelFocusManager.Instance.PopPanel();

        Checkpoint_Menu.Instance.ReOpenMenu();

        OnBackPressed?.Invoke();
    }

    public void Open()
    {
        isTraveling = false;

        RefreshList();

        currentlySelectedButton = null;
        selectedCheckpoint = null;

        previewImage.sprite = null;
        checkpointName.text = "Selecciona un destino";
        travelButton.interactable = false;

        DiscoveredCheckpoint activeCheckpoint = Checkpoints_Manager.Instance != null? Checkpoints_Manager.Instance.GetActiveCheckpoint() : null;

        if (activeCheckpoint != null)
        {
            SelectCheckpoint(activeCheckpoint);
        }

        inputEnabled = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (_eventSystem != null)
        {
            _eventSystem.SetSelectedGameObject(null);
        }

        if (PanelFocusManager.Instance != null)
        {
            PanelFocusManager.Instance.PushPanel(this);
        }
    }

    public void SelectCheckpoint(DiscoveredCheckpoint checkpoint)
    {
        if (checkpoint == null || checkpoint._data == null) return;

        selectedCheckpoint = checkpoint;

        previewImage.sprite = checkpoint._data.previewImage;
        checkpointName.text = checkpoint._data.checkpointName;

        travelButton.interactable = true;

        OnCheckpointSelected?.Invoke();
    }


    private void Travel()
    {
        if (isTraveling)
        {
            Debug.LogWarning("[Travel Panel] A travel operation is already running.");
            return;
        }

        if (selectedCheckpoint == null)
        {
            Debug.LogError("[Travel Panel] No checkpoint is selected.");
            return;
        }

        if (Teleport_Manager.Instance == null)
        {
            Debug.LogError("[Travel Panel] Teleport_Manager.Instance is missing.");
            return;
        }

        Debug.Log(
            $"[Travel Panel] Traveling to '{selectedCheckpoint._checkpointID}' " +
            $"in scene '{selectedCheckpoint._sceneName}'.");

        isTraveling = true;
        inputEnabled = false;
        travelButton.interactable = false;

        Teleport_Manager.Instance.Teleport(selectedCheckpoint);

        Time.timeScale = 1f;

        OnTravelPressed?.Invoke();
    }

    private void RefreshList()
    {
        Clear();

        var manager = Checkpoints_Manager.Instance;

        if (manager == null) return;

        DiscoveredCheckpoint current = manager.GetActiveCheckpoint();

        foreach (DiscoveredCheckpoint checkpoint in Checkpoints_Manager.Instance.GetDiscoveredCheckpoints())
        {
            var entry = Instantiate(entryPrefab, content);

            bool isCurrent = current != null && current.Matches(checkpoint._sceneName, checkpoint._checkpointID);

            var entryUI = entry.GetComponent<CheckpointRow>();

            entryUI.Initialize(checkpoint, this, isCurrent);

            entries.Add(entryUI);
        }
    }

    private void Clear()
    {
        foreach (CheckpointRow entry in entries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    public List<CheckpointRow> GetEntries() => entries;

    #region Gamepanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void SetInteractable(bool interactable)
    {
        travelButton.interactable = interactable && selectedCheckpoint != null;
        returnButton.interactable = interactable;

        foreach (CheckpointRow entry in entries)
        {
            if (entry != null) entry.SetInteractable(interactable);
        }
    }

    public string GetPanelName() => "Checkpoint -> Travel panel";

    #endregion
}
