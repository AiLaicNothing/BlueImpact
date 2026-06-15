using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuSettingsUI : SettingsPanelUI
{
    [SerializeField] private CanvasGroup videoTabContent;
    [SerializeField] private CanvasGroup audioTabContent;
    [SerializeField] private CanvasGroup controlsTabContent;

    // VIDEO
    [SerializeField] private SettingDropdown resolutionDropdown;
    [SerializeField] private SettingSlider brightnessSlider;
    [SerializeField] private SettingSlider contrastSlider;
    [SerializeField] private SettingToggle fullscreenToggle;

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

    private SettingsManager settingsManager;

    protected override void Awake()
    {
        base.Awake();

        settingsManager = SettingsManager.Instance;
        InitializeAllSettings();
    }

    private void InitializeAllSettings()
    {
        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeControlsSettings();
    }

    private void InitializeVideoSettings()
    {
        var settings = settingsManager.GetSettings();

        // Resolución
        var resolutions = settingsManager.GetAvailableResolutions();
        string[] resolutionStrings = new string[resolutions.Length];
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionStrings[i] = settingsManager.GetResolutionString(i);
        }

        resolutionDropdown.Initialize(
            "Resolución",
            resolutionStrings,
            settings.video.resolutionIndex,
            (index) => settingsManager.SetResolution(index)
        );

        // Brillo
        brightnessSlider.Initialize(
            "Brillo",
            0,
            200,
            settings.video.brightnessLevel,
            (value) => settingsManager.SetBrightness((int)value)
        );

        // Contraste
        contrastSlider.Initialize(
            "Contraste",
            0,
            200,
            settings.video.contrastLevel,
            (value) => settingsManager.SetContrast((int)value)
        );

        // Pantalla completa
        fullscreenToggle.Initialize(
            "Pantalla Completa",
            settings.video.fullscreen,
            (value) => settingsManager.SetFullscreen(value)
        );
    }

    private void InitializeAudioSettings()
    {
        var settings = settingsManager.GetSettings();

        masterVolumeSlider.Initialize(
            "Volumen Global",
            0,
            1,
            settings.audio.masterVolume,
            (value) => settingsManager.SetMasterVolume(value)
        );

        voiceVolumeSlider.Initialize(
            "Volumen de Voz",
            0,
            1,
            settings.audio.voiceVolume,
            (value) => settingsManager.SetVoiceVolume(value)
        );

        sfxVolumeSlider.Initialize(
            "Volumen de Efectos",
            0,
            1,
            settings.audio.sfxVolume,
            (value) => settingsManager.SetSFXVolume(value)
        );

        musicVolumeSlider.Initialize(
            "Volumen de Música",
            0,
            1,
            settings.audio.musicVolume,
            (value) => settingsManager.SetMusicVolume(value)
        );
    }

    private void InitializeControlsSettings()
    {
        var settings = settingsManager.GetSettings();

        mouseSensitivitySlider.Initialize(
            "Sensibilidad del Mouse",
            0.1f,
            3f,
            settings.controls.mouseSensitivity,
            (value) => settingsManager.SetMouseSensitivity(value)
        );

        invertMouseYToggle.Initialize(
            "Invertir Eje Y del Mouse",
            settings.controls.invertMouseY,
            (value) => settingsManager.SetInvertMouseY(value)
        );

        CreateKeybinds();
    }

    private void CreateKeybinds()
    {
        // Limpiar contenedor
        foreach (Transform child in keybindsContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear botones para las teclas principales
        var keybinds = new[]
        {
            ("Movimiento Adelante", "Player/Move", 0),
            ("Atacar", "Player/Attack", 0),
            ("Saltar", "Player/Jump", 0),
            ("Esquiva", "Player/Dash", 0),
            ("Interactuar", "Player/Interact", 0),
            ("Agacharse", "Player/Crouch", 0),
        };

        foreach (var (label, action, binding) in keybinds)
        {
            var keybindButton = Instantiate(keybindButtonPrefab, keybindsContainer);
            keybindButton.Initialize(label, action, binding);
        }
    }

    protected override void OnBackPressed()
    {
        settingsManager.SaveSettings();
        Close();
    }

    public override void Open()
    {
        base.Open();
        RefreshAllSettings();
    }

    private void RefreshAllSettings()
    {
        // Actualizar todos los valores mostrados
        var keybinds = keybindsContainer.GetComponentsInChildren<KeybindButton>();
        foreach (var keybind in keybinds)
        {
            keybind.RefreshDisplay();
        }
    }
}