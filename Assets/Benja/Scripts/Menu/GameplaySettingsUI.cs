using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplaySettingsUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;
    [SerializeField] private TabsManager tabsManager;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private EventSystem eventSystem;

    // AUDIO
    [SerializeField] private SettingSlider masterVolumeSlider;
    [SerializeField] private SettingSlider voiceVolumeSlider;
    [SerializeField] private SettingSlider sfxVolumeSlider;
    [SerializeField] private SettingSlider musicVolumeSlider;

    // CONTROLS
    [SerializeField] private SettingSlider mouseSensitivitySlider;
    [SerializeField] private SettingToggle invertMouseYToggle;
    [SerializeField] private Transform keybindsContainer;
    [SerializeField] private KeybindButton keybindButtonPrefab;

    // Input System
    private PlayerInputHandler playerInputHandler;

    private SettingsManager settingsManager;
    private bool isOpen = false;

    // ✅ NUEVO: Detectar dispositivo
    private bool isGamepadActive = false;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backButton != null)
            backButton.onClick.AddListener(Close);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
        settingsManager = SettingsManager.Instance;
        InitializeAllSettings();
    }

    private void OnEnable()
    {
        if (playerInputHandler != null)
        {
            // Las acciones de navegación las maneja el EventSystem automáticamente
        }
    }

    private void OnDisable()
    {
        // Limpiar suscripciones si es necesario
    }

    private void Update()
    {
        // ✅ DETECTAR DISPOSITIVO CADA FRAME
        if (isOpen)
        {
            bool wasGamepadActive = isGamepadActive;

            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            {
                isGamepadActive = true;
            }
            else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            {
                isGamepadActive = false;
            }

            // ✅ SI CAMBIA DE DISPOSITIVO, ACTUALIZAR KEYBINDS
            if (wasGamepadActive != isGamepadActive)
            {
                CreateKeybinds();
            }
        }
    }

    private void InitializeAllSettings()
    {
        if (settingsManager == null) return;

        InitializeAudioSettings();
        InitializeControlsSettings();
    }

    private void InitializeAudioSettings()
    {
        if (settingsManager == null) return;

        var settings = settingsManager.GetSettings();

        if (masterVolumeSlider != null)
            masterVolumeSlider.Initialize(
                "Volumen Global", 0f, 1f,
                settings.audio.masterVolume,
                (value) => settingsManager.SetMasterVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.Initialize(
                "Volumen de Voz", 0f, 1f,
                settings.audio.voiceVolume,
                (value) => settingsManager.SetVoiceVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.Initialize(
                "Volumen de Efectos", 0f, 1f,
                settings.audio.sfxVolume,
                (value) => settingsManager.SetSFXVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );

        if (musicVolumeSlider != null)
            musicVolumeSlider.Initialize(
                "Volumen de Música", 0f, 1f,
                settings.audio.musicVolume,
                (value) => settingsManager.SetMusicVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );
    }

    private void InitializeControlsSettings()
    {
        if (settingsManager == null) return;

        var settings = settingsManager.GetSettings();

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.Initialize(
                "Sensibilidad del Mouse", 0.1f, 3f,
                settings.controls.mouseSensitivity,
                (value) => settingsManager.SetMouseSensitivity(value),
                SettingSlider.DisplayFormat.DecimalTwoPlaces
            );

        if (invertMouseYToggle != null)
            invertMouseYToggle.Initialize(
                "Invertir Eje Y del Mouse",
                settings.controls.invertMouseY,
                (value) => settingsManager.SetInvertMouseY(value)
            );

        CreateKeybinds();
    }

    private void CreateKeybinds()
    {
        if (keybindsContainer == null || keybindButtonPrefab == null)
            return;

        foreach (Transform child in keybindsContainer)
            Destroy(child.gameObject);

        // ✅ MOSTRAR KEYBINDS SEGÚN DISPOSITIVO
        if (isGamepadActive)
        {
            CreateGamepadKeybinds();
        }
        else
        {
            CreateKeyboardKeybinds();
        }
    }

    private void CreateKeyboardKeybinds()
    {
        // ✅ KEYBINDS PARA TECLADO
        // [1] = WASD/Up (W - adelante)
        // [2] = WASD/Down (S - atrás)
        // [3] = WASD/Left (A - izquierda)
        // [4] = WASD/Right (D - derecha)
        var keybinds = new[]
        {
            ("Adelante", "Player/Move", 1),
            ("Izquierda", "Player/Move", 3),
            ("Atrás", "Player/Move", 2),
            ("Derecha", "Player/Move", 4),
            ("Atacar", "Player/Attack", 0),
            ("Melee", "Player/OnMelee", 0),
            ("Saltar", "Player/Jump", 0),
            ("Dash", "Player/Dash", 0),
            ("Interactuar", "Player/Interact", 0),
            ("Skill 1", "Player/Skill1", 0),
            ("Skill 2", "Player/Skill2", 0),
            ("Skill 3", "Player/Skill3", 0),
            ("Skill 4", "Player/Skill4", 0),
        };

        foreach (var (label, action, binding) in keybinds)
        {
            var btn = Instantiate(keybindButtonPrefab, keybindsContainer);
            btn.Initialize(label, action, binding);
        }
    }

    private void CreateGamepadKeybinds()
    {
        // ✅ KEYBINDS PARA GAMEPAD
        // [5] = Left Stick (movimiento)
        // Otras acciones según índices del gamepad
        var keybinds = new[]
        {
            ("Movimiento", "Player/Move", 5),
            ("Atacar", "Player/Attack", 1),
            ("Melee", "Player/OnMelee", 1),
            ("Saltar", "Player/Jump", 1),
            ("Dash", "Player/Dash", 1),
            ("Interactuar", "Player/Interact", 1),
            ("Skill 1", "Player/Skill1", 1),
            ("Skill 2", "Player/Skill2", 1),
            ("Skill 3", "Player/Skill3", 1),
            ("Skill 4", "Player/Skill4", 1),
        };

        foreach (var (label, action, binding) in keybinds)
        {
            var btn = Instantiate(keybindButtonPrefab, keybindsContainer);
            btn.Initialize(label, action, binding);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        isOpen = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.blocksRaycasts = false;
            pausePanelCanvasGroup.interactable = false;
        }

        if (tabsManager != null)
            tabsManager.ShowTab(0);

        // Seleccionar primer slider para que gamepad pueda navegar
        if (eventSystem != null && masterVolumeSlider != null)
        {
            var slider = masterVolumeSlider.GetComponent<Slider>();
            if (slider != null)
            {
                eventSystem.SetSelectedGameObject(slider.gameObject);
            }
        }

        Debug.Log("GameplaySettingsUI abierto");
    }

    public void Close()
    {
        isOpen = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.blocksRaycasts = true;
            pausePanelCanvasGroup.interactable = true;
        }

        gameObject.SetActive(false);

        if (settingsManager != null)
            settingsManager.SaveSettings();

        // Notificar a PauseManager que vuelva a seleccionar botón
        if (pauseManager != null)
        {
            pauseManager.OnSettingsClosed();
        }

        Debug.Log("GameplaySettingsUI cerrado");
    }
}