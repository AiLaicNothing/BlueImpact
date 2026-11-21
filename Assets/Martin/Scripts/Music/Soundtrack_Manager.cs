using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Soundtrack_Manager : MonoBehaviour
{
    public enum MusicTransition
    {
        CrossFade,
        FadeOutThenIn,
        Cut
    }

    public static Soundtrack_Manager Instance { get; private set; }

    public static bool IsAvailable => Instance != null && !isQuitting;

    static bool isQuitting = false;

    [Header("Output")]
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool ignoreListenerPause;

    [Header("Default fade times")]
    [SerializeField] private float defaultFadeIn = 2.5f;
    [SerializeField] private float defaultFadeOut = 2.5f;

    [Header("Transitions")]
    [SerializeField] private MusicTransition zoneTransition = MusicTransition.CrossFade;
    [SerializeField] private MusicTransition bossStartTransition = MusicTransition.CrossFade;
    [SerializeField] private MusicTransition bossEndTransition = MusicTransition.FadeOutThenIn;
    [SerializeField, Min(0f)] float silenceGap = 0.25f;
    [SerializeField, Min(0f)] float bossEndSilenceGap = 1.5f;

    [Header("Optional")]
    [SerializeField] private Soundtrack startingTrack;

    // One song that is playing, fading in, fading out or waiting to start.
    class Voice
    {
        public AudioSource source;
        public Soundtrack track;
        public float fade;          // 0..1 how far the fade is (0 = silent, 1 = full volume)
        public float target;        // 0 = fading out, 1 = fading in / playing
        public float fadeDuration;  // seconds for a full fade (0 = instant)
        public float delay;         // seconds to wait before starting to play
        public int startSamples;    // where to start the song (to continue where it stopped)
        public bool started;
    }

    struct SavedPosition
    {
        public int samples;
        public float time;
    }

    List<Voice> voices = new List<Voice>();
    List<AudioSource> freeSources = new List<AudioSource>();
    Dictionary<Soundtrack, SavedPosition> savedPositions = new Dictionary<Soundtrack, SavedPosition>();

    List<MusicZone> activeZones = new List<MusicZone>();

    Soundtrack zoneTrack;
    Soundtrack bossTrack;
    Soundtrack currentTrack;

    public Soundtrack CurrentTrack { get { return currentTrack; } }
    public Soundtrack ZoneTrack { get { return zoneTrack; } }
    public bool IsBossMusicActive { get { return bossTrack != null; } }

    public float MusicVolume
    {
        get { return musicVolume; }
        set { musicVolume = Mathf.Clamp01(value); }
    }

    void Awake()
    {
        // Only one manager can exist.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        isQuitting = false;

        // DontDestroyOnLoad only works on objects that are not children of anything.
        if (transform.parent != null) transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (startingTrack != null && currentTrack == null && zoneTrack == null)
            SetZoneMusic(startingTrack);
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------ Public API

    // Sets the music of the current zone. During a boss fight it's only remembered, not played.
    public void SetZoneMusic(Soundtrack track)
    {
        zoneTrack = track;

        if (bossTrack == null)TransitionTo(track, zoneTransition, silenceGap);
    }

    // Switches to boss music, overriding zone music.
    public void StartBossMusic(Soundtrack track)
    {
        if (track == null) return;
        bossTrack = track;
        TransitionTo(track, bossStartTransition, silenceGap);
    }

    // Ends the boss music and returns to the current zone's music.
    public void EndBossMusic()
    {
        if (bossTrack == null) return;
        bossTrack = null;
        TransitionTo(zoneTrack, bossEndTransition, bossEndSilenceGap);
    }

    // Fades out all music. Going through a MusicTrigger will start music again.
    // fadeOutTime < 0 = use each track's own fade-out time.
    public void StopMusic(float fadeOutTime = -1f)
    {
        currentTrack = null;
        foreach (Voice v in voices)
        {
            float time = fadeOutTime >= 0f ? fadeOutTime : GetFadeOutTime(v.track);
            FadeOut(v, time);
        }
    }

    // ------------------------------------------------------------------ Internals

    void TransitionTo(Soundtrack track, MusicTransition style, float gap)
    {
        // Same music requested again (e.g. two zones share a song): do nothing, no restart.
        if (track == currentTrack) return;
        currentTrack = track;

        bool cut = style == MusicTransition.Cut;

        // 1) Fade out every song that isn't the new one, and measure how long that will take.
        float longestFadeOut = 0f;
        foreach (Voice v in voices)
        {
            if (v.track == track) continue;

            FadeOut(v, cut ? 0f : GetFadeOutTime(v.track));

            // Time left = how loud it still is * how long a full fade takes.
            if (v.started) longestFadeOut = Mathf.Max(longestFadeOut, v.fade * v.fadeDuration);
        }

        if (track == null || track.clip == null) return; // a "silent" zone

        // 2) If this song is still playing (e.g. it was fading out because the player
        //    stepped back over a border), just fade it back in: no restart, no jump.
        Voice voice = FindVoice(track);
        if (voice == null)
        {
            voice = CreateVoice(track);
            if (style == MusicTransition.FadeOutThenIn && longestFadeOut > 0f)
                voice.delay = longestFadeOut + gap;
        }

        FadeIn(voice, cut ? 0f : GetFadeInTime(track));
    }

    void Update()
    {
        // unscaledDeltaTime = fades keep working when the game is paused with Time.timeScale = 0
        float dt = Time.unscaledDeltaTime;

        // Loop backwards because we may remove voices from the list.
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            Voice v = voices[i];

            if (!v.started)
            {
                // Cancelled before it even started.
                if (v.target <= 0f)
                {
                    RemoveVoice(i);
                    continue;
                }

                // Still waiting (FadeOutThenIn).
                if (v.delay > 0f)
                {
                    v.delay -= dt;
                    continue;
                }

                StartVoice(v);
            }
            else if (!v.source.loop && !v.source.isPlaying && !AudioListener.pause)
            {
                // A non-looping song finished on its own.
                savedPositions.Remove(v.track);
                if (currentTrack == v.track) currentTrack = null;
                RemoveVoice(i);
                continue;
            }

            // Move the fade towards its target.
            if (v.fadeDuration <= 0f) v.fade = v.target;  // instant
            else v.fade = Mathf.MoveTowards(v.fade, v.target, dt / v.fadeDuration);

            v.source.volume = FadeCurve(v.fade) * v.track.volume * musicVolume;

            // Fully faded out: remember where it was and free the AudioSource.
            if (v.target <= 0f && v.fade <= 0f)
            {
                SavePosition(v);
                RemoveVoice(i);
            }
        }
    }

    // Turns the straight 0..1 fade into a curve.
    // With a straight line, a crossfade sounds quieter in the middle.
    // With this curve (sin² + cos² = 1) the total loudness stays the same during the whole crossfade.
    float FadeCurve(float fade)
    {
        return Mathf.Sin(fade * Mathf.PI * 0.5f);
    }

    Voice FindVoice(Soundtrack track)
    {
        foreach (Voice v in voices)
        {
            if (v.track == track) return v;
        }
        return null;
    }

    Voice CreateVoice(Soundtrack track)
    {
        AudioSource source = GetFreeSource();
        source.clip = track.clip;
        source.loop = track.loop;
        source.volume = 0f;

        // Start loading the song now so it's ready when it has to play.
        if (track.clip.loadState == AudioDataLoadState.Unloaded)
            track.clip.LoadAudioData();

        Voice voice = new Voice();
        voice.source = source;
        voice.track = track;

        // Continue where it stopped last time (if allowed and not too long ago).
        if (track.resumeFromLastPosition && savedPositions.ContainsKey(track))
        {
            SavedPosition saved = savedPositions[track];
            float secondsSinceStopped = Time.unscaledTime - saved.time;

            if (track.forgetPositionAfter <= 0f || secondsSinceStopped <= track.forgetPositionAfter)
                voice.startSamples = saved.samples;
        }

        voices.Add(voice);
        return voice;
    }

    AudioSource GetFreeSource()
    {
        // Reuse an AudioSource if there's a free one.
        if (freeSources.Count > 0)
        {
            AudioSource reused = freeSources[freeSources.Count - 1];
            freeSources.RemoveAt(freeSources.Count - 1);
            return reused;
        }

        // Otherwise create a new one as a child object.
        GameObject go = new GameObject("Music Voice");
        go.transform.SetParent(transform, false);

        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;        // 2D, same volume everywhere
        s.priority = 0;             // highest priority, never cut by Unity's voice limit
        s.bypassReverbZones = true; // music shouldn't echo in caves
        s.dopplerLevel = 0f;
        s.ignoreListenerPause = ignoreListenerPause;
        s.outputAudioMixerGroup = outputGroup;
        return s;
    }

    void StartVoice(Voice v)
    {
        v.source.volume = 0f;
        v.source.Play();

        if (v.startSamples > 0 && v.startSamples < v.track.clip.samples)
            v.source.timeSamples = v.startSamples;

        v.started = true;
    }

    void SavePosition(Voice v)
    {
        if (!v.started || !v.track.resumeFromLastPosition) return;

        SavedPosition saved = new SavedPosition();
        saved.samples = v.source.timeSamples;
        saved.time = Time.unscaledTime;
        savedPositions[v.track] = saved;
    }

    void RemoveVoice(int index)
    {
        Voice v = voices[index];
        v.source.Stop();
        v.source.clip = null;
        freeSources.Add(v.source);
        voices.RemoveAt(index);
    }

    void FadeIn(Voice v, float time)
    {
        v.target = 1f;
        v.fadeDuration = time;
    }

    void FadeOut(Voice v, float time)
    {
        v.target = 0f;
        v.fadeDuration = time;
    }

    float GetFadeInTime(Soundtrack track)
    {
        if (track != null && track.fadeInTime >= 0f) return track.fadeInTime;
        return defaultFadeIn;
    }

    float GetFadeOutTime(Soundtrack track)
    {
        if (track != null && track.fadeOutTime >= 0f) return track.fadeOutTime;
        return defaultFadeOut;
    }
}