using UnityEngine;

public class Audio_Manager : MonoBehaviour
{
    public static Audio_Manager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource audioSource;
    private void Awake()
    {
        Instance = this;
    }

    public void PlayAudio(AudioClip audio,float volume = 1)
    {
        if (audio == null) return;

        Instance.audioSource.PlayOneShot(audio,volume);
    }

    public void ChangeMusic(AudioClip music, float volume = 1)
    {
        if (music == null) return;

        Instance.musicSource.clip = music;
        Instance.musicSource.volume = volume;
        Instance.musicSource.loop = true;
        Instance.musicSource.Play();
    }
}
