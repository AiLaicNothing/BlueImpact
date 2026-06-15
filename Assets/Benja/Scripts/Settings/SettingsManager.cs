using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private SettingsData currentSettings;
    private string settingsPath;
    private InputActionAsset inputActions;

    [SerializeField] private bool autoSaveOnChange = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");

        LoadSettings();
        ApplyAllSettings();
    }

    // ==================== LOAD & SAVE ====================

    public void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                currentSettings = JsonUtility.FromJson<SettingsData>(json);
                Debug.Log("Configuración cargada desde: " + settingsPath);
            }
            catch
            {
                Debug.LogWarning("Error al cargar configuración, usando valores por defecto");
                currentSettings = new SettingsData();
            }
        }
        else
        {
            currentSettings = new SettingsData();
            SaveSettings();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSettings, true);
            File.WriteAllText(settingsPath, json);
            Debug.Log("Configuración guardada en: " + settingsPath);
        }
        catch
        {
            Debug.LogError("Error al guardar configuración");
        }
    }

    public void ResetToDefaults()
    {
        currentSettings = new SettingsData();
        ApplyAllSettings();
        SaveSettings();
    }

    // ==================== APPLY SETTINGS ====================

    private void ApplyAllSettings()
    {
        ApplyVideoSettings();
        ApplyAudioSettings();
        ApplyControlsSettings();
        ApplyGameplaySettings();
    }

    private void ApplyVideoSettings()
    {
        var videoSettings = currentSettings.video;

        // Resolución
        var resolutions = Screen.resolutions;
        if (videoSettings.resolutionIndex < resolutions.Length)
        {
            Resolution res = resolutions[videoSettings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, videoSettings.fullscreen);
        }

        // Brillo (usando post-processing o ajuste de gamma)
        float brightness = videoSettings.brightnessLevel / 100f;
        ApplyBrightness(brightness);

        Debug.Log("Video settings aplicadas");
    }

    private void ApplyAudioSettings()
    {
        var audioSettings = currentSettings.audio;

        // Aquí integrarás con tu sistema de audio
        // Ejemplo si usas AudioMixer:
        // audioMixer.SetFloat("MasterVolume", Mathf.Log10(audioSettings.masterVolume) * 20);

        Debug.Log($"Audio settings aplicadas - Master: {audioSettings.masterVolume}");
    }

    private void ApplyControlsSettings()
    {
        var controlSettings = currentSettings.controls;

        // Esto se aplicará cuando cargues el Input System
        // Por ahora, solo guardamos los datos
        Debug.Log($"Controls settings aplicadas - Sensibilidad: {controlSettings.mouseSensitivity}");
    }

    private void ApplyGameplaySettings()
    {
        var gameplaySettings = currentSettings.gameplay;
        Debug.Log($"Gameplay settings aplicadas - Dificultad: {gameplaySettings.gameplayDifficulty}");
    }

    private void ApplyBrightness(float brightness)
    {
        // Implementar según tu sistema de post-processing
        // Ejemplo simple:
        // RenderSettings.ambientLight = Color.white * brightness;
    }

    // ==================== GETTERS & SETTERS ====================

    public SettingsData GetSettings() => currentSettings;

    // Video
    public void SetResolution(int index)
    {
        currentSettings.video.resolutionIndex = index;
        ApplyVideoSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetBrightness(int level)
    {
        currentSettings.video.brightnessLevel = Mathf.Clamp(level, 0, 200);
        ApplyVideoSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetContrast(int level)
    {
        currentSettings.video.contrastLevel = Mathf.Clamp(level, 0, 200);
        ApplyVideoSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetFullscreen(bool fullscreen)
    {
        currentSettings.video.fullscreen = fullscreen;
        ApplyVideoSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    // Audio
    public void SetMasterVolume(float value)
    {
        currentSettings.audio.masterVolume = Mathf.Clamp01(value);
        ApplyAudioSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetVoiceVolume(float value)
    {
        currentSettings.audio.voiceVolume = Mathf.Clamp01(value);
        ApplyAudioSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetSFXVolume(float value)
    {
        currentSettings.audio.sfxVolume = Mathf.Clamp01(value);
        ApplyAudioSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetMusicVolume(float value)
    {
        currentSettings.audio.musicVolume = Mathf.Clamp01(value);
        ApplyAudioSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    // Controls
    public void SetMouseSensitivity(float value)
    {
        currentSettings.controls.mouseSensitivity = Mathf.Clamp(value, 0.1f, 3f);
        ApplyControlsSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetInvertMouseY(bool invert)
    {
        currentSettings.controls.invertMouseY = invert;
        ApplyControlsSettings();
        if (autoSaveOnChange) SaveSettings();
    }

    // Gameplay
    public void SetScreenShake(bool enabled)
    {
        currentSettings.gameplay.screenShake = enabled;
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetScreenShakeIntensity(float value)
    {
        currentSettings.gameplay.screenShakeIntensity = Mathf.Clamp01(value);
        if (autoSaveOnChange) SaveSettings();
    }

    // ==================== HELPERS ====================

    public Resolution[] GetAvailableResolutions() => Screen.resolutions;

    public string GetResolutionString(int index)
    {
        if (index < Screen.resolutions.Length)
        {
            var res = Screen.resolutions[index];
            return $"{res.width}x{res.height}";
        }
        return "Default";
    }

    public int GetCurrentResolutionIndex()
    {
        Resolution current = new Resolution { width = Screen.width, height = Screen.height };

        for (int i = Screen.resolutions.Length - 1; i >= 0; i--)
        {
            if (Screen.resolutions[i].width == current.width &&
                Screen.resolutions[i].height == current.height)
            {
                return i;
            }
        }
        return 0;
    }
}