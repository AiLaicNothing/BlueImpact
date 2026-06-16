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

    // ✅ NUEVO: Detectar dispositivo
    private bool isGamepadActive = false;

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

    private void Update()
    {
        // ✅ DETECTAR DISPOSITIVO CADA FRAME
        if (isOpen)
        {
            bool wasGamepadActive = isGamepadActive;

            if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.wasUpdatedThisFrame)
            {
                isGamepadActive = true;
            }
            else if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.wasUpdatedThisFrame)
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

        // Brillo
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

        // Contraste
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

        // Sensibilidad
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.Initialize(
                "Sensibilidad del Mouse",
                0.1f,
                3f,
                settings.controls.mouseSensitivity,
                (value) => settingsManager.SetMouseSensitivity(value),
                SettingSlider.DisplayFormat.DecimalTwoPlaces
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
        };

        foreach (var (label, action, binding) in keybinds)
        {
            var keybindButton = Instantiate(keybindButtonPrefab, keybindsContainer);
            keybindButton.Initialize(label, action, binding);
        }
    }

    private void CreateGamepadKeybinds()
    {
        // ✅ KEYBINDS PARA GAMEPAD
        // [5] = Left Stick (movimiento)
        var keybinds = new[]
        {
            ("Movimiento", "Player/Move", 5),
            ("Atacar", "Player/Attack", 1),
            ("Melee", "Player/OnMelee", 1),
            ("Saltar", "Player/Jump", 1),
            ("Dash", "Player/Dash", 1),
            ("Interactuar", "Player/Interact", 1),
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
            tabsManager.ShowTab(0);
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