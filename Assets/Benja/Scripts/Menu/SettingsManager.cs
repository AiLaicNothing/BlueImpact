using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// SettingsManager - Gestor centralizado de configuración
/// DontDestroyOnLoad — colocar en escena MENU
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private SettingsData currentSettings;
    private string settingsPath;
    private Audio_Manager audioManager;
    private Resolution[] cachedBestResolutions;

    [SerializeField] private bool autoSaveOnChange = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");

        // ✅ CACHEAR RESOLUCIONES ÚNICAS CON MEJOR REFRESH RATE (MÁXIMA PRIMERO)
        cachedBestResolutions = GetBestResolutionsInternal();

        LoadSettings();
        ApplyAllSettings();

        Debug.Log($"✅ SettingsManager — ruta: {settingsPath}");
    }

    private void Start()
    {
        audioManager = Audio_Manager.Instance;
        if (audioManager == null)
            Debug.LogError("❌ Audio_Manager no encontrado.");

        // Aplicar audio ahora que Audio_Manager ya existe
        ApplyAudioSettings();
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

                // ✅ CRÍTICO: JsonUtility deja en 0 los campos que no existían
                // en el JSON (ej. archivo guardado con versión anterior del juego).
                // SanitizeAfterLoad corrige esos 0s a valores válidos.
                SanitizeAfterLoad();

                Debug.Log("✅ Configuración cargada");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"⚠️ Error al cargar: {ex.Message} — usando defaults.");
                ResetToDefaultsInternal();
            }
        }
        else
        {
            ResetToDefaultsInternal();
            SaveSettings();
            Debug.Log("✅ Primer arranque — settings.json creado");
        }
    }

    /// <summary>
    /// Corrige valores en 0 que JsonUtility deja cuando el campo no existía
    /// en el JSON guardado (versión anterior del juego, archivo corrupto, etc.)
    /// </summary>
    private void SanitizeAfterLoad()
    {
        var a = currentSettings.audio;

        // Si algún volumen está exactamente en 0 Y no hubo una sesión previa
        // donde el jugador lo bajó a 0 a propósito, lo restauramos al default.
        // Usamos -1 como centinela: si el valor es <= 0 lo tratamos como "nunca guardado".
        // Nota: el jugador SÍ puede querer 0, por eso solo lo corregimos si
        // todos los canales están en 0 al mismo tiempo (señal de JSON sin datos).
        bool allZero = a.masterVolume <= 0f && a.musicVolume <= 0f &&
                       a.sfxVolume <= 0f && a.ambientVolume <= 0f;

        if (allZero)
        {
            Debug.LogWarning("⚠️ Todos los volúmenes estaban en 0 — restaurando defaults de audio.");
            a.masterVolume = 1f;
            a.musicVolume = 1f;
            a.sfxVolume = 1f;
            a.ambientVolume = 1f;
        }

        // Sensibilidad nunca puede ser 0
        if (currentSettings.controls.mouseSensitivity <= 0f)
            currentSettings.controls.mouseSensitivity = 1f;


    }

    private void ResetToDefaultsInternal()
    {
        currentSettings = new SettingsData();
        SetDefault1920x1080();
    }

    private void SetDefault1920x1080()
    {
        // ✅ BUSCAR 1920x1080 EN LAS RESOLUCIONES FILTRADAS
        for (int i = 0; i < cachedBestResolutions.Length; i++)
        {
            if (cachedBestResolutions[i].width == 1920 && cachedBestResolutions[i].height == 1080)
            {
                currentSettings.video.resolutionIndex = i;
                return;
            }
        }

        // Fallback: la máxima resolución disponible (índice 0)
        currentSettings.video.resolutionIndex = 0;
        Debug.LogWarning("⚠️ 1920x1080 no disponible — usando máxima resolución");
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
            Debug.LogError($"❌ Error al guardar: {ex.Message}");
        }
    }

    public void ResetToDefaults()
    {
        ResetToDefaultsInternal();
        ApplyAllSettings();
        SaveSettings();
        Debug.Log("✅ Settings reseteados a defaults");
    }

    // ==================== APPLY ====================

    private void ApplyAllSettings()
    {
        ApplyVideoSettings();
        ApplyAudioSettings();
    }

    private void ApplyVideoSettings()
    {
        var v = currentSettings.video;

        // ✅ USAR RESOLUCIONES CACHEADAS (FILTRADAS)
        if (v.resolutionIndex >= 0 && v.resolutionIndex < cachedBestResolutions.Length)
        {
            var resolution = cachedBestResolutions[v.resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, v.fullscreen);
        }
    }


    private void ApplyAudioSettings()
    {
        if (audioManager == null) return;
        var a = currentSettings.audio;
        audioManager.SetMasterVolume(a.masterVolume);
        audioManager.SetMusicVolume(a.musicVolume);
        audioManager.SetSFXVolume(a.sfxVolume);
        audioManager.SetAmbientVolume(a.ambientVolume);
    }

    // ==================== GETTERS ====================

    public SettingsData GetSettings() => currentSettings;

    // ✅ OBTENER TODAS LAS RESOLUCIONES DEL SISTEMA
    public Resolution[] GetAvailableResolutions() => Screen.resolutions;

    // ✅ OBTENER RESOLUCIONES FILTRADAS (ÚNICAS + MEJOR REFRESH RATE, MÁXIMA PRIMERO)
    public Resolution[] GetBestResolutions() => cachedBestResolutions;

    // ✅ MÉTODO INTERNO PARA CACHEAR RESOLUCIONES
    private Resolution[] GetBestResolutionsInternal()
    {
        Resolution[] allResolutions = Screen.resolutions;
        var bestByResolution = new Dictionary<string, Resolution>();

        foreach (var res in allResolutions)
        {
            string key = $"{res.width}x{res.height}";

            if (!bestByResolution.ContainsKey(key) ||
                res.refreshRateRatio.numerator > bestByResolution[key].refreshRateRatio.numerator)
            {
                bestByResolution[key] = res;
            }
        }

        // ✅ ORDENAR DE MAYOR A MENOR (máxima primero)
        return bestByResolution.Values.OrderByDescending(r => r.width * r.height).ToArray();
    }

    // ✅ OBTENER STRING DE RESOLUCIÓN
    public string GetResolutionString(int index)
    {
        if (index < cachedBestResolutions.Length)
        {
            var r = cachedBestResolutions[index];
            return $"{r.width}x{r.height}";
        }
        return "Default";
    }

    // ✅ OBTENER ÍNDICE DE RESOLUCIÓN MÁXIMA
    public int GetMaxResolutionIndex() => 0;

    // ✅ OBTENER ÍNDICE DE RESOLUCIÓN ACTUAL
    public int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < cachedBestResolutions.Length; i++)
        {
            if (cachedBestResolutions[i].width == Screen.width &&
                cachedBestResolutions[i].height == Screen.height)
                return i;
        }
        return GetMaxResolutionIndex(); // Fallback a máxima
    }


    // ==================== VIDEO SETTERS ====================

    public void SetResolution(int index)
    {
        // ✅ VALIDAR CONTRA RESOLUCIONES FILTRADAS
        if (index >= 0 && index < cachedBestResolutions.Length)
        {
            currentSettings.video.resolutionIndex = index;
            ApplyVideoSettings();
            if (autoSaveOnChange) SaveSettings();
            Debug.Log($"✅ Resolución cambiada a: {GetResolutionString(index)}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Índice de resolución inválido: {index}");
        }
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

    public void SetAmbientVolume(float value)
    {
        currentSettings.audio.ambientVolume = Mathf.Clamp01(value);
        if (audioManager != null) audioManager.SetAmbientVolume(value);
        if (autoSaveOnChange) SaveSettings();
    }

    // ==================== CONTROLS SETTERS ====================

    public void SetMouseSensitivity(float value)
    {
        currentSettings.controls.mouseSensitivity = Mathf.Clamp(value, 0.1f, 1f);
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