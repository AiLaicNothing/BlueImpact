using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class CharSelector_UI : MonoBehaviour, IGamepadPanel
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI charName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image charImage;

    [Header("Audio")]
    [SerializeField] private AudioClip music;
    private AudioSource audioSource;

    [Header("Buttons")]
    [SerializeField] private Button[] charButtons;
    [SerializeField] private Button confirmButton;

    [Header("Gamepad Settings")]
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    [Header("EventSystem")]
    [SerializeField] private EventSystem eventSystem;

    private int currentIndex;
    private bool hasSelectedAlready;
    private Button currentlySelectedButton; // ✅ TRACKEAR BOTÓN SELECCIONADO
    private bool inputEnabled = true; // ✅ CONTROL DE FOCO

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // ✅ OBTENER EventSystem si no está asignado
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        // ✅ CREAR AudioSource LOCAL para la música
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;  // ✅ LOOP ACTIVO
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (hasSelectedAlready) return;

        StartSelector();
    }

    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || !panel.activeSelf || !inputEnabled)
            return;

        HandleGamepadInput();
    }

    private void HandleGamepadInput()
    {
        // ✅ MOVER CURSOR CON JOYSTICK
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
    /// ✅ Manejar botones de sistema (A para confirmar, B sin acción)
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
    }

    private void StartSelector()
    {
        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        // ✅ HABILITAR INPUT Y CURSOR PARA GAMEPAD
        inputEnabled = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentlySelectedButton = null;
        Debug.Log("✅ Cursor visible y desbloqueado - Gamepad activado en CharSelector");

        // ✅ REPRODUCIR MÚSICA EN LOOP
        if (music != null && !audioSource.isPlaying)
        {
            audioSource.clip = music;
            audioSource.volume = 0.5f;
            audioSource.Play();
            Debug.Log("🎵 Música del selector iniciada en loop");
        }

        SelectCharacter(0);
        SetButtons();
    }

    private void SetButtons()
    {
        for (int i = 0; i < charButtons.Length; i++)
        {
            int index = i;
            charButtons[i].onClick.RemoveAllListeners();
            charButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => StartGame());
    }

    private void SelectCharacter(int index)
    {
        var data = CharSelector_Manager.Instance.GetCharacterInfo(index);

        if (data == null) return;

        currentIndex = index;

        charName.text = data.name;
        description.text = data.description;
        charImage.sprite = data.portrait;

        PlayerSpawn_Manager.Instance.SetCharacter(data);
    }

    private void StartGame()
    {
        panel.SetActive(false);
        inputEnabled = false; // ✅ DESHABILITAR INPUT DEL SELECTOR

        // ✅ DETENER MÚSICA DEL SELECTOR
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("⏹️ Música del selector detenida");
        }

        var spawnPoint = CharSelector_Manager.Instance.GetInitialSpawnPoint();

        // ✅ CONFIGURAR RESPAWN INICIAL
        CharSelector_Manager.Instance.SetupRespawn();

        PlayerSpawn_Manager.Instance.SpawnCharacter(spawnPoint);
    }

    private void OnDestroy()
    {
        // ✅ LIMPIAR AudioSource
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[CharSelector_UI] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        // Deshabilitar todos los botones de personajes
        foreach (var btn in charButtons)
        {
            btn.interactable = interactable;
        }

        // Deshabilitar botón de confirmación
        confirmButton.interactable = interactable;

        Debug.Log($"[CharSelector_UI] Interactable = {interactable}");
    }

    public string GetPanelName() => "CharSelector_UI";
}