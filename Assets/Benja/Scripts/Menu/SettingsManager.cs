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

        // ✅ AGREGA: Validar que settingsPath existe
        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }

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
                SetDefault1920x1080();  // ✅ AGREGA ESTO
            }
        }
        else
        {
            currentSettings = new SettingsData();
            SetDefault1920x1080();  // ✅ AGREGA ESTO
            SaveSettings();
        }
    }

    // ✅ AGREGA ESTE MÉTODO
    private void SetDefault1920x1080()
    {
        Resolution[] resolutions = Screen.resolutions;

        // Buscar 1920x1080
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == 1920 && resolutions[i].height == 1080)
            {
                currentSettings.video.resolutionIndex = i;
                Debug.Log($"✅ Resolución por defecto: 1920x1080 (índice {i})");
                return;
            }
        }

        // Si no existe, buscar la más cercana a 1920x1080
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        foreach (var res in resolutions)
        {
            float distance = Mathf.Abs(res.width - 1920) + Mathf.Abs(res.height - 1080);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = System.Array.IndexOf(resolutions, res);
            }
        }

        currentSettings.video.resolutionIndex = closestIndex;
        Debug.LogWarning($"⚠️ 1920x1080 no disponible, usando: {resolutions[closestIndex].width}x{resolutions[closestIndex].height}");
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
        SetDefault1920x1080();  // ✅ AGREGA ESTO - Resetear a 1920x1080
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
        if (videoSettings.resolutionIndex >= 0 && videoSettings.resolutionIndex < resolutions.Length)
        {
            Resolution res = resolutions[videoSettings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, videoSettings.fullscreen);
            Debug.Log($"✅ Resolución aplicada: {res.width}x{res.height} | Pantalla completa: {videoSettings.fullscreen}");
        }
        else
        {
            Debug.LogWarning("⚠️ Índice de resolución inválido");
        }

        // Brillo
        float brightness = videoSettings.brightnessLevel / 100f;  // ✅ CAMBIÉ: 10000f → 100f (rango más realista)
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
