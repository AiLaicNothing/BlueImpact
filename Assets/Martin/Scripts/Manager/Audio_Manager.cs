using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// 🎵 Audio_Manager - Gestor centralizado de audio
/// 
/// ⚠️ UBICACIÓN: Escena de MENU (como DontDestroyOnLoad)
/// 
/// Características:
/// ✅ Integración con AudioMixer
/// ✅ Control de volumen por canales (Master, Music, SFX, UI, Player, Enemy, Ambient)
/// ✅ Reproducción de música con fade transitions
/// ✅ Pool de AudioSources para efectos simultáneos
/// ✅ Sonido 3D posicional
/// ✅ Pausa/Reanudación de audio
/// ✅ Persistencia entre escenas
/// </summary>
public class Audio_Manager : MonoBehaviour
{
    public static Audio_Manager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup playerSFXGroup;
    [SerializeField] private AudioMixerGroup enemySFXGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private int sfxPoolSize = 16;

    private AudioSource[] sfxPool;
    private int currentPoolIndex = 0;
    private AudioSource currentMusicSource;
    private bool isMusicTransitioning = false;

    // ==================== INICIALIZACIÓN ====================

    private void Awake()
    {
        // ✅ SINGLETON - DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ CREAR MÚSICA SOURCE
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
        }

        // ✅ CREAR POOL DE SFX
        InitializeSFXPool();

        // ✅ SINCRONIZAR VOLÚMENES CON AUDIO MIXER
        SyncVolumesWithMixer();

        Debug.Log("✅ Audio_Manager inicializado (escena: MENU)");
    }

    private void InitializeSFXPool()
    {
        sfxPool = new AudioSource[sfxPoolSize];

        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].outputAudioMixerGroup = sfxGroup;
            sfxPool[i].playOnAwake = false;
        }

        Debug.Log($"✅ SFX Pool creado con {sfxPoolSize} AudioSources");
    }

    private void SyncVolumesWithMixer()
    {
        // ✅ SINCRONIZAR TODOS LOS VOLÚMENES AL INICIAR
        // Los parámetros deben existir en tu AudioMixer
        audioMixer.GetFloat("MasterVolume", out float masterVol);
        audioMixer.GetFloat("MusicVolume", out float musicVol);
        audioMixer.GetFloat("SFXVolume", out float sfxVol);
        audioMixer.GetFloat("UIVolume", out float uiVol);

        Debug.Log($"🔊 Volúmenes sincronizados desde AudioMixer");
    }

    // ==================== REPRODUCCIÓN DE MÚSICA ====================

    /// <summary>
    /// Cambiar música con fade transition
    /// </summary>
    public void PlayMusic(AudioClip musicClip, float fadeDuration = 1f)
    {
        if (musicClip == null) return;

        StartCoroutine(FadeMusicTransition(musicClip, fadeDuration));
    }

    private IEnumerator FadeMusicTransition(AudioClip newClip, float fadeDuration)
    {
        isMusicTransitioning = true;

        // ✅ FADE OUT música actual
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(musicSource, musicSource.volume, 0f, fadeDuration));
            musicSource.Stop();
        }

        // ✅ CAMBIAR CLIP Y PLAY
        musicSource.clip = newClip;
        musicSource.time = 0f;
        musicSource.Play();

        // ✅ FADE IN música nueva
        yield return StartCoroutine(FadeAudioSource(musicSource, 0f, 1f, fadeDuration));

        isMusicTransitioning = false;
    }

    private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, endVolume, t);
            yield return null;
        }

        source.volume = endVolume;
    }

    /// <summary>
    /// Pausar toda la música
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    /// <summary>
    /// Reanudar música pausada
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.Play();
    }

    // ==================== REPRODUCCIÓN DE SFX ====================

    /// <summary>
    /// Reproducir SFX simple 2D
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f; // 2D
        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Reproducir SFX 3D posicional
    /// </summary>
    public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f; // 3D
        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Reproducir SFX con grupo específico (Player, Enemy, UI)
    /// </summary>
    public void PlaySFXWithGroup(AudioClip clip, AudioMixerGroup group, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.outputAudioMixerGroup = group;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volume);

        // ✅ Resetear grupo después de reproducir
        StartCoroutine(ResetGroupAfterClip(source, sfxGroup, clip.length));
    }

    /// <summary>
    /// Reproducir SFX UI (con volumen separado)
    /// </summary>
    public void PlayUISound(AudioClip clip, float volume = 1f)
    {
        PlaySFXWithGroup(clip, uiGroup, volume);
    }

    /// <summary>
    /// Reproducir SFX de Player (con volumen separado)
    /// </summary>
    public void PlayPlayerSound(AudioClip clip, float volume = 1f)
    {
        PlaySFXWithGroup(clip, playerSFXGroup, volume);
    }

    /// <summary>
    /// Reproducir SFX de Enemy (con volumen separado)
    /// </summary>
    public void PlayEnemySound(AudioClip clip, float volume = 1f)
    {
        PlaySFXWithGroup(clip, enemySFXGroup, volume);
    }

    /// <summary>
    /// Reproducir SFX de Ambient (con volumen separado)
    /// </summary>
    public void PlayAmbientSound(AudioClip clip, float volume = 1f)
    {
        PlaySFXWithGroup(clip, ambientGroup, volume);
    }

    private AudioSource GetNextAvailableSFXSource()
    {
        AudioSource source = sfxPool[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % sfxPoolSize;
        return source;
    }

    private IEnumerator ResetGroupAfterClip(AudioSource source, AudioMixerGroup defaultGroup, float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f);
        source.outputAudioMixerGroup = defaultGroup;
    }

    // ==================== CONTROL DE VOLUMEN ====================

    /// <summary>
    /// Cambiar volumen general (Master)
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("MasterVolume", dB);
        Debug.Log($"🔊 Master Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de música
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("MusicVolume", dB);
        Debug.Log($"🎵 Music Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de SFX general
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("SFXVolume", dB);
        Debug.Log($"🔊 SFX Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de UI
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetUIVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("UIVolume", dB);
        Debug.Log($"🔊 UI Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de Player SFX
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetPlayerSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("PlayerSFXVolume", dB);
        Debug.Log($"🔊 Player SFX Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de Enemy SFX
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetEnemySFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("EnemySFXVolume", dB);
        Debug.Log($"🔊 Enemy SFX Volume: {volume:P0} ({dB:F2} dB)");
    }

    /// <summary>
    /// Cambiar volumen de Ambient
    /// ⚠️ Llamado desde SettingsManager
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(volume);
        audioMixer.SetFloat("AmbientVolume", dB);
        Debug.Log($"🔊 Ambient Volume: {volume:P0} ({dB:F2} dB)");
    }

    // ==================== PAUSA GENERAL ====================

    /// <summary>
    /// Pausar todo el audio
    /// </summary>
    public void PauseAllAudio()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();

        foreach (var source in sfxPool)
        {
            if (source.isPlaying)
                source.Pause();
        }

        Debug.Log("⏸️ Todo el audio pausado");
    }

    /// <summary>
    /// Reanudar todo el audio
    /// </summary>
    public void ResumeAllAudio()
    {
        if (musicSource.clip != null)
            musicSource.Play();

        foreach (var source in sfxPool)
        {
            source.Play();
        }

        Debug.Log("▶️ Todo el audio reanudado");
    }

    // ==================== HELPERS ====================

    /// <summary>
    /// Convertir volumen lineal (0-1) a decibeles
    /// Fórmula: dB = 20 * log10(volume)
    /// </summary>
    private float VolumeToDecibels(float volume)
    {
        if (volume == 0f) return -80f; // Silencio
        return Mathf.Clamp(20f * Mathf.Log10(volume), -80f, 0f);
    }

    /// <summary>
    /// Convertir decibeles a volumen lineal
    /// Fórmula inversa: volume = 10^(dB/20)
    /// </summary>
    private float DecibelsToVolume(float dB)
    {
        if (dB <= -80f) return 0f;
        return Mathf.Pow(10f, dB / 20f);
    }

    /// <summary>
    /// Obtener volumen actual desde AudioMixer
    /// </summary>
    public float GetMasterVolume()
    {
        audioMixer.GetFloat("MasterVolume", out float dB);
        return DecibelsToVolume(dB);
    }

    public float GetMusicVolume()
    {
        audioMixer.GetFloat("MusicVolume", out float dB);
        return DecibelsToVolume(dB);
    }

    public float GetSFXVolume()
    {
        audioMixer.GetFloat("SFXVolume", out float dB);
        return DecibelsToVolume(dB);
    }

    public float GetUIVolume()
    {
        audioMixer.GetFloat("UIVolume", out float dB);
        return DecibelsToVolume(dB);
    }
}