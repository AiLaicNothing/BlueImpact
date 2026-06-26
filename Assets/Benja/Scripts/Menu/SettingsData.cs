using System.Collections.Generic;

/// <summary>
/// 📊 SettingsData - Estructura serializable de configuración
///
/// Audio simplificado: Master, Music, SFX, Ambient
/// (eliminados: UI, Player, Enemy, Voice)
/// </summary>
[System.Serializable]
public class SettingsData
{
    [System.Serializable]
    public class VideoSettings
    {
        public int resolutionIndex = 0;
        public int brightnessLevel = 100;   // 0-200 → slider 0.0-2.0
        public int contrastLevel = 100;   // 0-200 → slider 0.0-2.0
        public bool fullscreen = true;
    }

    [System.Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1f;   // AudioMixer: "MasterVolume"
        public float musicVolume = 1f;   // AudioMixer: "MusicVolume"
        public float sfxVolume = 1f;   // AudioMixer: "SFXVolume"
        public float ambientVolume = 1f;   // AudioMixer: "AmbientVolume"
    }

    [System.Serializable]
    public class ControlsSettings
    {
        public float mouseSensitivity = 1f;
        public bool invertMouseY = false;
        // keyBindings se persiste en PlayerPrefs via InputRebindingManager
    }

    [System.Serializable]
    public class GameplaySettings
    {
        public bool screenShake = true;
        public float screenShakeIntensity = 1f;
        public bool autoSave = true;
        public float gameplayDifficulty = 1f;
    }

    public VideoSettings video = new();
    public AudioSettings audio = new();
    public ControlsSettings controls = new();
    public GameplaySettings gameplay = new();
}