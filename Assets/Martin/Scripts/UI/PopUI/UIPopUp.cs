using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

public class UIPopUp : MonoBehaviour, IGamepadPanel
{
    public static UIPopUp Instance;

    [SerializeField] private GameObject popUpPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TextMeshProUGUI tittleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button previousPage;

    [Header("Gamepad Settings")]
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick
    [SerializeField] private EventSystem eventSystem;

    private List<PopUpPage> currentPages = new List<PopUpPage>();
    private int currentPageIndex;
    private Button currentlySelectedButton; // ✅ TRACKEAR BOTÓN SELECCIONADO
    private bool inputEnabled = true; // ✅ CONTROL DE FOCO

    private void Awake()
    {
        Instance = this;

        if (panelCanvasGroup == null)
            panelCanvasGroup = popUpPanel.GetComponent<CanvasGroup>();

        // ✅ OBTENER EventSystem si no está asignado
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (popUpPanel != null) popUpPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(ClosePopUp);
        if (nextPage != null) nextPage.onClick.AddListener(NextPage);
        if (previousPage != null) previousPage.onClick.AddListener(PreviousPage);
    }

    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || !popUpPanel.activeSelf || !inputEnabled)
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
    /// ✅ Manejar botones de sistema (A para invocar botón, B para cerrar)
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

        // ✅ B BUTTON = Cerrar popup (si es la última página)
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            if (currentPageIndex == currentPages.Count - 1)
            {
                ClosePopUp();
            }
        }
    }

    public void ShowPopUp(List<PopUpPage> pages)
    {
        if (pages == null || pages.Count == 0) return;

        currentPages = pages;
        currentPageIndex = 0;
        currentlySelectedButton = null; // ✅ LIMPIAR SELECCIÓN ANTERIOR

        popUpPanel.SetActive(true);
        LoadPage();

        // ✅ HABILITAR INPUT Y CURSOR PARA GAMEPAD
        inputEnabled = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("✅ Cursor visible y desbloqueado - Gamepad activado en UIPopUp");

        // ✅ CAMBIAR GAMEMODE A UI
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.UI);
        }

        // ✅ ASEGURAR QUE EL PANEL RECIBE INPUT
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // ✅ CORTAR SONIDOS EN LOOP DEL PLAYER (ej. caminar)
        // Time.timeScale = 0 no pausa AudioSources que ya están sonando,
        // así que hay que detenerlos a mano.
        MutePlayerLoopAudio();

        Time.timeScale = 0f;
    }

    private void MutePlayerLoopAudio()
    {
        PlayerControl player = FindFirstObjectByType<PlayerControl>();

        if (player == null || player.audioLoopSource == null) return;

        player.audioLoopSource.Stop();
    }

    private void LoadPage()
    {
        if (currentPageIndex < 0 || currentPageIndex >= currentPages.Count) return;

        PopUpPage page = currentPages[currentPageIndex];

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = page.video;
        }

        if (videoScreen != null) videoScreen.texture = page.texture;

        if (tittleText != null) tittleText.text = page.title;

        if (descriptionText != null) descriptionText.text = page.description;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasPrevious = currentPageIndex > 0;
        bool hasNext = currentPageIndex < currentPages.Count - 1;
        bool isLastPage = currentPageIndex == currentPages.Count - 1;

        if (previousPage != null) previousPage.gameObject.SetActive(hasPrevious);
        if (nextPage != null) nextPage.gameObject.SetActive(hasNext);
        if (closeButton != null) closeButton.gameObject.SetActive(isLastPage);
    }

    public void NextPage()
    {
        if (currentPageIndex >= currentPages.Count - 1) return;

        currentPageIndex++;
        LoadPage();
    }

    public void PreviousPage()
    {
        if (currentPageIndex <= 0) return;

        currentPageIndex--;
        LoadPage();
    }

    public void ClosePopUp()
    {
        inputEnabled = false; // ✅ DESHABILITAR INPUT DEL POPUP

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        if (videoScreen != null) videoScreen.texture = null;
        if (tittleText != null) tittleText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;

        popUpPanel.SetActive(false);

        // ✅ DESACTIVAR INPUT DEL PANEL
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // ✅ RESTAURAR GAMEMODE
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.Gameplay);
        }

        currentPages.Clear();
        currentPageIndex = 0;

        Time.timeScale = 1f;
    }

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[UIPopUp] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        // Deshabilitar todos los botones de navegación
        if (closeButton != null)
            closeButton.interactable = interactable;

        if (nextPage != null)
            nextPage.interactable = interactable && (currentPageIndex < currentPages.Count - 1);

        if (previousPage != null)
            previousPage.interactable = interactable && (currentPageIndex > 0);

        Debug.Log($"[UIPopUp] Interactable = {interactable}");
    }

    public string GetPanelName() => "UIPopUp";
}