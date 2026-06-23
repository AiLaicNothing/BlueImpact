using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 📊 SettingsData - Estructura serializable para guardar configuración
/// 
/// Contiene:
/// ✅ Video Settings (resolución, brillo, contraste, fullscreen)
/// ✅ Audio Settings (Master, Music, SFX, UI, Player, Enemy, Ambient, Voice)
/// ✅ Controls Settings (sensibilidad, inverso)
/// ✅ Gameplay Settings (screen shake, dificultad)
/// </summary>
[System.Serializable]
public class SettingsData
{
    [System.Serializable]
    public class VideoSettings
    {
        public int resolutionIndex = 0;
        public int brightnessLevel = 100;
        public int contrastLevel = 100;
        public bool fullscreen = true;
    }

    [System.Serializable]
    public class AudioSettings
    {
        // ✅ VOLÚMENES PRINCIPALES
        public float masterVolume = 1f;        // Master → AudioMixer: "MasterVolume"
        public float musicVolume = 1f;         // Music → AudioMixer: "MusicVolume"
        public float sfxVolume = 1f;           // SFX → AudioMixer: "SFXVolume"
        public float uiVolume = 1f;            // UI → AudioMixer: "UIVolume"

        // ✅ VOLÚMENES ESPECÍFICOS (hijos de SFX en AudioMixer)
        public float playerSFXVolume = 1f;     // Player → AudioMixer: "PlayerSFXVolume"
        public float enemySFXVolume = 1f;      // Enemy → AudioMixer: "EnemySFXVolume"
        public float ambientVolume = 1f;       // Ambient → AudioMixer: "AmbientVolume"

        // ✅ OTROS
        public float voiceVolume = 0.8f;       // Para diálogos/voces si se agrega después
    }

    [System.Serializable]
    public class ControlsSettings
    {
        public float mouseSensitivity = 1f;
        public bool invertMouseY = false;
        public Dictionary<string, string> keyBindings = new();
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