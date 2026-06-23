using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🎮 GameplaySettingsUI - UI de configuración en escena GAMEPLAY
/// 
/// ⚠️ UBICACIÓN: Escena de GAMEPLAY
/// 
/// Características:
/// ✅ Tabs: Video, Audio, Controls
/// ✅ Control de volúmenes: Master, Music, SFX, UI, Player, Enemy, Ambient
/// ✅ Cambio de dispositivo (teclado/gamepad) en tiempo real
/// ✅ Integración con SettingsManager y PauseManager
/// </summary>
public class GameplaySettingsUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button backButton;

    [Header("Tabs")]
    [SerializeField] private TabsManager tabsManager;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;

    // ==================== VIDEO ====================
    [Header("VIDEO")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private Toggle fullscreenToggle;

    // ==================== AUDIO ====================
    [Header("AUDIO")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider uiVolumeSlider;

    // ✅ NUEVOS: Volúmenes específicos
    [SerializeField] private Slider playerSFXVolumeSlider;
    [SerializeField] private Slider enemySFXVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    // ==================== CONTROLS ====================
    [Header("CONTROLS")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Transform keybindsContainer;
    [SerializeField] private GameObject keybindPrefab;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private PauseManager pauseManager;

    private SettingsManager settingsManager;
    private PlayerInputHandler playerInputHandler;
    private bool isOpen = false;
    private bool isGamepadActive = false;

    // ✅ PROPIEDAD PÚBLICA PARA QUE PAUSEMANAGER SEPA SI ESTÁ ABIERTO
    public bool IsOpen => isOpen;

    // ==================== INICIALIZACIÓN ====================

    private void Awake()
    {
        // ✅ OBTENER CANVAS GROUP
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // ✅ CONECTAR BOTÓN VOLVER
        if (backButton != null)
            backButton.onClick.AddListener(Close);

        // ✅ OCULTAR AL INICIO
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // ✅ OBTENER REFERENCIAS
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        // ✅ BUSCAR SETTINGSMANAGER
        settingsManager = SettingsManager.Instance;

        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<SettingsManager>();

            if (settingsManager == null)
            {
                Debug.LogWarning("⚠️ SettingsManager no encontrado. Los sliders funcionarán pero sin persistencia.");
            }
        }

        InitializeAllSettings();
    }

    private void Update()
    {
        // ✅ DETECTAR CAMBIO DE DISPOSITIVO
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

    private void OnDisable()
    {
        // ✅ LIMPIAR SI ES NECESARIO
        // (PauseManager maneja la lógica de input)
    }

    private void InitializeAllSettings()
    {
        if (settingsManager == null) return;

        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeControlsSettings();
    }

    // ==================== VIDEO ====================

    private void InitializeVideoSettings()
    {
        if (settingsManager == null)
        {
            // ✅ VALORES DEFAULT
            if (resolutionDropdown != null)
            {
                resolutionDropdown.options.Clear();
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData("1920x1080"));
                resolutionDropdown.value = 0;
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.minValue = 0f;
                brightnessSlider.maxValue = 2f;
                brightnessSlider.value = 1f;
            }

            if (contrastSlider != null)
            {
                contrastSlider.minValue = 0f;
                contrastSlider.maxValue = 2f;
                contrastSlider.value = 1f;
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = false;
            }

            return;
        }

        var settings = settingsManager.GetSettings();

        // ✅ RESOLUCIÓN
        if (resolutionDropdown != null)
        {
            var resolutions = settingsManager.GetAvailableResolutions();
            resolutionDropdown.options.Clear();

            for (int i = 0; i < resolutions.Length; i++)
            {
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(settingsManager.GetResolutionString(i)));
            }

            resolutionDropdown.value = settings.video.resolutionIndex;
            resolutionDropdown.onValueChanged.AddListener((index) => settingsManager.SetResolution(index));
        }

        // ✅ BRILLO
        float brightnessNormalized = settings.video.brightnessLevel / 100f;
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = brightnessNormalized;
            brightnessSlider.onValueChanged.AddListener((value) => settingsManager.SetBrightness((int)(value * 100)));
        }

        // ✅ CONTRASTE
        float contrastNormalized = settings.video.contrastLevel / 100f;
        if (contrastSlider != null)
        {
            contrastSlider.minValue = 0f;
            contrastSlider.maxValue = 2f;
            contrastSlider.value = contrastNormalized;
            contrastSlider.onValueChanged.AddListener((value) => settingsManager.SetContrast((int)(value * 100)));
        }

        // ✅ PANTALLA COMPLETA
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = settings.video.fullscreen;
            fullscreenToggle.onValueChanged.AddListener((value) => settingsManager.SetFullscreen(value));
        }
    }

    // ==================== AUDIO ====================

    private void InitializeAudioSettings()
    {
        // ✅ SI NO HAY SETTINGS, USAR VALORES DEFAULT
        if (settingsManager == null)
        {
            //SetAudioDefaults();
            return;
        }

        var settings = settingsManager.GetSettings();

        // ✅ MASTER VOLUME
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = settings.audio.masterVolume;
            masterVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetMasterVolume(value));
        }

        // ✅ MUSIC VOLUME
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = settings.audio.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetMusicVolume(value));
        }

        // ✅ SFX VOLUME
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = settings.audio.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetSFXVolume(value));
        }

        // ✅ UI VOLUME
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.minValue = 0f;
            uiVolumeSlider.maxValue = 1f;
            uiVolumeSlider.value = settings.audio.uiVolume;
            uiVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetUIVolume(value));
        }

        // ✅ PLAYER SFX VOLUME
        if (playerSFXVolumeSlider != null)
        {
            playerSFXVolumeSlider.minValue = 0f;
            playerSFXVolumeSlider.maxValue = 1f;
            playerSFXVolumeSlider.value = settings.audio.playerSFXVolume;
            playerSFXVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetPlayerSFXVolume(value));
        }

        // ✅ ENEMY SFX VOLUME
        if (enemySFXVolumeSlider != null)
        {
            enemySFXVolumeSlider.minValue = 0f;
            enemySFXVolumeSlider.maxValue = 1f;
            enemySFXVolumeSlider.value = settings.audio.enemySFXVolume;
            enemySFXVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetEnemySFXVolume(value));
        }

        // ✅ AMBIENT VOLUME
        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.minValue = 0f;
            ambientVolumeSlider.maxValue = 1f;
            ambientVolumeSlider.value = settings.audio.ambientVolume;
            ambientVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetAmbientVolume(value));
        }

        // ✅ VOICE VOLUME
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.minValue = 0f;
            voiceVolumeSlider.maxValue = 1f;
            voiceVolumeSlider.value = settings.audio.voiceVolume;
            voiceVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetVoiceVolume(value));
        }
    }

    //private void SetAudioDefaults()
    //{
    //    float[] sliders = { masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider, uiVolumeSlider,
    //                       playerSFXVolumeSlider, enemySFXVolumeSlider, ambientVolumeSlider, voiceVolumeSlider }
    //                       .Where(s => s != null).ToArray();

    //    foreach (var slider in sliders)
    //    {
    //        slider.minValue = 0f;
    //        slider.maxValue = 1f;
    //        slider.value = 0.8f;
    //    }
    //}

    // ==================== CONTROLS ====================

    private void InitializeControlsSettings()
    {
        if (settingsManager == null)
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = 0.1f;
                mouseSensitivitySlider.maxValue = 3f;
                mouseSensitivitySlider.value = 1.5f;
            }
            CreateKeybinds();
            return;
        }

        var settings = settingsManager.GetSettings();

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 3f;
            mouseSensitivitySlider.value = settings.controls.mouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener((value) => settingsManager.SetMouseSensitivity(value));
        }

        CreateKeybinds();
    }

    private void CreateKeybinds()
    {
        if (keybindsContainer == null || keybindPrefab == null)
            return;

        // ✅ LIMPIAR
        foreach (Transform child in keybindsContainer)
            Destroy(child.gameObject);

        // ✅ CREAR SEGÚN DISPOSITIVO
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
        var keybinds = new (string label, string key)[]
        {
            ("Adelante", "W"),
            ("Atrás", "S"),
            ("Izquierda", "A"),
            ("Derecha", "D"),
            ("Saltar", "Space"),
            ("Melee", "F"),
            ("Dash", "LeftShift"),
            ("Interactuar", "E"),
            ("Skill 1", "1"),
            ("Skill 2", "2"),
            ("Skill 3", "3"),
            ("Skill 4", "4"),
            ("Pausa", "ESC"),
        };

        foreach (var (label, key) in keybinds)
        {
            var obj = Instantiate(keybindPrefab, keybindsContainer);
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = label;
                texts[1].text = key;
            }
        }
    }

    private void CreateGamepadKeybinds()
    {
        var keybinds = new (string label, string key)[]
        {
            ("Movimiento", "Left Stick"),
            ("Saltar", "South (A)"),
            ("Melee", "West (X)"),
            ("Dash", "East (B)"),
            ("Interactuar", "North (Y)"),
            ("Skill 1", "RB"),
            ("Skill 2", "LB"),
            ("Skill 3", "RT"),
            ("Skill 4", "LT"),
            ("Pausa", "Start"),
        };

        foreach (var (label, key) in keybinds)
        {
            var obj = Instantiate(keybindPrefab, keybindsContainer);
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = label;
                texts[1].text = key;
            }
        }
    }

    // ==================== ABRIR / CERRAR ====================

    public void Open()
    {
        isOpen = true;

        // ✅ MOSTRAR PANEL
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // ✅ OCULTAR PAUSE PANEL
        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.blocksRaycasts = false;
            pausePanelCanvasGroup.interactable = false;
        }

        // ✅ MOSTRAR TAB 0 (VIDEO)
        if (tabsManager != null)
            tabsManager.ShowTab(0);

        // ✅ SELECCIONAR PRIMER SLIDER PARA GAMEPAD
        if (eventSystem != null && resolutionDropdown != null)
        {
            eventSystem.SetSelectedGameObject(resolutionDropdown.gameObject);
        }

        // ✅ CAMBIAR A UI SI ESTAMOS EN GAMEPLAY
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameMode.Gameplay)
        {
            GameModeManager.Instance.SetMode(GameMode.UI);
        }

        Debug.Log("🔧 Settings abierto (Gameplay)");
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

        // ✅ GUARDAR SOLO SI EXISTE SETTINGSMANAGER
        if (settingsManager != null)
            settingsManager.SaveSettings();

        if (pauseManager != null)
        {
            pauseManager.OnSettingsClosed();
        }

        // ✅ REGRESAR A GAMEPLAY SOLO SI NO ESTAMOS EN PAUSA
        if (GameModeManager.Instance != null)
        {
            if (pauseManager != null && !pauseManager.IsPaused)
            {
                GameModeManager.Instance.SetMode(GameMode.Gameplay);
            }
            // Si estamos en pausa, mantener UI (PauseManager lo manejará al resumir)
        }

        Debug.Log("🔧 Settings cerrado (Gameplay)");
    }
}