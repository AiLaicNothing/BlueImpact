using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_MedusaMorgana : EnemyBase
{
    public enum MeteorAreaCenter
    {
        Player,   // la lluvia cae alrededor del jugador
        Boss      // la lluvia cae alrededor de Morgana
    }

    [Header("Info")]
    [SerializeField] private string bossDisplayName = "Medusa Morgana";

    [Header("Fases")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoThreshold = 0.6f;
    [SerializeField] private float firstAttackDelay = 1f;
    [SerializeField] private float pauseBetweenAttacks = 0.75f;

    [Header("Ventana de castigo (romper el casteo con dano)")]
    [SerializeField] private bool castBreakEnabled = true;
    [SerializeField] private float castBreakDamage = 60f;      // dano acumulado durante un casteo para romperlo (~10% de la vida)
    [SerializeField] private float castBreakStunDuration = 3f; // segundos que el boss queda abierto al romperse el casteo
    [SerializeField] private string castBreakTrigger = "";     // opcional: nombre del trigger de tambaleo en el Animator
    [SerializeField] private GameObject castBreakVfx;          // PREFAB opcional: destello al romper el casteo
    [SerializeField] private GameObject vulnerableVfx;         // HIJO del boss (no prefab): se enciende mientras el casteo se puede romper
    [SerializeField] private GameObject brokenVfx;             // HIJO del boss (no prefab): se enciende mientras esta tambaleandose

    [Header("Nombres de ataque (los ve el jugador en el HUD)")]
    [SerializeField] private string nameResonancia = "Resonancia";
    [SerializeField] private string namePartTitura = "Part-Titura";
    [SerializeField] private string nameClimax = "Climax Serpentinus";
    [SerializeField] private string nameMeteorBeam = "Meteor Beam";
    [SerializeField] private string nameMeteorShower = "Meteor Shower";

    [Header("Recuperacion despues de cada ataque (ventana para pegarle)")]
    [SerializeField] private float recoveryResonancia = 2.5f;
    [SerializeField] private float recoveryPartTitura = 2f;
    [SerializeField] private float recoveryClimax = 3f;
    [SerializeField] private float recoveryMeteorBeam = 2.5f;
    [SerializeField] private float recoveryMeteorShower = 2f;

    [Header("Animator (datos de los clips, 24 fps)")]
    [SerializeField] private float animFps = 24f;
    [SerializeField] private string idleStateName = "Iddle";   // nombre del estado de reposo en AMBOS controllers
    [SerializeField] private int screamFrames = 40;            // Scream de la fase 1 (intro y antes del cambio)
    [SerializeField] private int swordActiveStartFrame = 14;   // frame donde la espada empieza a barrer
    [SerializeField] private int swordActiveEndFrame = 24;     // frame donde termina el barrido
    [SerializeField] private int tailActiveStartFrame = 8;     // frame donde la cola empieza a barrer
    [SerializeField] private int tailActiveEndFrame = 18;      // frame donde termina el barrido

    [Header("Movimiento (solo para volver al centro y girar)")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float centerReachDistance = 0.3f;
    [SerializeField] private float moveToCenterTimeout = 6f;

    [Header("Puntas de arma (huesos del modelo FASE 1)")]
    [SerializeField] private Transform swordTip;
    [SerializeField] private Transform tailTip;

    [Header("Puntas de arma del modelo FASE 2")]
    // Los huesos de un modelo desactivado se quedan congelados donde estaban.
    // Si no pones estos, en fase 2 las ondas saldrian de la posicion vieja del modelo 1.
    [SerializeField] private Transform swordTipPhaseTwo;
    [SerializeField] private Transform tailTipPhaseTwo;
    [SerializeField] private Transform[] laserOriginsPhaseTwo;

    [Header("Laseres (Resonancia)")]
    [SerializeField] private Transform[] laserOrigins;         // juntas de las cabezas de serpiente
    [SerializeField] private float laserLength = 25f;
    [SerializeField] private float laserWidth = 0.15f;
    [SerializeField] private float laserWarnWidth = 0.03f;
    [SerializeField] private float laserRadius = 0.3f;         // grosor del laser para el dano
    [SerializeField] private float laserTickInterval = 0.5f;   // tiempo minimo entre dano de laser
    [SerializeField] private float laserDamagePerTick = 8f;
    [SerializeField] private Color laserColor = new Color(0.2f, 1f, 0.3f, 1f);
    [SerializeField] private Color laserWarnColor = new Color(0.2f, 1f, 0.3f, 0.35f);
    [SerializeField] private LayerMask laserObstacles;
    [SerializeField] private Material laserMaterial;           // opcional, si no usa Sprites/Default
    [SerializeField] private float laserStartSpread = 1f;      // separacion lateral (m) donde nace cada laser; todos llegan al pecho del jugador. 0 = nacen juntos
    [SerializeField] private float castTurnSpeed = 90f;        // grados/s con los que el boss gira hacia el jugador mientras lanza laseres

    [Header("Resonancia (Fase 1)")]
    [SerializeField] private float resonanciaWindUp = 2.5f;    // Z: gesto de espada, muestra el aviso
    [SerializeField] private float resonanciaDuration = 4f;    // tiempo que los laseres disparan
    [SerializeField] private int resonanciaLaserCount = 6;     // solo se usa si no hay juntas asignadas en laserOrigins
    [SerializeField] private float resonanciaTrackSpeed = 25f; // grados/s con los que siguen al jugador

    [Header("Part-Titura (Fase 1)")]
    [SerializeField] private float partTituraDuration = 12f;
    [SerializeField] private float partTituraHitInterval = 2f; // Y: segundos entre golpes
    [SerializeField] private float swordDamage = 12f;
    [SerializeField] private float tailDamage = 12f;
    [SerializeField] private float weaponThickness = 0.5f;     // grosor del arma para el barrido
    [SerializeField] private float bodyRadius = 0.4f;          // radio del cuerpo del jugador

    [Header("Climax Serpentinus (Fase 2) - Animacion 'Invocation' 80 frames")]
    [SerializeField] private int climaxFrames = 80;
    [SerializeField] private int climaxReleaseFrame = 40;      // frame donde desata la energia: hasta aqui se puede romper el casteo
    [SerializeField] private GameObject climaxCrystalGlow;     // HIJO del modelo fase 2 (cristal de la espada): brilla durante la invocacion
    [SerializeField] private float climaxCooldown = 40f;       // segundos minimos entre dos Climax (cuenta desde que empieza)

    [Header("Climax Serpentinus - Arena")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 15f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float borderWarningTime = 1.25f;  // X: segundos que el borde brilla antes de hacer dano
    [SerializeField] private float climaxDuration = 30f;       // tope maximo: termina antes si limpian las oleadas
    [SerializeField] private float borderTickInterval = 0.5f;
    [SerializeField] private float borderDamagePerSecond = 5f;
    [SerializeField] private GameObject arenaBorderVfx;        // PREFAB

    [Header("Climax Serpentinus - Oleadas (forma principal)")]
    // Lo pasa BossEvent con SetArena. Ese WaveManager debe tener startOnPlayerEnter en false,
    // si no, las oleadas se lanzarian solas al entrar el jugador a la sala.
    [SerializeField] private WaveManager climaxWaves;
    [SerializeField] private bool climaxEndsWhenWavesCleared = true;

    [Header("Climax Serpentinus - Invocacion simple (solo si no hay WaveManager)")]
    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private int golemsToSummon = 5;           // Y: enemigos que invoca
    [SerializeField] private Transform[] summonPoints;

    [Header("Meteor Beam (Fase 2) - Animacion 'MeteorBeam' 70 frames")]
    [SerializeField] private int meteorBeamFrames = 70;
    [SerializeField] private int meteorBeamLockFrame = 18;      // hasta este frame gira hacia el jugador; despues la trayectoria queda fija
    [SerializeField] private int meteorBeamFireStartFrame = 30; // frame donde sale el rayo (fin del casteo que se puede romper)
    [SerializeField] private int meteorBeamFireEndFrame = 58;   // frame donde se apaga el rayo
    [SerializeField] private Transform meteorBeamOrigin;        // HIJO del modelo fase 2 (boca o cristal). Vacio = frente del boss
    [SerializeField] private float meteorBeamOriginHeight = 1.6f; // solo si no hay meteorBeamOrigin
    [SerializeField] private float meteorBeamAimTurnSpeed = 180f; // grados/s mientras apunta
    [SerializeField] private float meteorBeamLength = 25f;
    [SerializeField] private float meteorBeamWidth = 2f;        // ancho de la franja que hace dano (y del aviso en el suelo)
    [SerializeField] private float meteorBeamDamagePerTick = 12f;
    [SerializeField] private float meteorBeamTickInterval = 0.25f;
    [SerializeField] private float meteorBeamVisualWidth = 1.2f;
    [SerializeField] private Color meteorBeamColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private GameObject meteorBeamVfx;          // PREFAB opcional: pivote al inicio, apunta en Z y mide 1 m de largo

    [Header("Meteor Shower (Fase 2) - Animacion 'MeteorShower' 70 frames")]
    [SerializeField] private int meteorShowerFrames = 70;
    [SerializeField] private int meteorShowerCastFrame = 30;    // frame donde invoca la lluvia (fin del casteo que se puede romper)
    [SerializeField] private int meteorCount = 12;
    [SerializeField] private float meteorSpawnDuration = 3f;    // tiempo en el que se reparten las caidas
    [SerializeField] private MeteorAreaCenter meteorAreaCenter = MeteorAreaCenter.Player;
    [SerializeField] private float meteorAreaRadius = 7f;
    [Range(0f, 1f)]
    [SerializeField] private float meteorAimAtPlayerChance = 0.35f; // probabilidad de que un meteorito vaya justo donde esta el jugador
    [SerializeField] private MeteorConfig meteor = new MeteorConfig();

    [Header("Cambio de fase (2 modelos)")]
    [SerializeField] private GameObject phaseOneModel;
    [SerializeField] private GameObject phaseTwoModel;
    [SerializeField] private GameObject transformVfx;          // PREFAB
    [SerializeField] private float swapDelay = 0.2f;
    [SerializeField] private bool screamBeforeSwap = true;     // grito corto de la fase 1 antes de cambiar de modelo
    [SerializeField] private string phaseTwoIntroTrigger = "Scream";
    [SerializeField] private int phaseTwoScreamFrames = 70;    // Scream del modelo fase 2

    [Header("Telegrafia y trail")]
    [SerializeField] private BossAttackTelegraph telegraph;

    [Header("Audio")]
    // Si lo dejas vacio se busca en el propio boss y sus hijos
    [SerializeField] private BossAudio bossAudio;
    [SerializeField] private bool playMusicFromBoss = true;
    [SerializeField] private float hurtVoiceCooldown = 1.2f;  // para que no se queje en cada golpe de un combo

    [Header("Ondas de corte (Sword Beam)")]
    [SerializeField] private bool meleeSweepEnabled = true;    // false = solo la onda, sin dano de cono
    [SerializeField] private SlashWaveConfig swordSlashWave = new SlashWaveConfig();
    [SerializeField] private SlashWaveConfig tailSlashWave = new SlashWaveConfig();

    [Header("Gizmos (solo en Scene view)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoArenaColor = Color.magenta;
    [SerializeField] private Color gizmoBorderColor = Color.red;
    [SerializeField] private Color gizmoSwordColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color gizmoTailColor = Color.cyan;
    [SerializeField] private Color gizmoLaserColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color gizmoSummonColor = Color.green;
    [SerializeField] private Color gizmoBeamColor = new Color(0.35f, 0.75f, 1f);
    [SerializeField] private Color gizmoMeteorColor = new Color(1f, 0.5f, 0.1f);

    public event System.Action OnPhaseTwoStarted;

    public bool inCinematic;

    // Parametros del Animator. Fase 1 y fase 2 tienen controllers distintos: cada llamada
    // revisa que el parametro exista en el Animator actual, asi no se llena la consola de avisos.
    private const string ParamIsMoving = "IsMoving";
    private const string ParamIncantation = "Incantation";
    private const string ParamAttackSword = "AttackSword";
    private const string ParamAttackTail = "AttackTail";
    private const string ParamScream = "Scream";
    private const string ParamInvocation = "Invocation";
    private const string ParamMeteorBeam = "MeteorBeam";
    private const string ParamMeteorShower = "MeteorShower";

    private static readonly string[] AllTriggers =
    {
        ParamIncantation, ParamAttackSword, ParamAttackTail, ParamScream,
        ParamInvocation, ParamMeteorBeam, ParamMeteorShower
    };

    private const int AttackNone = -1;
    private const int AttackResonancia = 0;
    private const int AttackPartTitura = 1;
    private const int AttackClimax = 2;
    private const int AttackMeteorBeam = 3;
    private const int AttackMeteorShower = 4;

    private bool isAttacking;
    private bool canInterrupt = true;
    private bool inPhaseTwo;
    private bool phaseTwoStarted;
    private float nextAttackTime;
    private int lastAttack = AttackNone;
    private float nextClimaxAllowedAt;

    private bool castWindowOpen;      // true mientras el casteo actual se puede romper con dano
    private bool castBroken;
    private bool inCastBreakStun;
    private float hpAtCastStart;
    private float castWindowStart;
    private float castWindowDuration;
    private string currentAttackName = "";

    private readonly List<LineRenderer> lasers = new List<LineRenderer>();
    private readonly List<Vector3> laserDirs = new List<Vector3>();

    private LineRenderer beamLine;
    private GameObject activeBeamVfx;
    private Vector3 beamDir = Vector3.forward;

    // Cache de parametros del Animator actual
    private readonly HashSet<int> animParams = new HashSet<int>();
    private Animator animParamsOwner;
    private readonly HashSet<string> warnedParams = new HashSet<string>();

    // Estado para el audio: detecta golpes y stagger comparando contra el frame anterior,
    // asi no hay que tocar EnemyBase ni exponer sus campos privados
    private float lastHpForAudio;
    private bool wasStaggeredForAudio;
    private float nextHurtVoiceAt;

    private Coroutine attackRoutine;
    private Coroutine swingRoutine;
    private GameObject activeBorderVfx;
    private EnemySpawner summonSpawner;

    public string DisplayName => bossDisplayName;
    public float MaxHp => stats != null ? stats.maxHp : 1f;
    public bool HasStagger => stats != null && stats.hasStagger;
    public float StaggerBuild => currentStaggerBuild;
    public float StaggerThreshold => stats != null ? stats.staggerThreshold : 1f;
    public bool InPhaseTwo => inPhaseTwo;

    // ---------- LO QUE LEE EL HUD ----------

    // true mientras el casteo actual se puede romper pegandole
    public bool IsCasting => castWindowOpen;

    // Nombre del ataque que esta lanzando, para mostrarlo sobre la barra
    public string CurrentAttackName => currentAttackName;

    // 0 a 1: cuanto falta para que el ataque salga (la carrera que el boss quiere ganar)
    public float CastTimeProgress => castWindowDuration <= 0f
        ? 0f
        : Mathf.Clamp01((Time.time - castWindowStart) / castWindowDuration);

    // 0 a 1: cuanto falta para romper el casteo (la carrera que el jugador quiere ganar)
    public float CastBreakProgress => castBreakDamage <= 0f
        ? 0f
        : Mathf.Clamp01(CastDamageTaken / castBreakDamage);

    // true mientras el boss esta tambaleandose por un casteo roto
    public bool IsCastBroken => inCastBreakStun;

    public bool CastBreakEnabled => castBreakEnabled;

    // true mientras no se le puede acumular stagger (durante el stagger y su cooldown)
    public bool IsStaggerImmune => IsStaggered || isInStaggerCooldown;

    protected override void Start()
    {
        base.Start();

        if (arenaCenter == null) arenaCenter = transform;
        if (telegraph == null) telegraph = GetComponent<BossAttackTelegraph>();

        if (phaseTwoModel != null) phaseTwoModel.SetActive(false);

        if (vulnerableVfx != null) vulnerableVfx.SetActive(false);
        if (brokenVfx != null) brokenVfx.SetActive(false);
        SetCrystalGlow(false);

        if (phaseOneModel != null)
        {
            phaseOneModel.SetActive(true);

            Animator phaseOneAnim = phaseOneModel.GetComponentInChildren<Animator>(true);
            if (phaseOneAnim != null) anim = phaseOneAnim;
        }

        if (bossAudio == null) bossAudio = GetComponentInChildren<BossAudio>();

        lastHpForAudio = currentHp;

        if (bossAudio != null && playMusicFromBoss)
        {
            bossAudio.StartPhaseOneMusic();
        }

        nextAttackTime = Time.time + firstAttackDelay;

        WarnPhaseTwoSetup();
        WarnClimaxSetup();
        WarnTimingSetup();

        if (bossAudio == null) Debug.LogWarning("[Morgana] Sin BossAudio: el boss va a pelear en silencio");
    }

    // Al destruirse (reinicio del combate) limpia lo que creo fuera de su jerarquia:
    // el borde de la arena y el VFX del rayo no son hijos del boss.
    protected override void OnDisable()
    {
        base.OnDisable();

        ClearBorderVfx();

        if (activeBeamVfx != null)
        {
            Destroy(activeBeamVfx);
            activeBeamVfx = null;
        }
    }

    // Climax es el ataque que invoca enemigos. Sin oleadas ni golemPrefab no invoca nada.
    private void WarnClimaxSetup()
    {
        if (climaxWaves != null) return;

        if (golemPrefab == null)
        {
            Debug.LogWarning("[Morgana] Climax no va a invocar nada: asigna climaxWaves (desde BossEvent) o golemPrefab");
            return;
        }

        if (summonSpawner == null)
        {
            Debug.LogWarning("[Morgana] Climax tiene golemPrefab pero no summonSpawner: BossEvent debe pasarlo con SetArena");
        }
    }

    // Avisa en consola lo que va a fallar en fase 2 antes de llegar a ella
    private void WarnPhaseTwoSetup()
    {
        if (phaseTwoModel == null)
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no asignado: la fase 2 sigue con el modelo de fase 1");
            return;
        }

        if (meteorBeamOrigin == null) Debug.LogWarning("[Morgana] Falta meteorBeamOrigin: el Meteor Beam saldra del frente del boss a " + meteorBeamOriginHeight + " m de altura");
        if (climaxCrystalGlow == null) Debug.LogWarning("[Morgana] Falta climaxCrystalGlow: el cristal de la espada no brillara durante Climax");

        Animator phaseTwoAnim = phaseTwoModel.GetComponentInChildren<Animator>(true);

        if (phaseTwoAnim == null)
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no tiene Animator: en fase 2 no se van a reproducir animaciones");
        }
        else if (phaseTwoAnim.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[Morgana] El Animator del modelo fase 2 no tiene controller. Usa Tools > Morgana > 1. Configurar Animator de fase 2");
        }
    }

    // Los frames donde ocurre cada golpe tienen que caer dentro de la animacion
    private void WarnTimingSetup()
    {
        if (meteorBeamFireStartFrame >= meteorBeamFireEndFrame)
            Debug.LogWarning("[Morgana] Meteor Beam: meteorBeamFireStartFrame debe ser menor que meteorBeamFireEndFrame");

        if (meteorBeamFireEndFrame > meteorBeamFrames)
            Debug.LogWarning("[Morgana] Meteor Beam: el rayo termina despues de la animacion (" + meteorBeamFrames + " frames)");

        if (climaxReleaseFrame > climaxFrames)
            Debug.LogWarning("[Morgana] Climax: climaxReleaseFrame esta despues del final de la animacion (" + climaxFrames + " frames)");

        if (meteorShowerCastFrame > meteorShowerFrames)
            Debug.LogWarning("[Morgana] Meteor Shower: meteorShowerCastFrame esta despues del final de la animacion (" + meteorShowerFrames + " frames)");
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        HandleAudioState();

        if (!phaseTwoStarted && GetHealthPercent() < phaseTwoThreshold)
        {
            StartPhaseTwo();
            return;
        }

        // El jugador le mete suficiente dano durante un casteo: se rompe y queda abierto
        if (castBreakEnabled && castWindowOpen && !castBroken && !inCinematic && CastDamageTaken >= castBreakDamage)
        {
            BreakCast();
            return;
        }

        if (IsStaggered && isAttacking && canInterrupt)
        {
            CancelAttack();
        }

        HandleAi();
    }

    protected override void Die()
    {
        if (isDead) return;

        CancelAttack();

        inCastBreakStun = false;
        if (brokenVfx != null) brokenVfx.SetActive(false);

        if (activeBorderVfx != null) Destroy(activeBorderVfx);

        // Corta todos los bucles de golpe y suelta el grito de muerte
        if (bossAudio != null) bossAudio.PlayDeath();

        base.Die();
    }

    // Suena la queja al recibir dano y el stagger, detectando el cambio contra el frame anterior
    private void HandleAudioState()
    {
        if (bossAudio == null) return;

        if (currentHp < lastHpForAudio - 0.01f && Time.time >= nextHurtVoiceAt)
        {
            nextHurtVoiceAt = Time.time + hurtVoiceCooldown;
            bossAudio.PlayHurt();
        }

        lastHpForAudio = currentHp;

        if (IsStaggered && !wasStaggeredForAudio)
        {
            wasStaggeredForAudio = true;

            // El stagger corta cualquier ataque, asi que tambien su audio
            bossAudio.StopAllLoops();
            bossAudio.PlayStaggerStart();
        }
        else if (!IsStaggered && wasStaggeredForAudio)
        {
            wasStaggeredForAudio = false;
            bossAudio.PlayStaggerEnd();
        }
    }

    // Pegarle durante el stagger no reinicia su duracion. El dano sigue entrando (con el
    // multiplicador de EnemyBase), pero el boss recupera su turno al terminar el stagger.
    protected override bool CanExtendStagger => false;

    private float GetHealthPercent()
    {
        if (stats == null || stats.maxHp <= 0f) return 1f;

        return currentHp / stats.maxHp;
    }

    private void HandleAi()
    {
        if (inCinematic || isAttacking || IsStaggered) return;

        if (!HasValidPlayer())
        {
            SetMoving(false);
            return;
        }

        if (Time.time < nextAttackTime) return;

        isAttacking = true;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        canInterrupt = true;
        PlayIdle(0.1f);

        int choice = ChooseAttack();
        lastAttack = choice;

        // Solo Climax necesita el centro de la arena. Los demas se lanzan donde este,
        // asi el jugador no se pasa el combate persiguiendo al boss hasta el centro.
        if (choice == AttackClimax)
        {
            yield return MoveToCenter();
        }

        if (!HasValidPlayer())
        {
            EndAttack();
            yield break;
        }

        switch (choice)
        {
            case AttackResonancia:
                yield return Resonancia();
                break;
            case AttackPartTitura:
                yield return PartTitura();
                break;
            case AttackClimax:
                yield return ClimaxSerpentinus();
                break;
            case AttackMeteorBeam:
                yield return MeteorBeam();
                break;
            case AttackMeteorShower:
                yield return MeteorShower();
                break;
        }

        EndAttack();
    }

    private int ChooseAttack()
    {
        if (!inPhaseTwo)
        {
            return Random.value < 0.5f ? AttackResonancia : AttackPartTitura;
        }

        // Fase 2: solo los ataques que tienen animacion en el modelo de fase 2.
        // Climax tiene cooldown propio porque puede durar hasta 30 segundos.
        List<int> pool = new List<int>(3);

        if (Time.time >= nextClimaxAllowedAt) pool.Add(AttackClimax);
        pool.Add(AttackMeteorBeam);
        pool.Add(AttackMeteorShower);

        // No repite el mismo ataque dos veces seguidas
        if (pool.Count > 1) pool.Remove(lastAttack);

        return pool[Random.Range(0, pool.Count)];
    }

    private void EndAttack()
    {
        HideLasers();
        HideBeam();
        SetMoving(false);
        CloseCastWindow();
        SetCrystalGlow(false);

        // Red de seguridad: al terminar un ataque no debe quedar ningun bucle sonando.
        // Aqui no se pisa el tambaleo, porque un casteo roto pasa por CancelAttack, no por aqui.
        if (bossAudio != null) bossAudio.StopAllLoops();

        attackRoutine = null;
        isAttacking = false;
        nextAttackTime = Time.time + pauseBetweenAttacks;
    }

    private void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (swingRoutine != null)
        {
            StopCoroutine(swingRoutine);
            swingRoutine = null;
        }

        ResetAnimatorTriggers();
        SetMoving(false);
        HideLasers();
        HideBeam();
        CloseCastWindow();
        SetCrystalGlow(false);

        // Se cortan los bucles del ataque que se interrumpio: laser, borde, rayo.
        // El canal del casteo no se toca aqui, porque si se rompio lo usa el tambaleo.
        if (bossAudio != null)
        {
            bossAudio.StopLaser();
            bossAudio.StopArenaBorder();
            bossAudio.StopMeteorBeam();
        }

        PlayIdle(0.1f);

        // Si se interrumpe Climax a medias, el borde de la arena tiene que irse con el.
        // Los enemigos ya invocados se quedan: el jugador los tiene que limpiar igual.
        // Los meteoritos que ya estan cayendo tambien siguen: ya estaban en el aire.
        ClearBorderVfx();

        if (telegraph != null) telegraph.CancelAll();

        isAttacking = false;
        nextAttackTime = Time.time + pauseBetweenAttacks;
    }

    // ---------- VENTANA DE CASTIGO ----------

    // Dano que el boss recibio desde que empezo el casteo actual
    private float CastDamageTaken => Mathf.Max(0f, hpAtCastStart - currentHp);

    // Abre la ventana: a partir de aqui el jugador puede romper el ataque pegandole.
    // 'duration' es lo que dura el casteo, para que el HUD muestre la carrera contra el reloj.
    private void OpenCastWindow(float duration)
    {
        castWindowOpen = true;
        castBroken = false;
        hpAtCastStart = currentHp;
        castWindowStart = Time.time;
        castWindowDuration = Mathf.Max(0.01f, duration);

        // Aviso en el mundo: el jugador mira al boss, no al HUD
        if (vulnerableVfx != null) vulnerableVfx.SetActive(true);
        if (bossAudio != null) bossAudio.StartCastCharge();
    }

    private void CloseCastWindow()
    {
        // Solo actua si estaba abierta, si no apagaria bucles que no abrio
        bool wasOpen = castWindowOpen;
        castWindowOpen = false;

        if (vulnerableVfx != null) vulnerableVfx.SetActive(false);

        // Si el casteo se rompio, el canal ya lo tomo el sonido de tambaleo
        if (wasOpen && !castBroken && bossAudio != null) bossAudio.StopCastCharge();
    }

    // El casteo se rompe: el ataque se pierde y el boss queda abierto varios segundos
    private void BreakCast()
    {
        castBroken = true;

        Debug.Log("[Morgana] Casteo roto por dano, el boss queda abierto " + castBreakStunDuration + "s");

        CancelAttack();

        // Despues de CancelAttack, para que el tambaleo tome el canal que dejo la carga.
        // El cooldown evita que la queja del golpe suene encima del grito de rotura.
        if (bossAudio != null)
        {
            nextHurtVoiceAt = Time.time + hurtVoiceCooldown;
            bossAudio.PlayCastBroken();
        }

        StartCoroutine(CastBrokenRoutine());
    }

    private IEnumerator CastBrokenRoutine()
    {
        // Bloquea HandleAi mientras esta tambaleandose
        isAttacking = true;
        inCastBreakStun = true;

        if (castBreakVfx != null)
        {
            GameObject vfx = Instantiate(castBreakVfx, transform.position, transform.rotation);
            Destroy(vfx, 2f);
        }

        if (brokenVfx != null) brokenVfx.SetActive(true);

        if (!string.IsNullOrEmpty(castBreakTrigger)) Trigger(castBreakTrigger);

        yield return new WaitForSeconds(castBreakStunDuration);

        inCastBreakStun = false;
        if (brokenVfx != null) brokenVfx.SetActive(false);

        if (isDead) yield break;

        if (bossAudio != null) bossAudio.StopCastBroken();

        isAttacking = false;
        nextAttackTime = Time.time + pauseBetweenAttacks;
    }

    // Ventana visible al terminar un ataque: el boss se queda quieto en reposo
    private IEnumerator Recover(float duration)
    {
        SetMoving(false);
        HideLasers();
        HideBeam();

        PlayIdle(0.15f);

        yield return new WaitForSeconds(duration);
    }

    // ---------- INTRO Y CAMBIO DE FASE ----------

    // Lo llama BossEvent durante la introduccion
    public IEnumerator PlayIntro()
    {
        SetMoving(false);
        Trigger(ParamScream);

        if (bossAudio != null) bossAudio.PlayIntroScream();

        yield return new WaitForSeconds(FramesToSeconds(screamFrames));
    }

    // La llama BossEvent al tocar el suelo durante la entrada
    public void PlayLandingSound()
    {
        if (bossAudio != null) bossAudio.PlayLandImpact();
    }

    private void StartPhaseTwo()
    {
        phaseTwoStarted = true;

        inCinematic = true;
        CancelAttack();

        Debug.Log("[Morgana] Vida bajo el umbral, inicia el cambio de fase");

        if (OnPhaseTwoStarted != null)
        {
            OnPhaseTwoStarted.Invoke();
        }
        else
        {
            StartCoroutine(PhaseTwoFallback());
        }
    }

    private IEnumerator PhaseTwoFallback()
    {
        yield return PlayPhaseTwoTransition();

        if (!isDead) SetCinematic(false);
    }

    // Grito de la fase 1 (opcional), VFX que tapa el cambio, cambio de modelo y Scream de la fase 2 (70 frames)
    public IEnumerator PlayPhaseTwoTransition()
    {
        FaceTargetInstant();
        SetMoving(false);

        // Nada del ataque anterior debe seguir sonando durante la cinematica
        if (bossAudio != null) bossAudio.StopAllLoops();

        if (screamBeforeSwap)
        {
            if (bossAudio != null) bossAudio.PlayPhaseScream();

            Trigger(ParamScream);
            yield return new WaitForSeconds(FramesToSeconds(screamFrames));
            if (isDead) yield break;
        }

        if (transformVfx != null)
        {
            GameObject vfx = Instantiate(transformVfx, transform.position, transform.rotation);
            Destroy(vfx, 3f);
        }

        if (bossAudio != null) bossAudio.PlayPhaseTransform();

        yield return new WaitForSeconds(swapDelay);
        if (isDead) yield break;

        SwapToPhaseTwoModel();

        // Un frame para que el Animator del modelo nuevo se inicialice antes del trigger
        yield return null;
        if (isDead) yield break;

        if (!string.IsNullOrEmpty(phaseTwoIntroTrigger)) Trigger(phaseTwoIntroTrigger);

        if (bossAudio != null)
        {
            bossAudio.PlayPhaseTwoEntrance();

            if (playMusicFromBoss) bossAudio.StartPhaseTwoMusic();
        }

        yield return new WaitForSeconds(FramesToSeconds(phaseTwoScreamFrames));
        if (isDead) yield break;

        inPhaseTwo = true;

        // El primer Climax no puede salir apenas termina el grito
        nextClimaxAllowedAt = Time.time + pauseBetweenAttacks;

        Debug.Log("[Morgana] Fase 2 activa");
    }

    private void SwapToPhaseTwoModel()
    {
        if (phaseTwoModel == null)
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no asignado, se sigue con el modelo actual");
            return;
        }

        ResetAnimatorTriggers();
        SetMoving(false);

        if (phaseOneModel != null) phaseOneModel.SetActive(false);
        phaseTwoModel.SetActive(true);

        // Cambia los huesos a los del modelo nuevo. Los del modelo apagado ya no se actualizan.
        if (swordTipPhaseTwo != null) swordTip = swordTipPhaseTwo;
        if (tailTipPhaseTwo != null) tailTip = tailTipPhaseTwo;

        if (laserOriginsPhaseTwo != null && laserOriginsPhaseTwo.Length > 0)
        {
            laserOrigins = laserOriginsPhaseTwo;

            // Los laseres se recrean con la cantidad de juntas del modelo nuevo
            HideLasers();
        }

        Animator phaseTwoAnim = phaseTwoModel.GetComponentInChildren<Animator>(true);

        if (phaseTwoAnim != null)
        {
            anim = phaseTwoAnim;
        }
        else
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no tiene Animator");
        }
    }

    // ---------- MOVIMIENTO AL CENTRO ----------

    // Vuelve al centro de la arena antes de Climax. Durante el ataque no se mueve.
    private IEnumerator MoveToCenter()
    {
        Vector3 target = ArenaCenterPos();
        target.y = transform.position.y;

        float timer = 0f;
        SetMoving(true);

        while (timer < moveToCenterTimeout && FlatDistance(transform.position, target) > centerReachDistance)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;

            transform.position += dir.normalized * walkSpeed * Time.deltaTime;
            RotateToward(dir, turnSpeed);

            timer += Time.deltaTime;
            yield return null;
        }

        SetMoving(false);
    }

    // ---------- RESONANCIA (FASE 1) ----------

    private IEnumerator Resonancia()
    {
        canInterrupt = true;
        currentAttackName = nameResonancia;
        Debug.Log("[Morgana] Resonancia");

        Trigger(ParamIncantation);
        if (bossAudio != null) bossAudio.PlayIncantation();

        // Gesto de espada: lineas finas que muestran hacia donde van los laseres.
        // Durante el aviso el jugador puede romper el casteo si le mete suficiente dano.
        OpenCastWindow(resonanciaWindUp);
        yield return LaserRoutine(resonanciaWindUp, false, LaserCount, resonanciaTrackSpeed);
        CloseCastWindow();

        if (!HasValidPlayer())
        {
            HideLasers();
            yield break;
        }

        // Laseres constantes que hacen dano por donde pasan
        yield return LaserRoutine(resonanciaDuration, true, LaserCount, resonanciaTrackSpeed);

        yield return Recover(recoveryResonancia);
    }

    // Dibuja y, si damaging es true, hace dano con los laseres durante 'duration' segundos.
    // Todos los laseres apuntan al pecho del jugador y se juntan en ese punto.
    private IEnumerator LaserRoutine(float duration, bool damaging, int count, float trackDegPerSec)
    {
        count = Mathf.Max(1, count);
        EnsureLasers(count);

        // El zumbido fino del apuntado y el laser que quema son sonidos distintos
        if (bossAudio != null)
        {
            if (damaging) bossAudio.StartLaserFire();
            else bossAudio.StartLaserWarn();
        }

        bool initialized = false;
        float t = 0f;
        float nextTickAt = 0f;

        while (t < duration)
        {
            if (!HasValidPlayer()) break;

            // El boss gira hacia el jugador a velocidad fija mientras dura el laser
            FaceTargetAtSpeed(castTurnSpeed);

            float dt = Time.deltaTime;
            Vector3 chest = PlayerChestPos();
            Vector3 side = Vector3.Cross(Vector3.up, FlatDirToPlayer()).normalized;

            for (int i = 0; i < count; i++)
            {
                Transform origin = GetLaserOrigin(i);

                // Cada laser nace separado (lateral), pero todos llegan al mismo punto: el pecho del jugador
                float lateral = count == 1 ? 0f : Mathf.Lerp(-laserStartSpread * 0.5f, laserStartSpread * 0.5f, i / (count - 1f));
                Vector3 start = origin.position + side * lateral;
                Vector3 desired = chest - start;
                if (desired.sqrMagnitude < 0.0001f) desired = transform.forward;
                desired.Normalize();

                // Sigue al jugador con velocidad limitada, para que se pueda esquivar
                if (!initialized)
                {
                    laserDirs[i] = desired;
                }
                else
                {
                    laserDirs[i] = Vector3.RotateTowards(laserDirs[i], desired, trackDegPerSec * Mathf.Deg2Rad * dt, 0f);
                }

                Vector3 dir = laserDirs[i];
                Vector3 end = start + dir * laserLength;

                if (Physics.Raycast(start, dir, out RaycastHit hit, laserLength, laserObstacles))
                {
                    end = hit.point;
                }

                DrawLaser(lasers[i], start, end, damaging);

                if (damaging && Time.time >= nextTickAt && SegmentTouchesPlayer(start, end, laserRadius))
                {
                    nextTickAt = Time.time + laserTickInterval;
                    DamagePlayer(laserDamagePerTick, dir);

                    if (bossAudio != null) bossAudio.PlayLaserTickHit(chest);
                }
            }

            initialized = true;
            t += dt;
            yield return null;
        }

        HideLasers();

        if (bossAudio != null) bossAudio.StopLaser();
    }

    // ---------- PART-TITURA (FASE 1) ----------

    private IEnumerator PartTitura()
    {
        canInterrupt = true;
        currentAttackName = namePartTitura;
        Debug.Log("[Morgana] Part-Titura");

        SetMoving(false);

        float timer = 0f;
        float nextSwingAt = 0f;
        bool nextIsSword = true;

        while (timer < partTituraDuration)
        {
            if (!HasValidPlayer()) break;

            // Solo rota para mirar al jugador. No se mueve de su lugar.
            FaceTarget(turnSpeed);

            if (timer >= nextSwingAt)
            {
                bool isSword = nextIsSword;
                nextIsSword = !nextIsSword;

                LaunchSwing(isSword);
                nextSwingAt = timer + partTituraHitInterval;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        yield return Recover(recoveryPartTitura);
    }

    private void LaunchSwing(bool isSword)
    {
        Trigger(isSword ? ParamAttackSword : ParamAttackTail);

        if (bossAudio != null) bossAudio.PlaySwing(isSword);

        float activeStart = FramesToSeconds(isSword ? swordActiveStartFrame : tailActiveStartFrame);
        float activeEnd = FramesToSeconds(isSword ? swordActiveEndFrame : tailActiveEndFrame);
        float damage = isSword ? swordDamage : tailDamage;

        if (swingRoutine != null) StopCoroutine(swingRoutine);

        swingRoutine = StartCoroutine(SwingRoutine(isSword, activeStart, activeEnd, damage));
    }

    // La estela de la punta es la que hace dano: se revisa cada frame el tramo que recorrio la punta
    private IEnumerator SwingRoutine(bool isSword, float activeStart, float activeEnd, float damage)
    {
        yield return new WaitForSeconds(activeStart);

        if (telegraph != null) telegraph.SetTrail(isSword, true);

        LaunchSlashWave(isSword);

        float window = Mathf.Max(0f, activeEnd - activeStart);
        float t = 0f;
        bool hitDone = false;
        Vector3 prev = WeaponOrigin(isSword);

        while (t < window)
        {
            Vector3 cur = WeaponOrigin(isSword);

            if (meleeSweepEnabled && !hitDone && HasValidPlayer() && SweepTouchesPlayer(prev, cur))
            {
                hitDone = true;

                Vector3 hitDir = cur - prev;
                hitDir.y = 0f;
                if (hitDir.sqrMagnitude < 0.0001f) hitDir = transform.forward;

                DamagePlayer(damage, hitDir.normalized);

                if (bossAudio != null) bossAudio.PlayMeleeHit(cur);
            }

            prev = cur;
            t += Time.deltaTime;
            yield return null;
        }

        if (telegraph != null) telegraph.SetTrail(isSword, false);

        swingRoutine = null;
    }

    // Onda de corte: sale de la punta del arma y viaja hacia el pecho del jugador
    private void LaunchSlashWave(bool isSword)
    {
        SlashWaveConfig cfg = isSword ? swordSlashWave : tailSlashWave;
        if (cfg == null || !HasValidPlayer()) return;

        Vector3 origin = WeaponOrigin(isSword);
        Vector3 dir = PlayerChestPos() - origin;

        // Espada: sigue la altura del pecho del jugador. Cola: viaja a ras del suelo.
        BossSlashWave.Launch(origin, dir, cfg, player.transform, !isSword);

        if (bossAudio != null) bossAudio.PlayWaveLaunch(isSword);
    }

    private Vector3 PlayerChestPos()
    {
        return player.transform.position + Vector3.up * 1f;
    }

    private bool SweepTouchesPlayer(Vector3 a, Vector3 b)
    {
        Vector3 feet = player.transform.position + Vector3.up * 0.2f;
        Vector3 chest = player.transform.position + Vector3.up * 1f;

        return DistancePointToSegment(feet, a, b) <= weaponThickness + bodyRadius
            || DistancePointToSegment(chest, a, b) <= weaponThickness + bodyRadius;
    }

    // ---------- CLIMAX SERPENTINUS (FASE 2) ----------

    // Animacion 'Invocation' (80 frames). El cristal de la espada brilla toda la invocacion.
    // 0 -> climaxReleaseFrame: canaliza (se puede romper).
    // climaxReleaseFrame: desata la energia, el borde de la arena se ilumina X segundos (aviso).
    // Tras el aviso: el borde quema (DoT) e invoca Y enemigos.
    private IEnumerator ClimaxSerpentinus()
    {
        // Interrumpible: si el jugador llena la barra de stagger, el ataque se corta.
        // Si no fuera asi, la barra subiria durante 30 segundos sin que pase nada.
        canInterrupt = true;
        currentAttackName = nameClimax;
        nextClimaxAllowedAt = Time.time + climaxCooldown;
        Debug.Log("[Morgana] Climax Serpentinus");

        float attackStart = Time.time;
        float animEnd = attackStart + FramesToSeconds(climaxFrames);
        float release = FramesToSeconds(Mathf.Clamp(climaxReleaseFrame, 0, climaxFrames));

        FaceTargetInstant();
        Trigger(ParamInvocation);
        SetCrystalGlow(true);

        if (bossAudio != null)
        {
            bossAudio.PlayIncantation();
            bossAudio.PlayCrystalGlow();
        }

        // Durante la canalizacion el jugador puede negar el ataque entero
        OpenCastWindow(release);
        yield return new WaitForSeconds(release);
        CloseCastWindow();

        // Desata la energia alrededor de todo el escenario
        if (bossAudio != null)
        {
            bossAudio.PlayClimaxScream();
            bossAudio.StartArenaBorder();
        }

        if (arenaBorderVfx != null)
        {
            activeBorderVfx = Instantiate(arenaBorderVfx, ArenaCenterPos(), Quaternion.identity);
        }

        // X segundos de aviso: el borde brilla pero todavia no quema
        yield return WaitWhileGlowing(borderWarningTime, animEnd);

        bool usingWaves = StartClimaxSummon();

        if (bossAudio != null) bossAudio.PlayWaveSummon();

        float elapsed = Time.time - attackStart;
        float maxDuration = Mathf.Max(0f, climaxDuration - elapsed);
        yield return ClimaxActivePhase(maxDuration, usingWaves, animEnd);

        SetCrystalGlow(false);
        ClearBorderVfx();

        if (bossAudio != null) bossAudio.StopArenaBorder();

        yield return Recover(recoveryClimax);
    }

    // Espera 'duration' segundos y apaga el cristal cuando termina la animacion
    private IEnumerator WaitWhileGlowing(float duration, float animEnd)
    {
        float end = Time.time + duration;

        while (Time.time < end)
        {
            if (Time.time >= animEnd) SetCrystalGlow(false);
            yield return null;
        }
    }

    // Lanza las oleadas si hay WaveManager. Si no, cae a la invocacion simple de golems.
    // Devuelve true cuando se usaron oleadas.
    private bool StartClimaxSummon()
    {
        if (climaxWaves != null)
        {
            // Permite repetir el ataque mas de una vez en el mismo combate
            climaxWaves.ResetWaves();
            climaxWaves.StartWaves();

            Debug.Log("[Morgana] Climax lanza " + climaxWaves.WaveCount + " oleadas");
            return true;
        }

        SummonGolems();
        return false;
    }

    // El borde de la arena hace dano mientras dura el ataque.
    // Con oleadas, el ataque termina antes si el jugador las limpia.
    private IEnumerator ClimaxActivePhase(float maxDuration, bool usingWaves, float animEnd)
    {
        float timer = 0f;
        float nextTickAt = 0f;

        while (timer < maxDuration)
        {
            if (Time.time >= animEnd) SetCrystalGlow(false);

            if (usingWaves && climaxEndsWhenWavesCleared && climaxWaves != null && climaxWaves.AllCleared)
            {
                Debug.Log("[Morgana] Oleadas limpiadas, Climax termina antes de tiempo");
                break;
            }

            if (timer >= nextTickAt)
            {
                nextTickAt = timer + borderTickInterval;

                if (HasValidPlayer() && IsInsideBorder(player.transform.position))
                {
                    DamagePlayer(borderDamagePerSecond * borderTickInterval, Vector3.zero);

                    if (bossAudio != null) bossAudio.PlayBorderTick(player.transform.position);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsInsideBorder(Vector3 pos)
    {
        Vector3 center = ArenaCenterPos();

        return FlatDistance(pos, center) >= arenaRadius - borderWidth;
    }

    private void ClearBorderVfx()
    {
        if (activeBorderVfx == null) return;

        Destroy(activeBorderVfx);
        activeBorderVfx = null;
    }

    private void SetCrystalGlow(bool on)
    {
        if (climaxCrystalGlow != null && climaxCrystalGlow.activeSelf != on)
        {
            climaxCrystalGlow.SetActive(on);
        }
    }

    // Respaldo cuando no hay WaveManager: suelta los golems de una sola tanda
    private void SummonGolems()
    {
        if (golemPrefab == null)
        {
            Debug.LogWarning("[Morgana] Climax no invoca nada: falta climaxWaves y tambien golemPrefab");
            return;
        }

        if (summonSpawner == null)
        {
            Debug.LogWarning("[Morgana] No hay summonSpawner, no se invocan golems");
            return;
        }

        Transform fallbackCenter = arenaCenter != null ? arenaCenter : transform;

        for (int i = 0; i < golemsToSummon; i++)
        {
            Transform point = (summonPoints != null && summonPoints.Length > 0)
                ? summonPoints[i % summonPoints.Length]
                : fallbackCenter;

            summonSpawner.SpawnAt(golemPrefab, point, fallbackCenter);
        }
    }

    // ---------- METEOR BEAM (FASE 2) ----------

    // Animacion 'MeteorBeam' (70 frames). Grito desgarrador y un rayo en trayectoria fija.
    // 0 -> lockFrame: gira hacia el jugador. lockFrame -> fireStart: la trayectoria ya no se mueve,
    // el aviso queda fijo en el suelo y el jugador tiene tiempo de salir. fireStart -> fireEnd: rayo.
    private IEnumerator MeteorBeam()
    {
        canInterrupt = true;
        currentAttackName = nameMeteorBeam;
        Debug.Log("[Morgana] Meteor Beam");

        float lockTime = FramesToSeconds(meteorBeamLockFrame);
        float fireStart = FramesToSeconds(meteorBeamFireStartFrame);
        float fireEnd = Mathf.Max(fireStart, FramesToSeconds(meteorBeamFireEndFrame));
        float animLength = FramesToSeconds(meteorBeamFrames);

        FaceTargetInstant();
        beamDir = FlatForward();

        Trigger(ParamMeteorBeam);
        if (bossAudio != null) bossAudio.PlayMeteorBeamScream();

        // La franja del suelo sigue al boss mientras apunta y se congela al fijar la trayectoria
        if (telegraph != null)
        {
            telegraph.StartTelegraph(
                () => BossAttackTelegraph.Rect(FlatPos(transform.position), beamDir, 0f, meteorBeamWidth, meteorBeamLength),
                fireStart);
        }

        OpenCastWindow(fireStart);

        // Despues de OpenCastWindow: el rayo tiene su propia carga y reemplaza a la generica
        if (bossAudio != null) bossAudio.StartMeteorBeamCharge();

        float t = 0f;

        while (t < fireStart)
        {
            if (t < lockTime)
            {
                FaceTargetAtSpeed(meteorBeamAimTurnSpeed);
                beamDir = FlatForward();
            }

            t += Time.deltaTime;
            yield return null;
        }

        CloseCastWindow();

        if (telegraph != null) telegraph.OnHit();
        if (bossAudio != null) bossAudio.StartMeteorBeamFire();

        Vector3 groundStart = FlatPos(transform.position);
        float nextTickAt = 0f;

        while (t < fireEnd)
        {
            float length = BeamLengthClipped(groundStart);
            Vector3 visualStart = BeamOriginPos();
            Vector3 visualEnd = groundStart + beamDir * length + Vector3.up * 1f;

            DrawBeam(visualStart, visualEnd, length);

            if (Time.time >= nextTickAt && HasValidPlayer() && PlayerInBeam(groundStart, length))
            {
                nextTickAt = Time.time + meteorBeamTickInterval;
                DamagePlayer(meteorBeamDamagePerTick, beamDir);

                if (bossAudio != null) bossAudio.PlayMeteorBeamHit(PlayerChestPos());
            }

            t += Time.deltaTime;
            yield return null;
        }

        HideBeam();
        if (bossAudio != null) bossAudio.StopMeteorBeam();

        // Deja terminar la animacion antes de la ventana de recuperacion
        if (t < animLength) yield return new WaitForSeconds(animLength - t);

        yield return Recover(recoveryMeteorBeam);
    }

    private Vector3 BeamOriginPos()
    {
        if (meteorBeamOrigin != null) return meteorBeamOrigin.position;

        return transform.position + Vector3.up * meteorBeamOriginHeight + beamDir * 0.5f;
    }

    // Largo real del rayo: se corta si choca con una pared
    private float BeamLengthClipped(Vector3 groundStart)
    {
        Vector3 rayStart = groundStart + Vector3.up * 1f;

        if (laserObstacles.value != 0 &&
            Physics.Raycast(rayStart, beamDir, out RaycastHit hit, meteorBeamLength, laserObstacles, QueryTriggerInteraction.Ignore))
        {
            return hit.distance;
        }

        return meteorBeamLength;
    }

    // La franja de dano va por el suelo, como el aviso: si el jugador la pisa, le pega
    private bool PlayerInBeam(Vector3 groundStart, float length)
    {
        Vector3 p = FlatPos(player.transform.position);
        Vector3 a = groundStart;
        Vector3 b = groundStart + beamDir * length;

        // Si salta muy alto por encima del rayo, se salva
        float height = player.transform.position.y - transform.position.y;
        if (height > 3f) return false;

        return DistancePointToSegment(p, a, b) <= meteorBeamWidth * 0.5f + bodyRadius;
    }

    private void DrawBeam(Vector3 start, Vector3 end, float length)
    {
        if (meteorBeamVfx != null)
        {
            if (activeBeamVfx == null)
            {
                activeBeamVfx = Instantiate(meteorBeamVfx, start, Quaternion.identity);
            }

            Vector3 dir = end - start;
            activeBeamVfx.transform.position = start;
            if (dir.sqrMagnitude > 0.0001f) activeBeamVfx.transform.rotation = Quaternion.LookRotation(dir.normalized);

            Vector3 s = activeBeamVfx.transform.localScale;
            activeBeamVfx.transform.localScale = new Vector3(s.x, s.y, dir.magnitude);
            return;
        }

        EnsureBeamLine();

        beamLine.enabled = true;
        beamLine.SetPosition(0, start);
        beamLine.SetPosition(1, end);
        beamLine.startWidth = meteorBeamVisualWidth;
        beamLine.endWidth = meteorBeamVisualWidth;
        beamLine.startColor = meteorBeamColor;
        beamLine.endColor = meteorBeamColor;
    }

    private void EnsureBeamLine()
    {
        if (beamLine != null) return;

        GameObject go = new GameObject("MeteorBeam_Line");
        go.transform.SetParent(transform, false);

        beamLine = go.AddComponent<LineRenderer>();
        beamLine.useWorldSpace = true;
        beamLine.positionCount = 2;
        beamLine.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        beamLine.enabled = false;
    }

    private void HideBeam()
    {
        if (beamLine != null) beamLine.enabled = false;

        if (activeBeamVfx != null)
        {
            Destroy(activeBeamVfx);
            activeBeamVfx = null;
        }
    }

    // ---------- METEOR SHOWER (FASE 2) ----------

    // Animacion 'MeteorShower' (70 frames). Hasta meteorShowerCastFrame se puede romper el casteo.
    // Despues invoca la lluvia: cada meteorito marca el suelo, cae y hace dano en area.
    private IEnumerator MeteorShower()
    {
        canInterrupt = true;
        currentAttackName = nameMeteorShower;
        Debug.Log("[Morgana] Meteor Shower");

        FaceTargetInstant();
        Trigger(ParamMeteorShower);

        if (bossAudio != null) bossAudio.PlayMeteorShowerCast();

        float cast = FramesToSeconds(Mathf.Clamp(meteorShowerCastFrame, 0, meteorShowerFrames));
        float animLength = FramesToSeconds(meteorShowerFrames);

        OpenCastWindow(cast);
        yield return new WaitForSeconds(cast);
        CloseCastWindow();

        int count = Mathf.Max(1, meteorCount);
        float interval = count > 1 ? meteorSpawnDuration / (count - 1) : 0f;
        float spent = 0f;

        for (int i = 0; i < count; i++)
        {
            if (!HasValidPlayer()) break;

            LaunchMeteor(PickMeteorTarget());

            if (i < count - 1 && interval > 0f)
            {
                yield return new WaitForSeconds(interval);
                spent += interval;
            }
        }

        // Espera a que caiga el ultimo meteorito y a que termine la animacion
        float animLeft = Mathf.Max(0f, animLength - cast - spent);
        float fall = meteor != null ? meteor.fallTime : 0f;
        yield return new WaitForSeconds(Mathf.Max(fall, animLeft));

        yield return Recover(recoveryMeteorShower);
    }

    private void LaunchMeteor(Vector3 target)
    {
        BossAudio audioRef = bossAudio;

        BossMeteor.Launch(
            target,
            meteor,
            player != null ? player.transform : null,
            pos => { if (audioRef != null) audioRef.PlayMeteorFall(pos); },
            pos => { if (audioRef != null) audioRef.PlayMeteorImpact(pos); });
    }

    private Vector3 PickMeteorTarget()
    {
        Vector3 playerPos = player.transform.position;

        if (Random.value < meteorAimAtPlayerChance)
        {
            return playerPos;
        }

        Vector3 center = meteorAreaCenter == MeteorAreaCenter.Player ? playerPos : transform.position;
        Vector2 r = Random.insideUnitCircle * meteorAreaRadius;
        Vector3 target = new Vector3(center.x + r.x, playerPos.y, center.z + r.y);

        // No deja caer meteoritos fuera de la arena
        Vector3 arena = ArenaCenterPos();
        Vector3 fromArena = FlatPos(target) - FlatPos(arena);
        float limit = Mathf.Max(0.5f, arenaRadius - 0.5f);

        if (fromArena.magnitude > limit)
        {
            Vector3 clamped = FlatPos(arena) + fromArena.normalized * limit;
            target = new Vector3(clamped.x, playerPos.y, clamped.z);
        }

        return target;
    }

    // ---------- LASERES Y LINEAS ----------

    private void EnsureLasers(int count)
    {
        while (lasers.Count < count)
        {
            GameObject go = new GameObject("Laser_" + lasers.Count);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.enabled = false;

            lasers.Add(lr);
            laserDirs.Add(Vector3.forward);
        }
    }

    private void DrawLaser(LineRenderer lr, Vector3 start, Vector3 end, bool damaging)
    {
        lr.enabled = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        float w = damaging ? laserWidth : laserWarnWidth;
        Color c = damaging ? laserColor : laserWarnColor;

        lr.startWidth = w;
        lr.endWidth = w;
        lr.startColor = c;
        lr.endColor = c;
    }

    private void HideLasers()
    {
        for (int i = 0; i < lasers.Count; i++)
        {
            if (lasers[i] != null) lasers[i].enabled = false;
        }
    }

    // Un laser por junta asignada (5 juntas = 5 laseres). Si no hay juntas, usa resonanciaLaserCount
    private int LaserCount => laserOrigins != null && laserOrigins.Length > 0
        ? laserOrigins.Length
        : Mathf.Max(1, resonanciaLaserCount);

    private Transform GetLaserOrigin(int index)
    {
        if (laserOrigins == null || laserOrigins.Length == 0 || laserOrigins[index % laserOrigins.Length] == null)
        {
            return transform;
        }

        return laserOrigins[index % laserOrigins.Length];
    }

    private bool SegmentTouchesPlayer(Vector3 a, Vector3 b, float radius)
    {
        Vector3 feet = player.transform.position + Vector3.up * 0.2f;
        Vector3 chest = player.transform.position + Vector3.up * 1f;

        return DistancePointToSegment(feet, a, b) <= radius + bodyRadius
            || DistancePointToSegment(chest, a, b) <= radius + bodyRadius;
    }

    private Vector3 WeaponOrigin(bool sword)
    {
        Transform tip = sword ? swordTip : tailTip;
        return tip != null ? tip.position : transform.position;
    }

    // ---------- DANO ----------

    private void DamagePlayer(float amount, Vector3 hitDir)
    {
        if (!HasValidPlayer()) return;

        IDamageable damageable = player.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = amount,
            hitDirection = hitDir,
        };

        damageable.TakeDamage(in info);
    }

    // ---------- ANIMATOR ----------

    // Fase 1 y fase 2 tienen controllers distintos. Solo se mandan parametros que existen
    // en el Animator actual; si falta uno se avisa una sola vez en consola.
    private bool HasParam(string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;

        if (animParamsOwner != anim || animParams.Count == 0)
        {
            animParams.Clear();

            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                animParams.Add(p.nameHash);
            }

            // Si el Animator aun no estaba listo la lista sale vacia: se vuelve a leer la proxima vez
            animParamsOwner = animParams.Count > 0 ? anim : null;
        }

        return animParams.Contains(Animator.StringToHash(paramName));
    }

    private void SetMoving(bool value)
    {
        if (HasParam(ParamIsMoving)) anim.SetBool(ParamIsMoving, value);

        // El bucle de pasos no se reinicia aunque esto se llame en cada frame
        if (bossAudio != null) bossAudio.SetSteps(value);
    }

    private void Trigger(string paramName)
    {
        if (anim == null) return;

        if (HasParam(paramName))
        {
            anim.SetTrigger(paramName);
            return;
        }

        string key = anim.GetInstanceID() + ":" + paramName;

        if (warnedParams.Add(key))
        {
            Debug.LogWarning("[Morgana] El Animator '" + anim.name + "' no tiene el trigger '" + paramName + "'. Revisa su controller.");
        }
    }

    private void ResetAnimatorTriggers()
    {
        if (anim == null) return;

        foreach (string trigger in AllTriggers)
        {
            if (HasParam(trigger)) anim.ResetTrigger(trigger);
        }
    }

    // Los controllers de Morgana llaman 'Iddle' al estado de reposo. Si no existe, no hace nada
    private void PlayIdle(float fade)
    {
        if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null) return;

        int hash = Animator.StringToHash(idleStateName);

        if (anim.HasState(0, hash))
        {
            anim.CrossFadeInFixedTime(hash, fade);
        }
    }

    private float FramesToSeconds(int frames)
    {
        return animFps > 0f ? frames / animFps : 0f;
    }

    // ---------- GEOMETRIA Y UTILIDADES ----------

    private Vector3 ArenaCenterPos()
    {
        return arenaCenter != null ? arenaCenter.position : transform.position;
    }

    private static Vector3 FlatPos(Vector3 p)
    {
        p.y = 0f;
        return p;
    }

    private Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;

        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float lengthSq = ab.sqrMagnitude;

        float t = lengthSq > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSq) : 0f;

        return Vector3.Distance(p, a + ab * t);
    }

    private Vector3 FlatDirToPlayer()
    {
        if (!HasValidPlayer()) return transform.forward;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    private void RotateToward(Vector3 dir, float speed)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    private void FaceTarget(float speed)
    {
        if (!HasValidPlayer()) return;

        RotateToward(FlatDirToPlayer(), speed);
    }

    // Gira hacia el jugador a una velocidad fija (grados/s), sin saltar a la rotacion final
    private void FaceTargetAtSpeed(float degPerSec)
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = FlatDirToPlayer();
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, degPerSec * Time.deltaTime);
    }

    private void FaceTargetInstant()
    {
        if (!HasValidPlayer()) return;

        transform.rotation = Quaternion.LookRotation(FlatDirToPlayer());
    }

    // ---------- API PARA BossEvent ----------

    public void FacePlayerInstant()
    {
        FaceTargetInstant();
    }

    public void SetCinematic(bool value)
    {
        inCinematic = value;

        if (!value)
        {
            nextAttackTime = Time.time + firstAttackDelay;
        }
    }

    // BossEvent lo llama justo despues de instanciar el boss.
    // El prefab del boss no puede guardar referencias de escena, por eso se pasan aqui.
    public void SetArena(Transform center, Transform[] points, EnemySpawner spawner, WaveManager waves = null)
    {
        if (center != null) arenaCenter = center;
        if (points != null && points.Length > 0) summonPoints = points;
        if (spawner != null) summonSpawner = spawner;
        if (waves != null) climaxWaves = waves;
    }

    // ---------- GIZMOS ----------

    private void DrawPolyline(Vector3[] pts, Color color)
    {
        Gizmos.color = color;

        for (int i = 0; i < pts.Length; i++)
        {
            Gizmos.DrawLine(pts[i], pts[(i + 1) % pts.Length]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 center = ArenaCenterPos();

        Gizmos.color = gizmoArenaColor;
        Gizmos.DrawWireSphere(center, arenaRadius);

        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireSphere(center, arenaRadius - borderWidth);

        // Franja del Meteor Beam hacia el frente del boss
        DrawPolyline(BossAttackTelegraph.Rect(transform.position, transform.forward, 0f, meteorBeamWidth, meteorBeamLength), gizmoBeamColor);

        if (meteorBeamOrigin != null)
        {
            Gizmos.color = gizmoBeamColor;
            Gizmos.DrawSphere(meteorBeamOrigin.position, 0.15f);
        }

        // Area de la lluvia de meteoritos (vista desde el boss)
        if (meteorAreaCenter == MeteorAreaCenter.Boss)
        {
            DrawPolyline(BossAttackTelegraph.Circle(transform.position, meteorAreaRadius), gizmoMeteorColor);
        }

        // Puntas de arma: las estelas salen de aqui
        if (swordTip != null)
        {
            Gizmos.color = gizmoSwordColor;
            Gizmos.DrawSphere(swordTip.position, 0.12f);
        }

        if (tailTip != null)
        {
            Gizmos.color = gizmoTailColor;
            Gizmos.DrawSphere(tailTip.position, 0.12f);
        }

        // Juntas de donde salen los laseres
        if (laserOrigins != null)
        {
            Gizmos.color = gizmoLaserColor;

            foreach (Transform o in laserOrigins)
            {
                if (o != null) Gizmos.DrawWireSphere(o.position, 0.2f);
            }
        }

        // Puntos donde aparecen los golems
        if (summonPoints != null)
        {
            Gizmos.color = gizmoSummonColor;

            foreach (Transform p in summonPoints)
            {
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.5f);
            }
        }
    }
}
