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
/// Audio: Master, Music, SFX, Ambient
/// Controls: Sensibilidad, Keybinds + botón "Restaurar controles"
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

    // ==================== AUDIO (4 canales) ====================
    [Header("AUDIO")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;

    // ==================== CONTROLS ====================
    [Header("CONTROLS")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Transform keybindsContainer;
    [SerializeField] private GameObject keybindPrefab;

    /// <summary>
    /// ✅ Botón "Restaurar controles" — conectar en el Inspector.
    /// </summary>
    [SerializeField] private Button resetKeybindsButton;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private PauseManager pauseManager;

    private SettingsManager settingsManager;
    private bool isOpen = false;
    private bool isGamepadActive = false;

    public bool IsOpen => isOpen;

    // ==================== INICIALIZACIÓN ====================

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backButton != null)
            backButton.onClick.AddListener(Close);

        // ✅ CONECTAR BOTÓN RESET KEYBINDS
        if (resetKeybindsButton != null)
            resetKeybindsButton.onClick.AddListener(OnResetKeybindsClicked);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        settingsManager = SettingsManager.Instance;

        if (settingsManager == null)
            settingsManager = FindFirstObjectByType<SettingsManager>();

        InitializeAllSettings();
    }

    private void Update()
    {
        if (!isOpen) return;

        bool wasGamepadActive = isGamepadActive;

        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            isGamepadActive = true;
        else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            isGamepadActive = false;

        if (wasGamepadActive != isGamepadActive)
            CreateKeybinds();
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
        if (settingsManager == null) return;
        var settings = settingsManager.GetSettings();

        if (resolutionDropdown != null)
        {
            var resolutions = settingsManager.GetAvailableResolutions();
            resolutionDropdown.options.Clear();
            for (int i = 0; i < resolutions.Length; i++)
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(settingsManager.GetResolutionString(i)));

            resolutionDropdown.value = settings.video.resolutionIndex;
            resolutionDropdown.onValueChanged.AddListener((index) => settingsManager.SetResolution(index));
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.value = settings.video.brightnessLevel / 100f;
            brightnessSlider.onValueChanged.AddListener((value) => settingsManager.SetBrightness((int)(value * 100)));
        }

        if (contrastSlider != null)
        {
            contrastSlider.minValue = 0f;
            contrastSlider.maxValue = 1f;
            contrastSlider.value = settings.video.contrastLevel / 100f;
            contrastSlider.onValueChanged.AddListener((value) => settingsManager.SetContrast((int)(value * 100)));
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = settings.video.fullscreen;
            fullscreenToggle.onValueChanged.AddListener((value) => settingsManager.SetFullscreen(value));
        }
    }

    // ==================== AUDIO ====================

    private void InitializeAudioSettings()
    {
        if (settingsManager == null) return;
        var settings = settingsManager.GetSettings();

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = settings.audio.masterVolume;
            masterVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetMasterVolume(value));
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = settings.audio.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetMusicVolume(value));
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = settings.audio.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetSFXVolume(value));
        }

        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.minValue = 0f;
            ambientVolumeSlider.maxValue = 1f;
            ambientVolumeSlider.value = settings.audio.ambientVolume;
            ambientVolumeSlider.onValueChanged.AddListener((value) => settingsManager.SetAmbientVolume(value));
        }
    }

    // ==================== CONTROLS ====================

    private void InitializeControlsSettings()
    {
        if (settingsManager != null && mouseSensitivitySlider != null)
        {
            var settings = settingsManager.GetSettings();
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 3f;
            mouseSensitivitySlider.value = settings.controls.mouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener((value) => settingsManager.SetMouseSensitivity(value));
        }

        CreateKeybinds();
    }

    private void CreateKeybinds()
    {
        if (keybindsContainer == null || keybindPrefab == null) return;

        foreach (Transform child in keybindsContainer)
            Destroy(child.gameObject);

        if (isGamepadActive)
            CreateGamepadKeybinds();
        else
            CreateKeyboardKeybinds();
    }

    private void CreateKeyboardKeybinds()
    {
        // ✅ DISEÑO ORIGINAL DEL JUEGO — referencia fija para los jugadores
        var keybinds = new (string label, string key)[]
        {
            ("Adelante",    "W"),
            ("Atrás",       "S"),
            ("Izquierda",   "A"),
            ("Derecha",     "D"),
            ("Saltar",      "Espacio"),
            ("Melee",       "F"),
            ("Dash",        "Shift"),
            ("Interactuar", "E"),
            ("Skill 1",     "1"),
            ("Skill 2",     "2"),
            ("Skill 3",     "3"),
            ("Skill 4",     "4"),
            ("Pausa",       "ESC"),
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
            ("Movimiento",  "L-Stick"),
            ("Saltar",      "A (Sur)"),
            ("Melee",       "X (Oeste)"),
            ("Dash",        "B (Este)"),
            ("Interactuar", "Y (Norte)"),
            ("Skill 1",     "RB"),
            ("Skill 2",     "LB"),
            ("Skill 3",     "RT"),
            ("Skill 4",     "LT"),
            ("Pausa",       "Start"),
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

    // ==================== RESET KEYBINDS ====================

    /// <summary>
    /// ✅ Llamado por el botón "Restaurar controles".
    /// </summary>
    private void OnResetKeybindsClicked()
    {
        if (InputRebindingManager.Instance == null) return;

        InputRebindingManager.Instance.ResetAllBindingsToDefault();

        // Refrescar visual si hay KeybindButtons en la lista
        if (keybindsContainer != null)
        {
            var keybinds = keybindsContainer.GetComponentsInChildren<KeybindButton>();
            foreach (var kb in keybinds)
                kb.RefreshDisplay();

            // Si son prefabs simples sin KeybindButton, recrear la lista completa
            if (keybinds.Length == 0)
                CreateKeybinds();
        }

        Debug.Log("✅ Controles restaurados al diseño original");
    }

    // ==================== ABRIR / CERRAR ====================

    public void Open()
    {
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

        if (eventSystem != null && resolutionDropdown != null)
            eventSystem.SetSelectedGameObject(resolutionDropdown.gameObject);

        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameMode.Gameplay)
            GameModeManager.Instance.SetMode(GameMode.UI);

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

        if (settingsManager != null)
            settingsManager.SaveSettings();

        if (pauseManager != null)
            pauseManager.OnSettingsClosed();

        if (GameModeManager.Instance != null)
        {
            if (pauseManager != null && !pauseManager.IsPaused)
                GameModeManager.Instance.SetMode(GameMode.Gameplay);
        }

        Debug.Log("🔧 Settings cerrado (Gameplay)");
    }
}