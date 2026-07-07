using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private EventSystem eventSystem;
    private bool isOpen = false;
    private bool isGamepadActive = false;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        eventSystem = EventSystem.current;

        if (backButton != null)
            backButton.onClick.AddListener(Close);

        if (resetKeybindsButton != null)
            resetKeybindsButton.onClick.AddListener(OnResetKeybindsClicked);

        HidePanel();

        settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
            settingsManager = FindFirstObjectByType<SettingsManager>();

        Debug.Log($"✅ Awake: settingsManager = {(settingsManager != null ? "ENCONTRADO" : "NULL")}");
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

    private void InitializeVideoSettings()
    {
        Debug.Log("📹 InitializeVideoSettings comenzó");
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

        Debug.Log("📹 InitializeVideoSettings completado");
    }

    private void InitializeAudioSettings()
    {
        Debug.Log("🔊 InitializeAudioSettings comenzó");
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

        Debug.Log("🔊 InitializeAudioSettings completado");
    }

    private void InitializeControlsSettings()
    {
        Debug.Log("🎮 InitializeControlsSettings comenzó");
        Debug.Log($"  → settingsManager: {(settingsManager != null ? "OK" : "NULL")}");
        Debug.Log($"  → mouseSensitivitySlider: {(mouseSensitivitySlider != null ? "OK" : "NULL")}");

        if (settingsManager != null && mouseSensitivitySlider != null)
        {
            var c = settingsManager.GetSettings().controls;
            mouseSensitivitySlider.Initialize("Sensibilidad del mouse", 0.1f, 1.0f,
                Mathf.Clamp(c.mouseSensitivity, 0.1f, 1.0f),
                v => settingsManager.SetMouseSensitivity(v),
                SettingSlider.DisplayFormat.Decimal);
            Debug.Log("  → Slider de sensibilidad inicializado ✅");
        }

        Debug.Log("🎮 Llamando RebuildKeybindList...");
        RebuildKeybindList();
        Debug.Log("🎮 InitializeControlsSettings completado");
    }

    private void RebuildKeybindList()
    {
        Debug.Log($"  → keybindsContainer: {(keybindsContainer != null ? "OK" : "NULL")}");
        Debug.Log($"  → keybindButtonPrefab: {(keybindButtonPrefab != null ? "OK" : "NULL")}");

        if (keybindsContainer == null || keybindButtonPrefab == null)
        {
            Debug.LogError("❌ No se puede crear keybinds — referencias nulas");
            return;
        }

        foreach (Transform child in keybindsContainer)
            Destroy(child.gameObject);

        var keybinds = isGamepadActive ? GetGamepadKeybinds() : GetKeyboardKeybinds();
        Debug.Log($"  → Creando {keybinds.Length} keybinds...");

        int count = 0;
        foreach (var (label, action, binding) in keybinds)
        {
            var btn = Instantiate(keybindButtonPrefab, keybindsContainer);
            btn.Initialize(label, action, binding);
            count++;
        }
        Debug.Log($"  → ✅ {count} keybinds creados");
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
        var btns = keybindsContainer.GetComponentsInChildren<KeybindButton>();
        foreach (var btn in btns)
            btn.RefreshDisplay();
        Debug.Log("✅ Controles restaurados");
    }

    public void Open()
    {
        Debug.Log("═══ MenuSettingsUI.Open() COMIENZA ═══");
        gameObject.SetActive(true);
        isOpen = true;
        ShowPanel();
        BlockSiblingPanel(true);

        if (tabsManager != null)
            tabsManager.ShowTab(0);

        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeControlsSettings();

        if (eventSystem != null && resolutionDropdown != null)
            eventSystem.SetSelectedGameObject(resolutionDropdown.gameObject);

        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameMode.Gameplay)
            GameModeManager.Instance.SetMode(GameMode.UI);

        Debug.Log("═══ MenuSettingsUI.Open() TERMINA ═══");
    }

    public void Close()
    {
        isOpen = false;
        HidePanel();
        BlockSiblingPanel(false);

        if (settingsManager != null)
            settingsManager.SaveSettings();

        if (pauseManager != null)
            pauseManager.OnSettingsClosed();

        if (GameModeManager.Instance != null && pauseManager != null && !pauseManager.IsPaused)
            GameModeManager.Instance.SetMode(GameMode.Gameplay);

        gameObject.SetActive(false);
    }

    public void NextTab() { if (tabsManager != null) tabsManager.NextTab(); }
    public void PreviousTab() { if (tabsManager != null) tabsManager.PreviousTab(); }

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