using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// 🎵 Audio_Manager - Gestor centralizado de audio
///
/// ⚠️ UBICACIÓN: Escena de MENU (como DontDestroyOnLoad)
///
/// Canales de volumen: Master, Music, SFX, Ambient
/// ✅ Reproducción de música con fade transitions
/// ✅ Pool de AudioSources para efectos simultáneos
/// ✅ Sonido 3D posicional
/// ✅ Persistencia entre escenas
/// </summary>
public class Audio_Manager : MonoBehaviour
{
    public static Audio_Manager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private int sfxPoolSize = 16;

    private AudioSource[] sfxPool;
    private int currentPoolIndex = 0;
    private Coroutine musicRoutine;

    // ==================== INICIALIZACIÓN ====================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
        }

        InitializeSFXPool();
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
    }

    // ==================== MÚSICA ====================

    public void PlayMusic(AudioClip musicClip, float fadeDuration = 1f)
    {
        if (musicClip == null) return;

        // Si llega otra canción durante un fade, se cancela el fade anterior
        // (antes dos corrutinas se pisaban el volumen).
        if (musicRoutine != null) StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(FadeMusicTransition(musicClip, fadeDuration));
    }

    private IEnumerator FadeMusicTransition(AudioClip newClip, float fadeDuration)
    {
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(musicSource, musicSource.volume, 0f, fadeDuration));
            musicSource.Stop();
        }

        musicSource.clip = newClip;
        musicSource.time = 0f;
        musicSource.Play();

        yield return StartCoroutine(FadeAudioSource(musicSource, 0f, 1f, fadeDuration));

        musicRoutine = null;
    }

    private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration)
    {
        if (duration <= 0f)
        {
            source.volume = endVolume;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            yield return null;
        }

        source.volume = endVolume;
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying) musicSource.Pause();
    }

    public void ResumeMusic()
    {
        // UnPause continúa desde donde se pausó; Play() empezaba la canción de cero.
        if (musicSource.clip != null) musicSource.UnPause();
    }

    // ==================== SFX ====================

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.outputAudioMixerGroup = sfxGroup;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volume);
    }

    public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.transform.position = position;
        source.outputAudioMixerGroup = sfxGroup;
        source.spatialBlend = 1f;
        source.PlayOneShot(clip, volume);
    }

    public void PlayAmbientSound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAvailableSFXSource();
        source.outputAudioMixerGroup = ambientGroup;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volume);
        StartCoroutine(ResetGroupAfterClip(source, sfxGroup, clip.length));
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

    // ==================== PAUSA ====================

    public void PauseAllAudio()
    {
        if (musicSource.isPlaying) musicSource.Pause();

        foreach (var source in sfxPool)
        {
            if (source.isPlaying) source.Pause();
        }
    }

    public void ResumeAllAudio()
    {
        // UnPause solo reanuda lo que estaba pausado.
        // Antes se usaba Play(), que reiniciaba la música y reproducía las fuentes vacías.
        musicSource.UnPause();

        foreach (var source in sfxPool) source.UnPause();
    }

    // ==================== CONTROL DE VOLUMEN ====================

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", VolumeToDecibels(Mathf.Clamp01(volume)));
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", VolumeToDecibels(Mathf.Clamp01(volume)));
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", VolumeToDecibels(Mathf.Clamp01(volume)));
    }

    public void SetAmbientVolume(float volume)
    {
        audioMixer.SetFloat("AmbientVolume", VolumeToDecibels(Mathf.Clamp01(volume)));
    }

    // ==================== GETTERS ====================

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

    public float GetAmbientVolume()
    {
        audioMixer.GetFloat("AmbientVolume", out float dB);
        return DecibelsToVolume(dB);
    }

    // ==================== HELPERS ====================

    private float VolumeToDecibels(float volume)
    {
        if (volume == 0f) return -80f;
        return Mathf.Clamp(20f * Mathf.Log10(volume), -80f, 0f);
    }

    private float DecibelsToVolume(float dB)
    {
        if (dB <= -80f) return 0f;
        return Mathf.Pow(10f, dB / 20f);
    }
}
