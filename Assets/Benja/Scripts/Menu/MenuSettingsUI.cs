using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuSettingsUI : MonoBehaviour
{
    [Header("Canvas / Navigation")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;
    [SerializeField] private TabsManager tabsManager;
    [SerializeField] private CanvasGroup menuPanelCanvasGroup;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;

    // Opcional: solo se asigna si los ajustes se abren desde la pausa
    [SerializeField] private PauseManager pauseManager;

    [Header("VIDEO")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("AUDIO")]
    [SerializeField] private SettingSlider masterVolumeSlider;
    [SerializeField] private SettingSlider musicVolumeSlider;
    [SerializeField] private SettingSlider sfxVolumeSlider;
    [SerializeField] private SettingSlider ambientVolumeSlider;

    [Header("CONTROLS")]
    [SerializeField] private SettingSlider mouseSensitivitySlider;
    [SerializeField] private Transform keybindsContainer;
    [SerializeField] private KeybindButton keybindButtonPrefab;
    [SerializeField] private Button resetKeybindsButton;

    private SettingsManager settingsManager;
    private bool isOpen = false;
    private bool isGamepadActive = false;
    private bool wasGameplayMode = false;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backButton != null)
            backButton.onClick.AddListener(Close);

        if (resetKeybindsButton != null)
            resetKeybindsButton.onClick.AddListener(OnResetKeybindsClicked);

        HidePanel();

        settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
            settingsManager = FindFirstObjectByType<SettingsManager>();
    }

    private void OnDisable()
    {
        // Si el objeto se desactiva con los ajustes abiertos, libera el cursor
        if (isOpen)
        {
            isOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        bool wasGamepad = isGamepadActive;
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            isGamepadActive = true;
        else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            isGamepadActive = false;

        if (wasGamepad != isGamepadActive)
            RebuildKeybindList();
    }

    // ---------- KEYBINDS ----------

    private void RebuildKeybindList()
    {
        if (keybindsContainer == null || keybindButtonPrefab == null)
        {
            Debug.LogError("❌ No se puede crear keybinds — referencias nulas");
            return;
        }

        foreach (Transform child in keybindsContainer)
            Destroy(child.gameObject);

        var keybinds = isGamepadActive ? GetGamepadKeybinds() : GetKeyboardKeybinds();

        foreach (var (label, action, binding) in keybinds)
        {
            var btn = Instantiate(keybindButtonPrefab, keybindsContainer);
            btn.Initialize(label, action, binding);
        }
    }

    private (string label, string action, int binding)[] GetKeyboardKeybinds() => new[]
    {
        ("Adelante",    "Player/Move",     1),
        ("Izquierda",   "Player/Move",     3),
        ("Atrás",       "Player/Move",     2),
        ("Derecha",     "Player/Move",     4),
        ("Atacar",      "Player/Attack",   0),
        ("Melee",       "Player/OnMelee",  0),
        ("Saltar",      "Player/Jump",     0),
        ("Dash",        "Player/Dash",     0),
        ("Interactuar", "Player/Interact", 0),
        ("Fijar Objetivo", "Player/LockOnTarget", 0),
        ("Skill 1",     "Player/Skill1",   0),
        ("Skill 2",     "Player/Skill2",   0),
        ("Skill 3",     "Player/Skill3",   0),
        ("Skill 4",     "Player/Skill4",   0),
    };

    private (string label, string action, int binding)[] GetGamepadKeybinds() => new[]
    {
        ("Movimiento",  "Player/Move",     5),
        ("Atacar",      "Player/Attack",   1),
        ("Melee",       "Player/OnMelee",  1),
        ("Saltar",      "Player/Jump",     1),
        ("Dash",        "Player/Dash",     1),
        ("Interactuar", "Player/Interact", 1),
        ("Fijar Objetivo", "Player/LockOnTarget", 1),
        ("Skill 1",     "Player/Skill1",   1),
        ("Skill 2",     "Player/Skill2",   1),
        ("Skill 3",     "Player/Skill3",   1),
        ("Skill 4",     "Player/Skill4",   1),
    };

    private void OnResetKeybindsClicked()
    {
        if (InputRebindingManager.Instance == null) return;

        InputRebindingManager.Instance.ResetAllBindingsToDefault();

        foreach (var btn in keybindsContainer.GetComponentsInChildren<KeybindButton>())
            btn.RefreshDisplay();
    }

    // ---------- OPEN / CLOSE ----------
    // Quita de la clase: pauseManager, wasGameplayMode y modeBeforeOpen

    public void Open()
    {
        if (isOpen) return;

        gameObject.SetActive(true);
        isOpen = true;

        ShowPanel();
        BlockSiblingPanel(true);

        if (tabsManager != null)
            tabsManager.ShowTab(0);

        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeControlsSettings();

        // La pila decide el modo de juego; no lo cambiamos aquí
        GamepadCursor.Instance?.Acquire(this, Close);
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        HidePanel();
        BlockSiblingPanel(false);

        if (settingsManager != null)
            settingsManager.SaveSettings();

        // Release sincroniza el modo con lo que quede debajo (pausa, menú o gameplay)
        GamepadCursor.Instance?.Release(this);

        gameObject.SetActive(false);
    }
    public void NextTab() { if (tabsManager != null) tabsManager.NextTab(); }
    public void PreviousTab() { if (tabsManager != null) tabsManager.PreviousTab(); }

    // ---------- INITIALIZATION ----------

    private void InitializeVideoSettings()
    {
        if (settingsManager == null) return;
        var s = settingsManager.GetSettings();

        if (resolutionDropdown != null)
        {
            var resolutions = settingsManager.GetAvailableResolutions();
            resolutionDropdown.options.Clear();
            for (int i = 0; i < resolutions.Length; i++)
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(settingsManager.GetResolutionString(i)));
            resolutionDropdown.SetValueWithoutNotify(s.video.resolutionIndex);
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(i => settingsManager.SetResolution(i));
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(s.video.fullscreen);
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(v => settingsManager.SetFullscreen(v));
        }
    }

    private void InitializeAudioSettings()
    {
        if (settingsManager == null) return;
        var a = settingsManager.GetSettings().audio;

        if (masterVolumeSlider != null)
            masterVolumeSlider.Initialize("Volumen Global", 0f, 1f, a.masterVolume,
                v => settingsManager.SetMasterVolume(v), SettingSlider.DisplayFormat.Percentage);

        if (musicVolumeSlider != null)
            musicVolumeSlider.Initialize("Música", 0f, 1f, a.musicVolume,
                v => settingsManager.SetMusicVolume(v), SettingSlider.DisplayFormat.Percentage);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.Initialize("Efectos de sonido", 0f, 1f, a.sfxVolume,
                v => settingsManager.SetSFXVolume(v), SettingSlider.DisplayFormat.Percentage);

        if (ambientVolumeSlider != null)
            ambientVolumeSlider.Initialize("Ambiente", 0f, 1f, a.ambientVolume,
                v => settingsManager.SetAmbientVolume(v), SettingSlider.DisplayFormat.Percentage);
    }

    private void InitializeControlsSettings()
    {
        if (settingsManager != null && mouseSensitivitySlider != null)
        {
            var c = settingsManager.GetSettings().controls;
            mouseSensitivitySlider.Initialize("Sensibilidad del mouse", 0.1f, 1.0f,
                Mathf.Clamp(c.mouseSensitivity, 0.1f, 1.0f),
                v => settingsManager.SetMouseSensitivity(v),
                SettingSlider.DisplayFormat.Decimal);
        }

        RebuildKeybindList();
    }

    private void ShowPanel()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void BlockSiblingPanel(bool block)
    {
        SetCanvasGroup(menuPanelCanvasGroup, block);
        SetCanvasGroup(pausePanelCanvasGroup, block);
    }

    private void SetCanvasGroup(CanvasGroup cg, bool block)
    {
        if (cg == null) return;
        cg.interactable = !block;
        cg.blocksRaycasts = !block;
    }
}