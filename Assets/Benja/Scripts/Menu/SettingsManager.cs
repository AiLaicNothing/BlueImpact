using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

/// <summary>
/// 🎮 SettingsManager - Gestor centralizado de configuración
/// 
/// ⚠️ UBICACIÓN: Escena de MENU (como DontDestroyOnLoad)
/// 
/// Características:
/// ✅ Integración con Audio_Manager para cambios de volumen
/// ✅ Persistencia de configuración en JSON
/// ✅ Aplicación automática de settings al cargar
/// ✅ Auto-save en cambios (configurable)
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private SettingsData currentSettings;
    private string settingsPath;
    private Audio_Manager audioManager;

    [SerializeField] private bool autoSaveOnChange = true;

    private void Awake()
    {
        // ✅ SINGLETON
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ OBTENER RUTA DE PERSISTENCIA
        settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");

        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }

        // ✅ CARGAR Y APLICAR CONFIGURACIÓN
        LoadSettings();
        ApplyAllSettings();

        Debug.Log($"✅ SettingsManager inicializado");
        Debug.Log($"📂 Ruta de configuración: {settingsPath}");
    }

    private void Start()
    {
        // ✅ OBTENER AUDIO_MANAGER (después de que ambos Awake hayan terminado)
        audioManager = Audio_Manager.Instance;
        if (audioManager == null)
        {
            Debug.LogError("❌ Audio_Manager no encontrado. Asegúrate de que esté en la escena del MENU.");
        }
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
                Debug.Log("✅ Configuración cargada desde: " + settingsPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"⚠️ Error al cargar configuración: {ex.Message}. Usando valores por defecto.");
                currentSettings = new SettingsData();
                SetDefault1920x1080();
            }
        }
        else
        {
            currentSettings = new SettingsData();
            SetDefault1920x1080();
            SaveSettings();
            Debug.Log("✅ Archivo de configuración creado por primera vez");
        }
    }

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
        Debug.LogWarning($"⚠️ 1920x1080 no disponible. Usando: {resolutions[closestIndex].width}x{resolutions[closestIndex].height}");
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSettings, true);
            File.WriteAllText(settingsPath, json);
            Debug.Log("✅ Configuración guardada");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error al guardar configuración: {ex.Message}");
        }
    }

    public void ResetToDefaults()
    {
        currentSettings = new SettingsData();
        SetDefault1920x1080();
        ApplyAllSettings();
        SaveSettings();
        Debug.Log("✅ Configuración resetada a valores por defecto");
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

        // ✅ RESOLUCIÓN
        var resolutions = Screen.resolutions;
        if (videoSettings.resolutionIndex >= 0 && videoSettings.resolutionIndex < resolutions.Length)
        {
            Resolution res = resolutions[videoSettings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, videoSettings.fullscreen);
            Debug.Log($"✅ Video: {res.width}x{res.height} | Fullscreen: {videoSettings.fullscreen}");
        }

        // ✅ BRILLO (se aplicará en tu sistema de post-processing)
        float brightness = videoSettings.brightnessLevel / 100f;
        ApplyBrightness(brightness);

        Debug.Log($"✅ Brillo: {videoSettings.brightnessLevel}% | Contraste: {videoSettings.contrastLevel}%");
    }

    private void ApplyAudioSettings()
    {
        var audioSettings = currentSettings.audio;

        // ✅ APLICAR A AUDIO_MANAGER
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(audioSettings.masterVolume);
            audioManager.SetMusicVolume(audioSettings.musicVolume);
            audioManager.SetSFXVolume(audioSettings.sfxVolume);
            audioManager.SetUIVolume(audioSettings.uiVolume);
            audioManager.SetPlayerSFXVolume(audioSettings.playerSFXVolume);
            audioManager.SetEnemySFXVolume(audioSettings.enemySFXVolume);
            audioManager.SetAmbientVolume(audioSettings.ambientVolume);

            Debug.Log($"✅ Audio settings aplicadas al AudioMixer");
        }
        else
        {
            Debug.LogWarning("⚠️ Audio_Manager no disponible, settings de audio no aplicados");
        }
    }

    private void ApplyControlsSettings()
    {
        var controlSettings = currentSettings.controls;
        Debug.Log($"✅ Sensibilidad del mouse: {controlSettings.mouseSensitivity}x");
    }

    private void ApplyGameplaySettings()
    {
        var gameplaySettings = currentSettings.gameplay;
        Debug.Log($"✅ Screen Shake: {gameplaySettings.screenShake} | Dificultad: {gameplaySettings.gameplayDifficulty}");
    }

    private void ApplyBrightness(float brightness)
    {
        // ✅ TODO: Implementar según tu sistema de post-processing
        // Ejemplo:
        // RenderSettings.ambientLight = Color.white * brightness;
        // O si usas post-processing: postProcessVolume.profile.GetSetting<Exposure>().postExposure.value = brightness;
    }

    // ==================== GETTERS ====================

    public SettingsData GetSettings() => currentSettings;

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

    // ==================== VIDEO SETTERS ====================

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

    // ==================== AUDIO SETTERS ====================

    public void SetMasterVolume(float value)
    {
        currentSettings.audio.masterVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetMasterVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetMusicVolume(float value)
    {
        currentSettings.audio.musicVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetMusicVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetSFXVolume(float value)
    {
        currentSettings.audio.sfxVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetSFXVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetUIVolume(float value)
    {
        currentSettings.audio.uiVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetUIVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetPlayerSFXVolume(float value)
    {
        currentSettings.audio.playerSFXVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetPlayerSFXVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetEnemySFXVolume(float value)
    {
        currentSettings.audio.enemySFXVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetEnemySFXVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetAmbientVolume(float value)
    {
        currentSettings.audio.ambientVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetAmbientVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetVoiceVolume(float value)
    {
        currentSettings.audio.voiceVolume = Mathf.Clamp01(value);
        if (autoSaveOnChange) SaveSettings();
    }

    // ==================== CONTROLS SETTERS ====================

    public void SetMouseSensitivity(float value)
    {
        currentSettings.controls.mouseSensitivity = Mathf.Clamp(value, 0.1f, 3f);
        if (autoSaveOnChange) SaveSettings();
    }

    public void SetInvertMouseY(bool invert)
    {
        currentSettings.controls.invertMouseY = invert;
        if (autoSaveOnChange) SaveSettings();
    }

    // ==================== GAMEPLAY SETTERS ====================

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

    public void SetGameplayDifficulty(float value)
    {
        currentSettings.gameplay.gameplayDifficulty = Mathf.Clamp01(value);
        if (autoSaveOnChange) SaveSettings();
    }
}