using System;
using System.Collections.Generic;
using UnityEngine;

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
        public float masterVolume = 1f;
        public float voiceVolume = 0.8f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
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