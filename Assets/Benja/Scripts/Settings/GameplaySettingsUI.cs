using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplaySettingsUI : SettingsPanelUI
{
    [SerializeField] private CanvasGroup audioTabContent;
    [SerializeField] private CanvasGroup controlsTabContent;
    [SerializeField] private CanvasGroup gameplayTabContent;

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

    // GAMEPLAY
    [SerializeField] private SettingToggle screenShakeToggle;
    [SerializeField] private SettingSlider screenShakeIntensitySlider;
    [SerializeField] private SettingToggle autoSaveToggle;
    [SerializeField] private Button resetToDefaultButton;

    private SettingsManager settingsManager;

    protected override void Awake()
    {
        base.Awake();

        settingsManager = SettingsManager.Instance;

        if (resetToDefaultButton != null)
            resetToDefaultButton.onClick.AddListener(ResetToDefaults);

        InitializeAllSettings();
    }

    private void InitializeAllSettings()
    {
        InitializeAudioSettings();
        InitializeControlsSettings();
        InitializeGameplaySettings();
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

        // Crear botones para las teclas principales (iguales al menú)
        var keybinds = new[]
        {
            ("Movimiento", "Player/Move", 0),
            ("Atacar", "Player/Attack", 0),
            ("Saltar", "Player/Jump", 0),
            ("Esquiva", "Player/Dash", 0),
            ("Interactuar", "Player/Interact", 0),
            ("Agacharse", "Player/Crouch", 0),
            ("Habilidad 1", "Player/Skill1", 0),
            ("Habilidad 2", "Player/Skill2", 0),
            ("Habilidad 3", "Player/Skill3", 0),
            ("Habilidad 4", "Player/Skill4", 0),
        };

        foreach (var (label, action, binding) in keybinds)
        {
            var keybindButton = Instantiate(keybindButtonPrefab, keybindsContainer);
            keybindButton.Initialize(label, action, binding);
        }
    }

    private void InitializeGameplaySettings()
    {
        var settings = settingsManager.GetSettings();

        screenShakeToggle.Initialize(
            "Screen Shake",
            settings.gameplay.screenShake,
            (value) => settingsManager.SetScreenShake(value)
        );

        screenShakeIntensitySlider.Initialize(
            "Intensidad del Screen Shake",
            0,
            1,
            settings.gameplay.screenShakeIntensity,
            (value) => settingsManager.SetScreenShakeIntensity(value)
        );

        // Auto-save es solo lectura en gameplay (no se cambia aquí)
        autoSaveToggle.Initialize(
            "Auto-guardado",
            settings.gameplay.autoSave,
            (_) => { } // Sin callback
        );
    }

    private void ResetToDefaults()
    {
        // Mostrar confirmación
        if (ConfirmDialogUI.Instance != null)
        {
            ConfirmDialogUI.Instance.Show(
                "¿Resetear todas las opciones a valores por defecto?",
                () =>
                {
                    settingsManager.ResetToDefaults();
                    InitializeAllSettings();
                    RefreshAllSettings();
                }
            );
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
        var keybinds = keybindsContainer.GetComponentsInChildren<KeybindButton>();
        foreach (var keybind in keybinds)
        {
            keybind.RefreshDisplay();
        }
    }
}