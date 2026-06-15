using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuSettingsUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;
    [SerializeField] private TabsManager tabsManager;

    [SerializeField] private CanvasGroup mainMenuPanelCanvasGroup;

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
    private bool isOpen = false;

    private void Awake()
    {
        // Obtener CanvasGroup si no está asignado
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Conectar botón Back
        if (backButton != null)
            backButton.onClick.AddListener(Close);

        // Inicializar oculto
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
        if (settingsManager == null)
        {
            Debug.LogError("SettingsManager no encontrado");
            return;
        }

        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeControlsSettings();
    }

    private void InitializeVideoSettings()
    {
        if (settingsManager == null) return;

        var settings = settingsManager.GetSettings();

        // Resolución
        if (resolutionDropdown != null)
        {
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
        }

        // Brillo - CORREGIDO: 0-100 en lugar de 0-200
        // Si el valor guardado es 0-200, lo normalizamos a 0-1
        float brightnessNormalized = settings.video.brightnessLevel / 100f;
        if (brightnessSlider != null)
        {
            brightnessSlider.Initialize(
                "Brillo",
                0f,
                1f,
                brightnessNormalized,
                (value) => settingsManager.SetBrightness((int)(value * 100)),
                SettingSlider.DisplayFormat.Percentage
            );
        }

        // Contraste - CORREGIDO: 0-100 en lugar de 0-200
        float contrastNormalized = settings.video.contrastLevel / 100f;
        if (contrastSlider != null)
        {
            contrastSlider.Initialize(
                "Contraste",
                0f,
                1f,
                contrastNormalized,
                (value) => settingsManager.SetContrast((int)(value * 100)),
                SettingSlider.DisplayFormat.Percentage
            );
        }

        // Pantalla completa
        if (fullscreenToggle != null)
        {
            fullscreenToggle.Initialize(
                "Pantalla Completa",
                settings.video.fullscreen,
                (value) => settingsManager.SetFullscreen(value)
            );
        }
    }

    private void InitializeAudioSettings()
    {
        if (settingsManager == null) return;

        var settings = settingsManager.GetSettings();

        // Todos los volúmenes van de 0-1, mostrados como porcentaje
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.Initialize(
                "Volumen Global",
                0f,
                1f,
                settings.audio.masterVolume,
                (value) => settingsManager.SetMasterVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.Initialize(
                "Volumen de Voz",
                0f,
                1f,
                settings.audio.voiceVolume,
                (value) => settingsManager.SetVoiceVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.Initialize(
                "Volumen de Efectos",
                0f,
                1f,
                settings.audio.sfxVolume,
                (value) => settingsManager.SetSFXVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.Initialize(
                "Volumen de Música",
                0f,
                1f,
                settings.audio.musicVolume,
                (value) => settingsManager.SetMusicVolume(value),
                SettingSlider.DisplayFormat.Percentage
            );
        }
    }

    private void InitializeControlsSettings()
    {
        if (settingsManager == null) return;

        var settings = settingsManager.GetSettings();

        // Sensibilidad - CORREGIDO: mostrar como decimales (0.1, 0.2, 0.3, etc)
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.Initialize(
                "Sensibilidad del Mouse",
                0.1f,
                3f,
                settings.controls.mouseSensitivity,
                (value) => settingsManager.SetMouseSensitivity(value),
                SettingSlider.DisplayFormat.DecimalTwoPlaces // Mostrar como 0.10, 0.20, etc
            );
        }

        if (invertMouseYToggle != null)
        {
            invertMouseYToggle.Initialize(
                "Invertir Eje Y del Mouse",
                settings.controls.invertMouseY,
                (value) => settingsManager.SetInvertMouseY(value)
            );
        }

        CreateKeybinds();
    }

    private void CreateKeybinds()
    {
        if (keybindsContainer == null || keybindButtonPrefab == null)
            return;

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

        if (mainMenuPanelCanvasGroup != null)
        {
            mainMenuPanelCanvasGroup.blocksRaycasts = false;
            mainMenuPanelCanvasGroup.interactable = false;
        }

        if (tabsManager != null)
        {
            tabsManager.ShowTab(0); // Mostrar primer tab
        }

        RefreshAllSettings();

        Debug.Log("MenuSettingsUI abierto");
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

        if (mainMenuPanelCanvasGroup != null)
        {
            mainMenuPanelCanvasGroup.blocksRaycasts = true;
            mainMenuPanelCanvasGroup.interactable = true;
        }

        gameObject.SetActive(false);

        if (settingsManager != null)
        {
            settingsManager.SaveSettings();
        }

        Debug.Log("MenuSettingsUI cerrado");
    }

    public void NextTab()
    {
        if (tabsManager != null)
            tabsManager.NextTab();
    }

    public void PreviousTab()
    {
        if (tabsManager != null)
            tabsManager.PreviousTab();
    }

    private void RefreshAllSettings()
    {
        // Actualizar todos los valores mostrados
        if (keybindsContainer != null)
        {
            var keybinds = keybindsContainer.GetComponentsInChildren<KeybindButton>();
            foreach (var keybind in keybinds)
            {
                keybind.RefreshDisplay();
            }
        }
    }
}