using System.Collections;
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

    [Header("Animator (datos de los clips)")]
    [SerializeField] private float animFps = 30f;
    [SerializeField] private int screamFrames = 40;
    [SerializeField] private int swordHitFrame = 12;   // frame del clip Attack Sword donde cae el golpe
    [SerializeField] private int tailHitFrame = 20;    // frame del clip Attack Cola donde cae el golpe

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 1.5f;   // m/s: distancia que recorre un ciclo de Walk / 1.25 s
    [SerializeField] private float turnSpeed = 8f;

    [Header("Resonancia (Fase 1)")]
    [SerializeField] private float resonanciaEngageDistance = 4f;
    [SerializeField] private float resonanciaWindUp = 2f;        // Z: igual a Incantation (60 frames / fps)
    [SerializeField] private float resonanciaRecovery = 0.5f;
    [SerializeField] private int resonanciaProjectiles = 6;      // X: proyectiles del area
    [SerializeField] private float resonanciaRangeOffset = 2f;   // 2U de rango melee
    [SerializeField] private float resonanciaRectWidth = 6f;
    [SerializeField] private float resonanciaRectDepth = 8f;
    [SerializeField] private float resonanciaShotHeight = 1f;
    [SerializeField] private float resonanciaDamage = 10f;       // DMG x proyectil
    [SerializeField] private GameObject projectilePrefab;

    [Header("Part-Titura (Fase 1)")]
    [SerializeField] private float partTituraDuration = 12f;
    [SerializeField] private float partTituraMoveSpeed = 1.2f;  // m/s mientras gira
    [SerializeField] private float partTituraStopDistance = 1.5f;
    [SerializeField] private float partTituraHitInterval = 1.5f; // Y: segundos entre ataques (minimo ~1.5 por el clip de cola)
    [SerializeField] private float swordHitRadius = 3f;
    [SerializeField] private float tailHitRadius = 3.5f;
    [SerializeField] private float swordDamage = 12f;            // DMG x hit
    [SerializeField] private float tailDamage = 12f;             // DMG x hit

    [Header("Climax Serpentinus (Fase 2)")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 15f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float climaxChannelTime = 2f;
    [SerializeField] private float borderWarningTime = 3f;       // X: segundos iluminado antes del dano
    [SerializeField] private float climaxDuration = 30f;         // duracion total de la habilidad
    [SerializeField] private float borderTickInterval = 0.5f;
    [SerializeField] private float borderDamagePerSecond = 5f;   // DMG x seg
    [SerializeField] private GameObject arenaBorderVfx;          // PREFAB: se instancia en el centro de la arena
    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private int golemsToSummon = 5;             // Y: enemigos a invocar
    [SerializeField] private Transform[] summonPoints;

    [Header("Disonancia (Fase 2)")]
    [SerializeField] private float disonanciaAimTime = 1.5f;
    [SerializeField] private float markDuration = 4f;
    [SerializeField] private float beamDelay = 1f;
    [SerializeField] private float beamRadius = 1.5f;
    [SerializeField] private float disonanciaDamage = 60f;       // DMG
    [SerializeField] private GameObject markVfx;
    [SerializeField] private GameObject beamWarningVfx;
    [SerializeField] private GameObject beamVfx;

    [Header("Cambio de fase (2 modelos)")]
    [SerializeField] private GameObject phaseOneModel;                 // hijo con el modelo y Animator de la fase 1
    [SerializeField] private GameObject phaseTwoModel;                 // hijo con el modelo y Animator de la fase 2
    [SerializeField] private GameObject transformVfx;                  // PREFAB: humo o destello que tapa el cambio de modelo
    [SerializeField] private float swapDelay = 0.2f;                   // espera tras lanzar el VFX, antes de cambiar el modelo
    [SerializeField] private string phaseTwoIntroTrigger = "Scream";   // trigger del Animator de la fase 2 para su entrada
    [SerializeField] private float phaseTwoIntroDuration = 2f;         // duracion del clip de entrada de la fase 2

    // BossEvent se suscribe para manejar camara y modo cinematico
    public event System.Action OnPhaseTwoStarted;

    public bool inCinematic;

    // Parametros del Animator
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashIncantation = Animator.StringToHash("Incantation");
    private static readonly int HashAttackSword = Animator.StringToHash("AttackSword");
    private static readonly int HashAttackTail = Animator.StringToHash("AttackTail");
    private static readonly int HashScream = Animator.StringToHash("Scream");
    private static readonly int HashIdleState = Animator.StringToHash("Idle");

    private bool isAttacking;
    private bool canInterrupt = true;
    private EnemySpawner summonSpawner;
    private bool inPhaseTwo;
    private bool phaseTwoStarted;
    private float nextAttackTime;

    private Coroutine attackRoutine;
    private GameObject activeMarker;
    private GameObject activeBorderVfx;

    // Datos que lee la barra de vida en pantalla
    public string DisplayName => bossDisplayName;
    public float MaxHp => stats != null ? stats.maxHp : 1f;
    public bool HasStagger => stats != null && stats.hasStagger;
    public float StaggerBuild => currentStaggerBuild;
    public float StaggerThreshold => stats != null ? stats.staggerThreshold : 1f;
    public bool InPhaseTwo => inPhaseTwo;

    protected override void Start()
    {
        base.Start();

        if (arenaCenter == null) arenaCenter = transform;

        // Arranca con el modelo de la fase 1
        if (phaseTwoModel != null) phaseTwoModel.SetActive(false);

        if (phaseOneModel != null)
        {
            phaseOneModel.SetActive(true);

            Animator phaseOneAnim = phaseOneModel.GetComponentInChildren<Animator>(true);
            if (phaseOneAnim != null) anim = phaseOneAnim;
        }

        nextAttackTime = Time.time + firstAttackDelay;
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

        // El stagger corta el ataque en curso (salvo las habilidades que no se pueden interrumpir)
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

        if (activeBorderVfx != null) Destroy(activeBorderVfx);

        base.Die();
    }

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

        if (!inPhaseTwo)
        {
            if (Random.value < 0.5f)
                yield return Resonancia();
            else
                yield return PartTitura();
        }
        else
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    yield return Resonancia();
                    break;
                case 1:
                    yield return PartTitura();
                    break;
                case 2:
                    yield return ClimaxSerpentinus();
                    break;
                case 3:
                    yield return Disonancia();
                    break;
            }
        }

        EndAttack();
    }

    private void EndAttack()
    {
        SetMoving(false);

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

        ResetAnimatorTriggers();
        SetMoving(false);

        if (anim != null) anim.CrossFadeInFixedTime(HashIdleState, 0.1f);

        if (activeMarker != null) Destroy(activeMarker);

        isAttacking = false;
        nextAttackTime = Time.time + pauseBetweenAttacks;
    }

    // ---------- INTRO Y TRANSICION ----------

    // Lo llama BossEvent durante la introduccion
    public IEnumerator PlayIntro()
    {
        SetMoving(false);
        Trigger(HashScream);

        yield return new WaitForSeconds(FramesToSeconds(screamFrames));
    }

    // Se llama una sola vez, en cuanto la vida baja del umbral
    private void StartPhaseTwo()
    {
        phaseTwoStarted = true;

        // Frena la IA y corta el ataque en curso
        inCinematic = true;
        CancelAttack();

        Debug.Log("[Morgana] Vida bajo el umbral, inicia el cambio de fase");

        if (OnPhaseTwoStarted != null)
        {
            // BossEvent se encarga de la camara y del modo cinematico
            OnPhaseTwoStarted.Invoke();
        }
        else
        {
            // Sin BossEvent hace el cambio por su cuenta, sin camara
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

        // 1. Grito con el modelo de la fase 1
        Trigger(HashScream);
        yield return new WaitForSeconds(FramesToSeconds(screamFrames));
        if (isDead) yield break;

        // 2. El VFX tapa el cambio de modelo
        if (transformVfx != null)
        {
            GameObject vfx = Instantiate(transformVfx, transform.position, transform.rotation);
            Destroy(vfx, 3f);
        }

        yield return new WaitForSeconds(swapDelay);
        if (isDead) yield break;

        SwapToPhaseTwoModel();

        // Un frame para que el nuevo Animator se inicialice antes de recibir el trigger
        yield return null;
        if (isDead) yield break;

        // 3. Entrada del modelo de la fase 2
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

    // ---------- FASE 1 ----------

    private IEnumerator Resonancia()
    {
        canInterrupt = true;
        Debug.Log("[Morgana] Resonancia");

        yield return ApproachPlayer(resonanciaEngageDistance, 3f);

        SetMoving(false);
        Trigger(HashIncantation);

        float timer = 0f;
        while (timer < resonanciaWindUp)
        {
            FaceTarget(turnSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        if (!HasValidPlayer()) yield break;

        ResonanciaBurst(FlatDirToPlayer());

        yield return new WaitForSeconds(resonanciaRecovery);
    }

    private void ResonanciaBurst(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 origin = transform.position + forward * resonanciaRangeOffset + Vector3.up * resonanciaShotHeight;

        int count = Mathf.Max(1, resonanciaProjectiles);
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);
        int spawned = 0;

        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float u = cols == 1 ? 0.5f : (float)c / (cols - 1);
                float v = rows == 1 ? 0.5f : (float)r / (rows - 1);

                Vector3 pos = origin
                    + right * ((u - 0.5f) * resonanciaRectWidth)
                    + forward * (v * resonanciaRectDepth);

                FireProjectile(pos, forward, resonanciaDamage);
                spawned++;
            }
        }
    }

    private IEnumerator PartTitura()
    {
        canInterrupt = true;
        Debug.Log("[Morgana] Part-Titura");

        SetMoving(true);

        float timer = 0f;
        float nextAttackAt = 0f;
        float pendingHitAt = -1f;
        bool nextIsSword = true;
        bool pendingIsSword = true;

        while (timer < partTituraDuration)
        {
            if (!HasValidPlayer()) break;

            // El giro lo hace la animacion, aqui solo avanza hacia el jugador
            if (DistanceToPlayerFlat() > partTituraStopDistance)
            {
                transform.position += FlatDirToPlayer() * partTituraMoveSpeed * Time.deltaTime;
            }

            // Cada Y segundos alterna espada y cola
            if (timer >= nextAttackAt)
            {
                pendingIsSword = nextIsSword;
                nextIsSword = !nextIsSword;

                Trigger(pendingIsSword ? HashAttackSword : HashAttackTail);

                pendingHitAt = timer + FramesToSeconds(pendingIsSword ? swordHitFrame : tailHitFrame);
                nextAttackAt = timer + partTituraHitInterval;
            }

            // El dano cae en el frame del golpe, en 360 grados alrededor del boss
            if (pendingHitAt >= 0f && timer >= pendingHitAt)
            {
                pendingHitAt = -1f;

                float radius = pendingIsSword ? swordHitRadius : tailHitRadius;
                float damage = pendingIsSword ? swordDamage : tailDamage;

                DamagePlayerWithin(transform.position, radius, damage, FlatDirToPlayer());
            }

            timer += Time.deltaTime;
            yield return null;
        }

        SetMoving(false);
        FaceTargetInstant();
    }

    // ---------- FASE 2 (animaciones provisionales: Incantation y Scream) ----------

    private IEnumerator ClimaxSerpentinus()
    {
        canInterrupt = false;
        Debug.Log("[Morgana] Climax Serpentinus");

        FaceTargetInstant();
        Trigger(HashIncantation);

        yield return new WaitForSeconds(climaxChannelTime);

        Trigger(HashScream);

        if (arenaBorderVfx != null)
        {
            activeBorderVfx = Instantiate(arenaBorderVfx, ArenaCenterPos(), Quaternion.identity);
        }

        // El borde se ilumina como aviso antes de que haga dano
        yield return new WaitForSeconds(borderWarningTime);

        SummonGolems();

        float remaining = Mathf.Max(0f, climaxDuration - climaxChannelTime - borderWarningTime);
        yield return BorderDamageOverTime(remaining);

        if (activeBorderVfx != null) Destroy(activeBorderVfx);
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
        Vector2 p = new Vector2(pos.x, pos.z);
        Vector2 c = new Vector2(center.x, center.z);

        return Vector2.Distance(p, c) >= arenaRadius - borderWidth;
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

    private IEnumerator Disonancia()
    {
        canInterrupt = false;
        Debug.Log("[Morgana] Disonancia");

        Trigger(HashIncantation);

        float aimTimer = 0f;
        while (aimTimer < disonanciaAimTime)
        {
            FaceTarget(turnSpeed * 2f);
            aimTimer += Time.deltaTime;
            yield return null;
        }

        if (!HasValidPlayer()) yield break;

        if (markVfx != null)
        {
            activeMarker = Instantiate(markVfx, player.transform.position, Quaternion.identity);
        }

        Vector3 lastPos = player.transform.position;
        float markTimer = 0f;

        // El marcador sigue al jugador durante markDuration
        while (markTimer < markDuration)
        {
            if (HasValidPlayer())
            {
                lastPos = player.transform.position;
            }

            if (activeMarker != null)
            {
                activeMarker.transform.position = lastPos + Vector3.up * 0.1f;
            }

            markTimer += Time.deltaTime;
            yield return null;
        }

        if (activeMarker != null) Destroy(activeMarker);

        // El haz cae donde estaba el jugador al terminar la marca, no donde esta ahora
        Vector3 beamPos = lastPos;

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

        DamagePlayerWithin(beamPos, beamRadius, disonanciaDamage, Vector3.up);
    }

    // ---------- MOVIMIENTO ----------

    private IEnumerator ApproachPlayer(float desiredDistance, float maxTime)
    {
        float t = 0f;

        while (HasValidPlayer() && DistanceToPlayerFlat() > desiredDistance && t < maxTime)
        {
            SetMoving(true);

            transform.position += FlatDirToPlayer() * walkSpeed * Time.deltaTime;
            FaceTarget(turnSpeed);

            t += Time.deltaTime;
            yield return null;
        }

        SetMoving(false);
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

    // ---------- DANO Y PROYECTILES ----------

    private void FireProjectile(Vector3 pos, Vector3 dir, float damage)
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
        E_Projectile projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            projectile.InitProj(damage, dir);
        }
    }

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

    // Aplica dano una sola vez si el jugador esta dentro del radio dado
    private void DamagePlayerWithin(Vector3 center, float radius, float amount, Vector3 hitDir)
    {
        if (!HasValidPlayer()) return;

        Vector2 p = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 c = new Vector2(center.x, center.z);

        if (Vector2.Distance(p, c) <= radius)
        {
            DamagePlayer(amount, hitDir);
        }
    }

    // ---------- UTILIDADES ----------

    private Vector3 ArenaCenterPos()
    {
        return arenaCenter != null ? arenaCenter.position : transform.position;
    }

    private Vector3 FlatDirToPlayer()
    {
        if (!HasValidPlayer()) return transform.forward;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    private float DistanceToPlayerFlat()
    {
        if (!HasValidPlayer()) return Mathf.Infinity;

        Vector3 a = transform.position;
        Vector3 b = player.transform.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void FaceTarget(float speed)
    {
        if (!HasValidPlayer()) return;

        Quaternion targetRot = Quaternion.LookRotation(FlatDirToPlayer());
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
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

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;

        Vector3 center = ArenaCenterPos();

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, arenaRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, arenaRadius - borderWidth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, swordHitRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, tailHitRadius);
    }
}