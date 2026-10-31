using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_MedusaMorgana : EnemyBase
{
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
    [SerializeField] private string nameNova = "Nova";
    [SerializeField] private string nameClimax = "Climax Serpentinus";
    [SerializeField] private string nameDisonancia = "Disonancia";

    [Header("Recuperacion despues de cada ataque (ventana para pegarle)")]
    [SerializeField] private float recoveryResonancia = 2.5f;
    [SerializeField] private float recoveryPartTitura = 2f;
    [SerializeField] private float recoveryNova = 3.5f;
    [SerializeField] private float recoveryClimax = 3f;
    [SerializeField] private float recoveryDisonancia = 3f;

    [Header("Animator (datos de los clips, 24 fps)")]
    [SerializeField] private float animFps = 24f;
    [SerializeField] private int screamFrames = 40;
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

    [Header("Laseres (Resonancia / Disonancia)")]
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

    [Header("Nova (Fase 2)")]
    [SerializeField] private float novaChannelTime = 5f;       // casteo antes del dano
    [SerializeField] private float novaDamage = 30f;
    [SerializeField] private float novaSafeAngle = 36f;        // 1/10 del circulo, el hueco seguro
    [SerializeField] private bool novaSafeTowardPlayer = false; // false = hueco aleatorio, true = hacia el jugador

    [Header("Climax Serpentinus (Fase 2)")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 15f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float climaxChannelTime = 2f;
    [SerializeField] private float borderWarningTime = 3f;
    [SerializeField] private float climaxDuration = 30f;
    [SerializeField] private float borderTickInterval = 0.5f;
    [SerializeField] private float borderDamagePerSecond = 5f;
    [SerializeField] private GameObject arenaBorderVfx;        // PREFAB
    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private int golemsToSummon = 5;
    [SerializeField] private Transform[] summonPoints;

    [Header("Disonancia (Fase 2)")]
    [SerializeField] private float disonanciaAimTime = 1.5f;
    [SerializeField] private float markDuration = 4f;
    [SerializeField] private float beamDelay = 1f;
    [SerializeField] private float beamRadius = 1.5f;
    [SerializeField] private float disonanciaDamage = 60f;
    [SerializeField] private float disonanciaTrackSpeed = 720f; // laseres que siguen al jugador de forma casi exacta
    [SerializeField] private GameObject markVfx;
    [SerializeField] private GameObject beamWarningVfx;
    [SerializeField] private GameObject beamVfx;

    [Header("Cambio de fase (2 modelos)")]
    [SerializeField] private GameObject phaseOneModel;
    [SerializeField] private GameObject phaseTwoModel;
    [SerializeField] private GameObject transformVfx;          // PREFAB
    [SerializeField] private float swapDelay = 0.2f;
    [SerializeField] private string phaseTwoIntroTrigger = "Scream";
    [SerializeField] private float phaseTwoIntroDuration = 2f;

    [Header("Telegrafia y trail")]
    [SerializeField] private BossAttackTelegraph telegraph;

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

    public event System.Action OnPhaseTwoStarted;

    public bool inCinematic;

    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashIncantation = Animator.StringToHash("Incantation");
    private static readonly int HashAttackSword = Animator.StringToHash("AttackSword");
    private static readonly int HashAttackTail = Animator.StringToHash("AttackTail");
    private static readonly int HashScream = Animator.StringToHash("Scream");
    private static readonly int HashIdleState = Animator.StringToHash("Idle");

    private const int AttackResonancia = 0;
    private const int AttackPartTitura = 1;
    private const int AttackNova = 2;
    private const int AttackClimax = 3;
    private const int AttackDisonancia = 4;

    private bool isAttacking;
    private bool canInterrupt = true;
    private bool inPhaseTwo;
    private bool phaseTwoStarted;
    private float nextAttackTime;

    private bool castWindowOpen;      // true mientras el casteo actual se puede romper con dano
    private bool castBroken;
    private bool inCastBreakStun;
    private float hpAtCastStart;
    private float castWindowStart;
    private float castWindowDuration;
    private string currentAttackName = "";

    private readonly List<LineRenderer> lasers = new List<LineRenderer>();
    private readonly List<Vector3> laserDirs = new List<Vector3>();
    private Vector3 markedPosition;

    private Coroutine attackRoutine;
    private Coroutine swingRoutine;
    private Coroutine markRoutine;
    private GameObject activeMarker;
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

        if (phaseOneModel != null)
        {
            phaseOneModel.SetActive(true);

            Animator phaseOneAnim = phaseOneModel.GetComponentInChildren<Animator>(true);
            if (phaseOneAnim != null) anim = phaseOneAnim;
        }

        nextAttackTime = Time.time + firstAttackDelay;

        WarnPhaseTwoSetup();
    }

    // Avisa en consola lo que va a fallar en fase 2 antes de llegar a ella
    private void WarnPhaseTwoSetup()
    {
        if (phaseTwoModel == null)
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no asignado: la fase 2 sigue con el modelo de fase 1");
            return;
        }

        if (swordTipPhaseTwo == null) Debug.LogWarning("[Morgana] Falta swordTipPhaseTwo: en fase 2 la onda de espada saldra del hueso congelado del modelo 1");
        if (tailTipPhaseTwo == null) Debug.LogWarning("[Morgana] Falta tailTipPhaseTwo: en fase 2 la onda de cola saldra del hueso congelado del modelo 1");
        if (laserOriginsPhaseTwo == null || laserOriginsPhaseTwo.Length == 0) Debug.LogWarning("[Morgana] Falta laserOriginsPhaseTwo: en fase 2 los laseres saldran de las juntas congeladas del modelo 1");

        if (phaseTwoModel.GetComponentInChildren<Animator>(true) == null)
        {
            Debug.LogWarning("[Morgana] phaseTwoModel no tiene Animator: en fase 2 no se van a reproducir animaciones");
        }
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

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

        base.Die();
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
        if (anim != null) anim.Play("Idle");

        int choice = ChooseAttack();

        // Solo Nova y Climax necesitan el centro de la arena. Los demas se lanzan donde este,
        // asi el jugador no se pasa el combate persiguiendo al boss hasta el centro.
        if (choice == AttackNova || choice == AttackClimax)
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
            case AttackNova:
                yield return Nova();
                break;
            case AttackClimax:
                yield return ClimaxSerpentinus();
                break;
            case AttackDisonancia:
                yield return Disonancia();
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

        return Random.Range(0, 5);
    }

    private void EndAttack()
    {
        HideLasers();
        SetMoving(false);
        CloseCastWindow();

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

        if (markRoutine != null)
        {
            StopCoroutine(markRoutine);
            markRoutine = null;
        }

        ResetAnimatorTriggers();
        SetMoving(false);
        HideLasers();
        CloseCastWindow();

        if (anim != null) anim.CrossFadeInFixedTime(HashIdleState, 0.1f);

        if (activeMarker != null) Destroy(activeMarker);

        // Si se interrumpe Climax a medias, el borde de la arena tiene que irse con el
        if (activeBorderVfx != null)
        {
            Destroy(activeBorderVfx);
            activeBorderVfx = null;
        }

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
    }

    private void CloseCastWindow()
    {
        castWindowOpen = false;

        if (vulnerableVfx != null) vulnerableVfx.SetActive(false);
    }

    // El casteo se rompe: el ataque se pierde y el boss queda abierto varios segundos
    private void BreakCast()
    {
        castBroken = true;

        Debug.Log("[Morgana] Casteo roto por dano, el boss queda abierto " + castBreakStunDuration + "s");

        CancelAttack();
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

        if (anim != null && !string.IsNullOrEmpty(castBreakTrigger))
        {
            anim.SetTrigger(Animator.StringToHash(castBreakTrigger));
        }

        yield return new WaitForSeconds(castBreakStunDuration);

        inCastBreakStun = false;
        if (brokenVfx != null) brokenVfx.SetActive(false);

        if (isDead) yield break;

        isAttacking = false;
        nextAttackTime = Time.time + pauseBetweenAttacks;
    }

    // Ventana visible al terminar un ataque: el boss se queda quieto en Idle
    private IEnumerator Recover(float duration)
    {
        SetMoving(false);
        HideLasers();

        if (anim != null) anim.CrossFadeInFixedTime(HashIdleState, 0.15f);

        yield return new WaitForSeconds(duration);
    }

    // ---------- INTRO Y CAMBIO DE FASE ----------

    // Lo llama BossEvent durante la introduccion
    public IEnumerator PlayIntro()
    {
        SetMoving(false);
        Trigger(HashScream);

        yield return new WaitForSeconds(FramesToSeconds(screamFrames));
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

    // Grito de la fase 1, VFX que tapa el cambio, cambio de modelo y entrada de la fase 2
    public IEnumerator PlayPhaseTwoTransition()
    {
        FaceTargetInstant();
        SetMoving(false);

        Trigger(HashScream);
        yield return new WaitForSeconds(FramesToSeconds(screamFrames));
        if (isDead) yield break;

        if (transformVfx != null)
        {
            GameObject vfx = Instantiate(transformVfx, transform.position, transform.rotation);
            Destroy(vfx, 3f);
        }

        yield return new WaitForSeconds(swapDelay);
        if (isDead) yield break;

        SwapToPhaseTwoModel();

        yield return null;
        if (isDead) yield break;

        if (anim != null && !string.IsNullOrEmpty(phaseTwoIntroTrigger))
        {
            anim.SetTrigger(Animator.StringToHash(phaseTwoIntroTrigger));
        }

        yield return new WaitForSeconds(phaseTwoIntroDuration);
        if (isDead) yield break;

        inPhaseTwo = true;
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

    // Vuelve al centro de la arena antes de cada ataque. Durante el ataque no se mueve.
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

        Trigger(HashIncantation);

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
                }
            }

            initialized = true;
            t += dt;
            yield return null;
        }

        HideLasers();
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
        Trigger(isSword ? HashAttackSword : HashAttackTail);

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

    // ---------- NOVA (FASE 2) ----------

    // Casteo de 5 segundos desde el centro. Luego dano en toda la arena excepto el hueco seguro.
    private IEnumerator Nova()
    {
        canInterrupt = true;
        currentAttackName = nameNova;
        Debug.Log("[Morgana] Nova");

        Vector3 center = ArenaCenterPos();
        center.y = transform.position.y;

        Vector3 safeDir = (novaSafeTowardPlayer && HasValidPlayer()) ? FlatDirToPlayer() : RandomFlatDirection();
        float half = novaSafeAngle * 0.5f;

        FaceTargetInstant();
        Trigger(HashIncantation);

        // Se dibuja la zona que hace dano: el circulo con el hueco seguro abierto
        if (telegraph != null)
        {
            telegraph.StartTelegraph(
                () => BossAttackTelegraph.Pie(center, safeDir, arenaRadius, half, 360f - novaSafeAngle),
                novaChannelTime);
        }

        // Los 5 segundos de canalizacion son la mejor oportunidad del jugador:
        // si le mete castBreakDamage aqui, la Nova entera se cancela
        OpenCastWindow(novaChannelTime);
        yield return new WaitForSeconds(novaChannelTime);
        CloseCastWindow();

        if (telegraph != null) telegraph.OnHit();

        if (HasValidPlayer())
        {
            Vector3 playerPos = player.transform.position;

            bool inArena = FlatDistance(playerPos, center) <= arenaRadius;
            bool inSafe = IsInsideWedge(playerPos, center, safeDir, half);

            if (inArena && !inSafe)
            {
                Vector3 hitDir = playerPos - center;
                hitDir.y = 0f;
                DamagePlayer(novaDamage, hitDir.normalized);
            }
        }

        yield return Recover(recoveryNova);
    }

    // ---------- CLIMAX SERPENTINUS (FASE 2) ----------

    private IEnumerator ClimaxSerpentinus()
    {
        // Interrumpible: si el jugador llena la barra de stagger, el ataque se corta.
        // Si no fuera asi, la barra subiria durante 30 segundos sin que pase nada.
        canInterrupt = true;
        currentAttackName = nameClimax;
        Debug.Log("[Morgana] Climax Serpentinus");

        FaceTargetInstant();
        Trigger(HashIncantation);

        // Durante la canalizacion el jugador puede negar el ataque entero
        OpenCastWindow(climaxChannelTime);
        yield return new WaitForSeconds(climaxChannelTime);
        CloseCastWindow();

        Trigger(HashScream);

        if (arenaBorderVfx != null)
        {
            activeBorderVfx = Instantiate(arenaBorderVfx, ArenaCenterPos(), Quaternion.identity);
        }

        yield return new WaitForSeconds(borderWarningTime);

        SummonGolems();

        float remaining = Mathf.Max(0f, climaxDuration - climaxChannelTime - borderWarningTime);
        yield return BorderDamageOverTime(remaining);

        if (activeBorderVfx != null)
        {
            Destroy(activeBorderVfx);
            activeBorderVfx = null;
        }

        yield return Recover(recoveryClimax);
    }

    private IEnumerator BorderDamageOverTime(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (HasValidPlayer() && IsInsideBorder(player.transform.position))
            {
                DamagePlayer(borderDamagePerSecond * borderTickInterval, Vector3.zero);
            }

            yield return new WaitForSeconds(borderTickInterval);
            timer += borderTickInterval;
        }
    }

    private bool IsInsideBorder(Vector3 pos)
    {
        Vector3 center = ArenaCenterPos();

        return FlatDistance(pos, center) >= arenaRadius - borderWidth;
    }

    private void SummonGolems()
    {
        if (golemPrefab == null)
        {
            Debug.LogWarning("[Morgana] golemPrefab no asignado");
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

    // ---------- DISONANCIA (FASE 2) ----------

    private IEnumerator Disonancia()
    {
        // Interrumpible, igual que Climax: la barra de stagger tiene que servir para algo
        canInterrupt = true;
        currentAttackName = nameDisonancia;
        Debug.Log("[Morgana] Disonancia");

        Trigger(HashIncantation);

        // Apunta con laseres finos hacia el jugador. Se puede romper durante el apuntado.
        OpenCastWindow(disonanciaAimTime);
        yield return LaserRoutine(disonanciaAimTime, false, LaserCount, disonanciaTrackSpeed);
        CloseCastWindow();

        if (!HasValidPlayer())
        {
            HideLasers();
            yield break;
        }

        if (markVfx != null)
        {
            activeMarker = Instantiate(markVfx, player.transform.position, Quaternion.identity);
        }

        markedPosition = player.transform.position;

        // El marcador y los laseres siguen al jugador durante la marca
        markRoutine = StartCoroutine(MarkFollowRoutine(markDuration));
        yield return LaserRoutine(markDuration, true, LaserCount, disonanciaTrackSpeed);

        if (markRoutine != null)
        {
            StopCoroutine(markRoutine);
            markRoutine = null;
        }

        if (activeMarker != null) Destroy(activeMarker);

        // El haz cae donde estaba el jugador al terminar la marca
        Vector3 beamPos = markedPosition;

        if (telegraph != null)
        {
            telegraph.StartTelegraph(() => BossAttackTelegraph.Circle(beamPos, beamRadius), beamDelay);
        }

        if (beamWarningVfx != null)
        {
            GameObject warning = Instantiate(beamWarningVfx, beamPos, Quaternion.identity);
            Destroy(warning, beamDelay);
        }

        yield return new WaitForSeconds(beamDelay);

        if (beamVfx != null)
        {
            GameObject beam = Instantiate(beamVfx, beamPos, Quaternion.identity);
            Destroy(beam, 0.5f);
        }

        if (telegraph != null) telegraph.OnHit();

        DamagePlayerWithin(beamPos, beamRadius, disonanciaDamage, Vector3.up);

        yield return Recover(recoveryDisonancia);
    }

    private IEnumerator MarkFollowRoutine(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            if (HasValidPlayer())
            {
                markedPosition = player.transform.position;
            }

            if (activeMarker != null)
            {
                activeMarker.transform.position = markedPosition + Vector3.up * 0.1f;
            }

            t += Time.deltaTime;
            yield return null;
        }
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

    private void DamagePlayerWithin(Vector3 center, float radius, float amount, Vector3 hitDir)
    {
        if (!HasValidPlayer()) return;

        if (FlatDistance(player.transform.position, center) <= radius)
        {
            DamagePlayer(amount, hitDir);
        }
    }

    // ---------- ANIMATOR ----------

    private void SetMoving(bool value)
    {
        if (anim != null) anim.SetBool(HashIsMoving, value);
    }

    private void Trigger(int hash)
    {
        if (anim != null) anim.SetTrigger(hash);
    }

    private void ResetAnimatorTriggers()
    {
        if (anim == null) return;

        anim.ResetTrigger(HashIncantation);
        anim.ResetTrigger(HashAttackSword);
        anim.ResetTrigger(HashAttackTail);
        anim.ResetTrigger(HashScream);
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

    // Verdadero si el punto esta dentro del sector de 'halfAngle' grados a cada lado de wedgeDir
    private static bool IsInsideWedge(Vector3 p, Vector3 center, Vector3 wedgeDir, float halfAngle)
    {
        Vector3 flat = p - center;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.0001f) return true;

        wedgeDir.y = 0f;
        return Vector3.Angle(wedgeDir, flat) <= halfAngle;
    }

    private static Vector3 RandomFlatDirection()
    {
        Vector2 r = Random.insideUnitCircle;
        if (r.sqrMagnitude < 0.0001f) r = Vector2.up;

        Vector2 n = r.normalized;
        return new Vector3(n.x, 0f, n.y);
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

    // BossEvent lo llama justo despues de instanciar el boss
    public void SetArena(Transform center, Transform[] points, EnemySpawner spawner)
    {
        if (center != null) arenaCenter = center;
        if (points != null && points.Length > 0) summonPoints = points;
        if (spawner != null) summonSpawner = spawner;
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

        // Hueco seguro de Nova (vista de referencia, direccion frontal)
        float half = novaSafeAngle * 0.5f;
        DrawPolyline(BossAttackTelegraph.Pie(center, transform.forward, arenaRadius, half, 360f - novaSafeAngle), gizmoArenaColor);

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