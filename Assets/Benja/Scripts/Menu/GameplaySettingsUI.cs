using UnityEngine;
using UnityEngine.UI;

public class GameplaySettingsUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;
    [SerializeField] private TabsManager tabsManager;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;

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

        settingsManager = SettingsManager.Instance;
        InitializeAllSettings();
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

        var keybinds = new[]
        {
            ("Movimiento", "Player/Move", 0),
            ("Atacar", "Player/Attack", 0),
            ("Saltar", "Player/Jump", 0),
            ("Esquiva", "Player/Dash", 0),
            ("Interactuar", "Player/Interact", 0),
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

        Debug.Log("GameplaySettingsUI abierto");
    }

    public void Close()
    {
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

        Debug.Log("GameplaySettingsUI cerrado");
    }
}