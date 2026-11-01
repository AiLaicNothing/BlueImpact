using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Un sonido con variaciones. Si pones varios clips, elige uno al azar en cada disparo,
// asi el mismo golpe no suena identico diez veces seguidas.
[Serializable]
public class BossSound
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitchMin = 0.95f;
    [Range(0.1f, 3f)] public float pitchMax = 1.05f;

    public bool HasClip
    {
        get
        {
            if (clips == null) return false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return true;
            }

            return false;
        }
    }

    public AudioClip Pick()
    {
        if (clips == null || clips.Length == 0) return null;

        // Se intenta varias veces por si hay huecos vacios en la lista
        for (int attempt = 0; attempt < 4; attempt++)
        {
            AudioClip c = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (c != null) return c;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null) return clips[i];
        }

        return null;
    }

    public float PickPitch()
    {
        float lo = Mathf.Min(pitchMin, pitchMax);
        float hi = Mathf.Max(pitchMin, pitchMax);

        return UnityEngine.Random.Range(lo, hi);
    }
}

// Todo el audio de Medusa Morgana. Boss_MedusaMorgana llama a estos metodos en los
// momentos que ya marca su codigo, asi no hay que tocar ningun clip de animacion.
public class BossAudio : MonoBehaviour
{
    // Canales de bucle. Cada uno tiene su propio AudioSource, asi dos bucles distintos
    // pueden sonar a la vez sin cortarse (por ejemplo la marca y los laseres de Disonancia).
    public enum Loop
    {
        Cast = 0,    // carga de un casteo, y tambien el tambaleo al romperse
        Laser = 1,   // laseres de Resonancia y Disonancia
        Arena = 2,   // borde de la arena en Climax
        Mark = 3,    // marca de Disonancia sobre el jugador
        Steps = 4    // pasos mientras camina al centro
    }

    private const int LoopChannelCount = 5;

    [Header("Fuentes (opcional: si las dejas vacias se crean solas)")]
    [SerializeField] private AudioSource sfxSource;     // golpes, impactos, disparos
    [SerializeField] private AudioSource voiceSource;   // gritos y voz, para que no corten un golpe
    [SerializeField] private AudioSource musicSource;   // musica del combate

    [Header("Espacializacion")]
    [Tooltip("1 = sonido 3D que baja con la distancia. 0 = 2D, se oye igual en toda la sala.")]
    [Range(0f, 1f)][SerializeField] private float sfxSpatialBlend = 1f;
    [Range(0f, 1f)][SerializeField] private float voiceSpatialBlend = 0.6f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 45f;

    [Header("Volumen general")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float voiceVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float loopVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.6f;

    [Header("Fades de los bucles")]
    [SerializeField] private float loopFadeIn = 0.12f;
    [SerializeField] private float loopFadeOut = 0.18f;
    [SerializeField] private float musicFade = 1.5f;

    // ---------- INTRO Y CAMBIO DE FASE ----------

    [Header("Intro")]
    [SerializeField] private BossSound introScream;
    [SerializeField] private BossSound landImpact;

    [Header("Cambio de fase")]
    [SerializeField] private BossSound phaseScream;        // grito antes de transformarse
    [SerializeField] private BossSound phaseTransform;     // el momento del cambio de modelo
    [SerializeField] private BossSound phaseTwoEntrance;   // rugido de entrada de la fase 2

    // ---------- MOVIMIENTO ----------

    [Header("Movimiento")]
    [SerializeField] private BossSound stepsLoop;

    // ---------- CASTEO Y VENTANA DE CASTIGO ----------

    [Header("Casteo")]
    [SerializeField] private BossSound incantationVoice;   // voz al empezar a castear
    [SerializeField] private BossSound castChargeLoop;     // zumbido que crece durante el casteo
    [SerializeField] private BossSound vulnerableOpen;     // aviso de que se puede interrumpir

    [Header("Casteo roto")]
    [SerializeField] private BossSound castBroken;         // el crack del momento en que se rompe
    [SerializeField] private BossSound brokenLoop;         // mientras esta tambaleandose
    [SerializeField] private BossSound castRecover;        // se recupera y vuelve a atacar

    // ---------- LASERES ----------

    [Header("Laseres")]
    [SerializeField] private BossSound laserWarnLoop;      // zumbido fino mientras apunta
    [SerializeField] private BossSound laserFireStart;     // golpe seco al empezar a disparar
    [SerializeField] private BossSound laserFireLoop;      // bucle mientras hacen dano
    [SerializeField] private BossSound laserTickHit;       // cada vez que queman al jugador

    // ---------- ESPADA Y COLA ----------

    [Header("Espada y cola")]
    [SerializeField] private BossSound swordSwing;
    [SerializeField] private BossSound tailSwing;
    [SerializeField] private BossSound swordWave;          // lanzamiento de la onda de corte
    [SerializeField] private BossSound tailWave;
    [SerializeField] private BossSound meleeHit;           // conecta el barrido

    // ---------- NOVA ----------

    [Header("Nova")]
    [SerializeField] private BossSound novaChargeLoop;     // los 5 segundos de canalizacion
    [SerializeField] private BossSound novaBlast;          // detona
    [SerializeField] private BossSound novaHit;            // alcanza al jugador

    // ---------- CLIMAX ----------

    [Header("Climax Serpentinus")]
    [SerializeField] private BossSound climaxScream;
    [SerializeField] private BossSound arenaBorderStart;   // aparece el borde
    [SerializeField] private BossSound arenaBorderLoop;    // zumbido del borde mientras dura
    [SerializeField] private BossSound borderTick;         // el borde quema al jugador
    [SerializeField] private BossSound waveSummon;         // aparecen las oleadas

    // ---------- DISONANCIA ----------

    [Header("Disonancia")]
    [SerializeField] private BossSound markPlaced;         // cae la marca sobre el jugador
    [SerializeField] private BossSound markLoop;           // mientras la marca sigue encima
    [SerializeField] private BossSound beamWarning;        // aviso antes del haz
    [SerializeField] private BossSound beamImpact;         // cae el haz

    // ---------- DANO Y MUERTE ----------

    [Header("Dano y muerte")]
    [SerializeField] private BossSound hurtVoice;          // queja al recibir dano
    [SerializeField] private BossSound staggerStart;
    [SerializeField] private BossSound staggerEnd;
    [SerializeField] private BossSound death;

    // ---------- MUSICA ----------

    [Header("Musica (opcional)")]
    [SerializeField] private AudioClip phaseOneMusic;
    [SerializeField] private AudioClip phaseTwoMusic;

    private readonly AudioSource[] loopSources = new AudioSource[LoopChannelCount];
    private readonly Coroutine[] loopFades = new Coroutine[LoopChannelCount];

    // Que sonido esta sonando en cada canal. Evita reiniciar el bucle cuando el boss
    // vuelve a pedir el mismo sonido (SetSteps se llama en cada frame).
    private readonly BossSound[] loopPlaying = new BossSound[LoopChannelCount];

    private Coroutine musicRoutine;

    private void Awake()
    {
        sfxSource = EnsureSource(sfxSource, "SFX", sfxSpatialBlend, false);
        voiceSource = EnsureSource(voiceSource, "Voice", voiceSpatialBlend, false);
        musicSource = EnsureSource(musicSource, "Music", 0f, true);

        for (int i = 0; i < LoopChannelCount; i++)
        {
            loopSources[i] = EnsureSource(null, "Loop_" + ((Loop)i), sfxSpatialBlend, true);
            loopSources[i].volume = 0f;
        }
    }

    private void OnDisable()
    {
        // Si el boss se desactiva o lo destruyen, nada debe quedar sonando
        StopAllLoopsImmediate();
    }

    private AudioSource EnsureSource(AudioSource existing, string label, float spatial, bool loop)
    {
        AudioSource src = existing;

        if (src == null)
        {
            GameObject go = new GameObject("Audio_" + label);
            go.transform.SetParent(transform, false);

            src = go.AddComponent<AudioSource>();
        }

        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = spatial;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;

        return src;
    }

    // ---------- REPRODUCCION BASE ----------

    // Nota: el pitch se aplica al AudioSource, asi que afecta a los one-shot que ya estan
    // sonando en esa fuente. Con sonidos cortos no se nota.
    private void PlayOne(AudioSource src, BossSound sound, float channelVolume)
    {
        if (src == null || sound == null || !sound.HasClip) return;

        AudioClip clip = sound.Pick();
        if (clip == null) return;

        src.pitch = sound.PickPitch();
        src.PlayOneShot(clip, sound.volume * channelVolume);
    }

    private void PlaySfx(BossSound sound)
    {
        PlayOne(sfxSource, sound, sfxVolume);
    }

    private void PlayVoice(BossSound sound)
    {
        PlayOne(voiceSource, sound, voiceVolume);
    }

    // Reproduce un sonido puntual en una posicion concreta del mundo (impactos lejos del boss)
    private void PlaySfxAt(BossSound sound, Vector3 position)
    {
        if (sound == null || !sound.HasClip) return;

        AudioClip clip = sound.Pick();
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, sound.volume * sfxVolume);
    }

    // ---------- BUCLES ----------

    public void StartLoop(Loop channel, BossSound sound)
    {
        int i = (int)channel;
        if (sound == null || !sound.HasClip) return;

        AudioSource src = loopSources[i];
        if (src == null) return;

        // Si ya esta sonando este mismo sonido, solo se asegura el volumen y no reinicia.
        // Se compara el BossSound, no el clip, porque puede tener varias variaciones.
        if (src.isPlaying && loopPlaying[i] == sound)
        {
            FadeLoop(i, sound.volume * loopVolume, loopFadeIn);
            return;
        }

        AudioClip clip = sound.Pick();
        if (clip == null) return;

        loopPlaying[i] = sound;
        src.clip = clip;
        src.pitch = sound.PickPitch();
        src.volume = 0f;
        src.Play();

        FadeLoop(i, sound.volume * loopVolume, loopFadeIn);
    }

    public void StopLoop(Loop channel)
    {
        int i = (int)channel;

        AudioSource src = loopSources[i];
        if (src == null || !src.isPlaying) return;

        loopPlaying[i] = null;
        FadeLoop(i, 0f, loopFadeOut, true);
    }

    public void StopAllLoops()
    {
        for (int i = 0; i < LoopChannelCount; i++)
        {
            StopLoop((Loop)i);
        }
    }

    // Corta de golpe, sin fade. Para muerte o desactivacion.
    public void StopAllLoopsImmediate()
    {
        for (int i = 0; i < LoopChannelCount; i++)
        {
            if (loopFades[i] != null)
            {
                StopCoroutine(loopFades[i]);
                loopFades[i] = null;
            }

            loopPlaying[i] = null;

            if (loopSources[i] != null)
            {
                loopSources[i].Stop();
                loopSources[i].volume = 0f;
            }
        }
    }

    private void FadeLoop(int index, float target, float duration, bool stopAtEnd = false)
    {
        if (loopFades[index] != null)
        {
            StopCoroutine(loopFades[index]);
        }

        loopFades[index] = StartCoroutine(FadeLoopRoutine(index, target, duration, stopAtEnd));
    }

    private IEnumerator FadeLoopRoutine(int index, float target, float duration, bool stopAtEnd)
    {
        AudioSource src = loopSources[index];
        if (src == null) yield break;

        float start = src.volume;
        float t = 0f;

        // Tiempo real: el audio no debe congelarse si el juego se pausa en una cinematica
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, target, duration > 0f ? t / duration : 1f);
            yield return null;
        }

        src.volume = target;

        if (stopAtEnd)
        {
            src.Stop();
            src.clip = null;
        }

        loopFades[index] = null;
    }

    // ---------- API QUE USA EL BOSS ----------

    // Intro
    public void PlayIntroScream() { PlayVoice(introScream); }
    public void PlayLandImpact() { PlaySfx(landImpact); }

    // Cambio de fase
    public void PlayPhaseScream() { PlayVoice(phaseScream); }
    public void PlayPhaseTransform() { PlaySfx(phaseTransform); }
    public void PlayPhaseTwoEntrance() { PlayVoice(phaseTwoEntrance); }

    // Movimiento
    public void SetSteps(bool moving)
    {
        if (moving) StartLoop(Loop.Steps, stepsLoop);
        else StopLoop(Loop.Steps);
    }

    // Casteo
    public void PlayIncantation() { PlayVoice(incantationVoice); }

    public void StartCastCharge()
    {
        PlaySfx(vulnerableOpen);
        StartLoop(Loop.Cast, castChargeLoop);
    }

    public void StopCastCharge() { StopLoop(Loop.Cast); }

    // Casteo roto
    public void PlayCastBroken()
    {
        PlaySfx(castBroken);
        PlayVoice(hurtVoice);

        // La carga muere y entra el tambaleo, los dos en el mismo canal
        StartLoop(Loop.Cast, brokenLoop);
    }

    public void StopCastBroken()
    {
        StopLoop(Loop.Cast);
        PlaySfx(castRecover);
    }

    // Laseres
    public void StartLaserWarn() { StartLoop(Loop.Laser, laserWarnLoop); }

    public void StartLaserFire()
    {
        PlaySfx(laserFireStart);
        StartLoop(Loop.Laser, laserFireLoop);
    }

    public void StopLaser() { StopLoop(Loop.Laser); }
    public void PlayLaserTickHit(Vector3 at) { PlaySfxAt(laserTickHit, at); }

    // Espada y cola
    public void PlaySwing(bool isSword) { PlaySfx(isSword ? swordSwing : tailSwing); }
    public void PlayWaveLaunch(bool isSword) { PlaySfx(isSword ? swordWave : tailWave); }
    public void PlayMeleeHit(Vector3 at) { PlaySfxAt(meleeHit, at); }

    // Nova
    public void StartNovaCharge() { StartLoop(Loop.Cast, novaChargeLoop); }
    public void PlayNovaBlast() { StopLoop(Loop.Cast); PlaySfx(novaBlast); }
    public void PlayNovaHit(Vector3 at) { PlaySfxAt(novaHit, at); }

    // Climax
    public void PlayClimaxScream() { PlayVoice(climaxScream); }

    public void StartArenaBorder()
    {
        PlaySfx(arenaBorderStart);
        StartLoop(Loop.Arena, arenaBorderLoop);
    }

    public void StopArenaBorder() { StopLoop(Loop.Arena); }
    public void PlayBorderTick(Vector3 at) { PlaySfxAt(borderTick, at); }
    public void PlayWaveSummon() { PlaySfx(waveSummon); }

    // Disonancia
    public void PlayMarkPlaced(Vector3 at)
    {
        PlaySfxAt(markPlaced, at);
        StartLoop(Loop.Mark, markLoop);
    }

    public void StopMark() { StopLoop(Loop.Mark); }
    public void PlayBeamWarning(Vector3 at) { PlaySfxAt(beamWarning, at); }
    public void PlayBeamImpact(Vector3 at) { PlaySfxAt(beamImpact, at); }

    // Dano, stagger y muerte
    public void PlayHurt() { PlayVoice(hurtVoice); }
    public void PlayStaggerStart() { PlaySfx(staggerStart); PlayVoice(hurtVoice); }
    public void PlayStaggerEnd() { PlaySfx(staggerEnd); }

    public void PlayDeath()
    {
        StopAllLoopsImmediate();
        PlayVoice(death);
        StopMusic();
    }

    // ---------- MUSICA ----------

    public void StartPhaseOneMusic() { PlayMusic(phaseOneMusic); }
    public void StartPhaseTwoMusic() { PlayMusic(phaseTwoMusic); }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(MusicRoutine(clip));
    }

    private IEnumerator MusicRoutine(AudioClip clip)
    {
        // Baja lo que esta sonando
        if (musicSource.isPlaying)
        {
            yield return FadeMusic(0f);
            musicSource.Stop();
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        yield return FadeMusic(musicVolume);

        musicRoutine = null;
    }

    public void StopMusic()
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(StopMusicRoutine());
    }

    private IEnumerator StopMusicRoutine()
    {
        yield return FadeMusic(0f);

        musicSource.Stop();
        musicRoutine = null;
    }

    private IEnumerator FadeMusic(float target)
    {
        float start = musicSource.volume;
        float t = 0f;

        while (t < musicFade)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, target, musicFade > 0f ? t / musicFade : 1f);
            yield return null;
        }

        musicSource.volume = target;
    }

    // ---------- DIAGNOSTICO ----------

    // Lista en consola que sonidos quedaron sin clip, para no descubrirlo peleando
    [ContextMenu("Revisar sonidos sin asignar")]
    public void ReportMissingSounds()
    {
        List<string> missing = new List<string>();

        Check(introScream, "introScream", missing);
        Check(landImpact, "landImpact", missing);
        Check(phaseScream, "phaseScream", missing);
        Check(phaseTransform, "phaseTransform", missing);
        Check(phaseTwoEntrance, "phaseTwoEntrance", missing);
        Check(stepsLoop, "stepsLoop", missing);
        Check(incantationVoice, "incantationVoice", missing);
        Check(castChargeLoop, "castChargeLoop", missing);
        Check(vulnerableOpen, "vulnerableOpen", missing);
        Check(castBroken, "castBroken", missing);
        Check(brokenLoop, "brokenLoop", missing);
        Check(castRecover, "castRecover", missing);
        Check(laserWarnLoop, "laserWarnLoop", missing);
        Check(laserFireStart, "laserFireStart", missing);
        Check(laserFireLoop, "laserFireLoop", missing);
        Check(laserTickHit, "laserTickHit", missing);
        Check(swordSwing, "swordSwing", missing);
        Check(tailSwing, "tailSwing", missing);
        Check(swordWave, "swordWave", missing);
        Check(tailWave, "tailWave", missing);
        Check(meleeHit, "meleeHit", missing);
        Check(novaChargeLoop, "novaChargeLoop", missing);
        Check(novaBlast, "novaBlast", missing);
        Check(novaHit, "novaHit", missing);
        Check(climaxScream, "climaxScream", missing);
        Check(arenaBorderStart, "arenaBorderStart", missing);
        Check(arenaBorderLoop, "arenaBorderLoop", missing);
        Check(borderTick, "borderTick", missing);
        Check(waveSummon, "waveSummon", missing);
        Check(markPlaced, "markPlaced", missing);
        Check(markLoop, "markLoop", missing);
        Check(beamWarning, "beamWarning", missing);
        Check(beamImpact, "beamImpact", missing);
        Check(hurtVoice, "hurtVoice", missing);
        Check(staggerStart, "staggerStart", missing);
        Check(staggerEnd, "staggerEnd", missing);
        Check(death, "death", missing);

        if (phaseOneMusic == null) missing.Add("phaseOneMusic");
        if (phaseTwoMusic == null) missing.Add("phaseTwoMusic");

        if (missing.Count == 0)
        {
            Debug.Log("[BossAudio] Todos los sonidos tienen clip");
            return;
        }

        Debug.LogWarning("[BossAudio] Sin clip (" + missing.Count + "): " + string.Join(", ", missing));
    }

    private static void Check(BossSound sound, string name, List<string> missing)
    {
        if (sound == null || !sound.HasClip) missing.Add(name);
    }
}