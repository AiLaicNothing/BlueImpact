using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Miniboss_LavaGolem : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float maxChaseDistance = 45f;
    [SerializeField] private float detectionDelay = 0.25f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private float playerSearchInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Flujo Cinético - Melee")]
    [SerializeField] private float flujoCineticoRange = 4f;
    [SerializeField] private Vector3 flujoHitBoxSize = new Vector3(3f, 3f, 3f);
    [SerializeField] private Vector3 flujoHitBoxPos = new Vector3(0f, 1f, 2.5f);
    [SerializeField] private int flujoMeleeDamage = 18;
    [SerializeField] private float flujoImpactRadius = 2.5f; // brazos tumbados contra el suelo: radio alrededor de los pies
    [SerializeField] private GameObject flujoVfx;
    [SerializeField] private float vfxLifetime = 3f;

    [Header("Flujo - Tackle (dash)")]
    [SerializeField] private float tackleSpeed = 12f;
    [SerializeField] private float tackleStopDistance = 1.0f;

    [Header("Duraciones de animación (frames @ fps)")]
    [SerializeField] private int animFps = 24;
    [SerializeField] private int tackleFrames = 30;
    [SerializeField] private int punchFrames = 22;
    [SerializeField] private int fireBallFrames = 30;

    private float TackleDuration => tackleFrames / (float)animFps;
    private float PunchDuration => punchFrames / (float)animFps;
    private float FireBallDuration => fireBallFrames / (float)animFps;

    [Header("Ritmo de combate")]
    [SerializeField] private float flujoCooldown = 0.8f;
    [SerializeField] private float llamaradaCooldown = 1.5f;
    [SerializeField] private float cooldownVariance = 0.3f;
    [SerializeField] private float closeTurnSpeed = 18f;
    [SerializeField] private float meleeChanceWhenClose = 0.85f;

    [Header("Llamarada Densa - Projectile")]
    [SerializeField] private float llamaradaRange = 12f;      // solo dispara dentro de este rango; si está más lejos, se acerca
    [SerializeField] private float llamaradaChargeTime = 2.5f;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int llamaradaDamage = 50;
    [SerializeField] private GameObject chargeVfx;
    [SerializeField] private GameObject castVfx;

    [Header("Patrol")]
    [SerializeField] private float patrolStopDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator golemAnim;
    [SerializeField] private float moveAnimThreshold = 0.1f;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int TackleHash = Animator.StringToHash("Tackle");
    private static readonly int PunchHash = Animator.StringToHash("Punch");
    private static readonly int FireBallHash = Animator.StringToHash("FireBall");
    private bool animIsMoving;

    [Header("Debug")]
    [SerializeField] private GameObject hitBoxPrefab;
    [SerializeField] private bool verboseLogs = false;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool returningHome;
    private bool isPerformingAction;
    private float detectionTimer;
    private float nextPlayerSearchTime;

    private int patrolIndex;
    private int patrolDir = 1;
    private float actionCooldown;

    private float baseAcceleration = 8f;
    private Coroutine actionRoutine;
    private GameObject activeChargeVfx;
    private readonly Collider[] overlapBuffer = new Collider[16];

    protected override void Awake()
    {
        base.Awake();

        if (golemAnim == null)
            golemAnim = GetComponentInChildren<Animator>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = stopDistance;
            baseAcceleration = agent.acceleration;
        }

    }

    protected override void Update()
    {
        base.Update();

        if (isDead || isStunned || IsStaggered)
        {
            CancelAction();
            return;
        }

        if (agent == null || !agent.enabled) return;

        UpdateTarget();
        HandleDetection();
        UpdateAnimation();

        if (isPerformingAction) return;

        HandleActions();
        HandleMovement();
    }

    private void OnDisable()
    {
        isPerformingAction = false;
        actionRoutine = null;
        ClearChargeVfx();
    }

    // ---------- Utilidades ----------

    private void Log(string message)
    {
        if (verboseLogs) Debug.Log("[LavaGolem] " + message);
    }

    private void SetTrigger(int hash)
    {
        if (golemAnim != null) golemAnim.SetTrigger(hash);
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void ResetAgentDefaults()
    {
        if (agent == null || !agent.enabled) return;
        agent.speed = chaseSpeed;
        agent.acceleration = baseAcceleration;
        agent.stoppingDistance = stopDistance;
        StopAgent();
    }

    private void ClearChargeVfx()
    {
        if (activeChargeVfx != null) Destroy(activeChargeVfx);
        activeChargeVfx = null;
    }

    // ---------- Control de acciones ----------

    private void StartAction(IEnumerator routine)
    {
        isPerformingAction = true;
        actionRoutine = StartCoroutine(routine);
    }

    private void FinishAction(float cooldown)
    {
        isPerformingAction = false;
        actionRoutine = null;
        actionCooldown = cooldown;
    }

    // Se llama si el golem muere o es aturdido a mitad de un ataque.
    private void CancelAction()
    {
        if (!isPerformingAction && actionRoutine == null) return;

        if (actionRoutine != null) StopCoroutine(actionRoutine);
        ClearChargeVfx();
        ResetAgentDefaults();

        isPerformingAction = false;
        actionRoutine = null;
        actionCooldown = Mathf.Max(actionCooldown, 0.5f);
    }

    // ---------- Objetivo y detección ----------

    private void UpdateAnimation()
    {
        if (golemAnim == null || agent == null) return;

        bool moving = agent.enabled && !agent.isStopped &&
                      agent.velocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;

        if (moving == animIsMoving) return;

        animIsMoving = moving;
        golemAnim.SetBool(IsMovingHash, moving);
    }

    private void UpdateTarget()
    {
        if (HasValidPlayer()) return;
        if (Time.time < nextPlayerSearchTime) return;

        nextPlayerSearchTime = Time.time + playerSearchInterval;
        player = FindFirstObjectByType<PlayerControl>();
    }

    private bool HasLineOfSight()
    {
        if (!HasValidPlayer()) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;

        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != player.transform && hit.transform.root != player.transform.root)
                return false;
        }

        return true;
    }

    private void HandleDetection()
    {
        Transform home = safeZone != null ? safeZone : spawnPos;
        if (home == null) return;

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            detectionTimer = 0f;
            return;
        }

        float playerDistHome = Vector3.Distance(player.transform.position, home.position);
        float distPlayer = DistanceToPlayer();

        if (!hasDetectedPlayer)
        {
            if (distPlayer <= detectionRange && HasLineOfSight())
            {
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= detectionDelay)
                {
                    hasDetectedPlayer = true;
                    isFollowingPlayer = true;
                    returningHome = false;
                    Log($"Jugador detectado a {distPlayer:F2}m");
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else if (playerDistHome > maxChaseDistance)
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            returningHome = true;
            detectionTimer = 0f;
            Log("Jugador perdido - retornando a spawn");
        }
    }

    // ---------- Movimiento ----------

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);

            if (DistanceToPlayer() <= flujoCineticoRange)
                FacePlayerSmooth();
            else
                RotateToVelocity();
        }
        else if (returningHome && spawnPos != null)
        {
            agent.isStopped = false;
            agent.SetDestination(spawnPos.position);
            RotateToVelocity();

            if (!agent.pathPending && agent.remainingDistance <= patrolStopDistance)
            {
                returningHome = false;
                agent.ResetPath();
            }
        }
        else if (hasPatrol && patrolZones != null && patrolZones.Length > 0)
        {
            agent.isStopped = false;
            HandlePatrol();
            RotateToVelocity();
        }
        else
        {
            returningHome = false;
            agent.ResetPath();
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        if (patrolZones.Length == 1)
        {
            agent.SetDestination(patrolZones[0].position);
            return;
        }

        Transform posDesired = patrolZones[patrolIndex];
        agent.SetDestination(posDesired.position);

        float dist = Vector3.Distance(transform.position, posDesired.position);
        if (dist > patrolStopDistance) return;

        patrolIndex += patrolDir;

        if (patrolIndex >= patrolZones.Length)
        {
            patrolIndex = patrolZones.Length - 2;
            patrolDir = -1;
        }
        else if (patrolIndex < 0)
        {
            patrolIndex = 1;
            patrolDir = 1;
        }
    }

    // ---------- Selección de ataque ----------

    private void HandleActions()
    {
        if (!hasDetectedPlayer || !HasValidPlayer()) return;

        actionCooldown -= Time.deltaTime;
        if (actionCooldown > 0f) return;

        // Sin especial: dos ataques, elegidos por distancia (de cerca casi siempre Flujo; de lejos, mezcla)
        bool close = DistanceToPlayer() <= flujoCineticoRange;
        bool useFlujo = close ? Random.value < meleeChanceWhenClose : Random.value > 0.4f;

        if (useFlujo)
        {
            Log("ATAQUE NORMAL: Flujo Cinético");
            StartAction(PerformFlujo());
        }
        else
        {
            Log("ATAQUE NORMAL: Llamarada Densa");
            StartAction(PerformLlamarada());
        }
    }

    // ---------- Flujo Cinético (melee) ----------

    private IEnumerator PerformFlujo()
    {
        StopAgent();

        if (DistanceToPlayer() > flujoCineticoRange)
        {
            Log("Demasiado lejos, acercándose...");
            yield return ApproachPlayer(flujoCineticoRange, 2f);
        }

        if (!HasValidPlayer())
        {
            FinishAction(0.5f);
            yield break;
        }

        // Tackle: el golem se lanza hacia el jugador mientras dura el clip
        FaceTargetInstant();
        SetTrigger(TackleHash);
        yield return TackleDash();

        // Punch: el daño sale del Animation Event OnFlujoImpact en el frame de impacto
        SetTrigger(PunchHash);
        yield return new WaitForSeconds(PunchDuration);

        FinishAction(flujoCooldown + Random.Range(0f, cooldownVariance));
        Log("Flujo Cinético completado");
    }

    private IEnumerator ApproachPlayer(float range, float timeout)
    {
        agent.isStopped = false;
        float elapsed = 0f;

        while (elapsed < timeout && HasValidPlayer() && DistanceToPlayer() > range)
        {
            agent.SetDestination(player.transform.position);
            RotateToVelocity();
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopAgent();
    }

    private IEnumerator TackleDash()
    {
        agent.isStopped = false;
        agent.speed = tackleSpeed;
        agent.acceleration = 100f; // arranque casi instantáneo
        agent.stoppingDistance = tackleStopDistance;

        float t = 0f;
        while (t < TackleDuration)
        {
            if (HasValidPlayer())
                agent.SetDestination(player.transform.position);

            RotateToVelocity();

            t += Time.deltaTime;
            yield return null;

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= tackleStopDistance)
                break;
        }

        ResetAgentDefaults();

        // Si el dash terminó antes, completa el tiempo del clip mirando al jugador
        float remaining = TackleDuration - t;
        if (remaining > 0f)
            yield return WaitFacing(remaining);
    }

    private IEnumerator WaitFacing(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            FaceTargetInstant();
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Llamado por el Animation Event del clip Punch
    public void OnFlujoImpact()
    {
        // Brazos tumbados contra el suelo: daño en radio alrededor de los pies
        Vector3 center = transform.position;

        if (flujoVfx != null)
            Destroy(Instantiate(flujoVfx, center, Quaternion.identity), vfxLifetime);
        else
            Debug.LogWarning("[LavaGolem] flujoVfx NO asignado");

        int count = Physics.OverlapSphereNonAlloc(center, flujoImpactRadius, overlapBuffer);
        Log($"Impacto: {count} colisiones detectadas");

        // Un jugador puede tener varios colliders: se daña una sola vez por impacto
        var damaged = new System.Collections.Generic.HashSet<IDamageable>();

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (!hit.CompareTag("Player")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damaged.Add(damageable)) continue;

            DamageInfo info = new DamageInfo
            {
                damage = flujoMeleeDamage,
                hitDirection = transform.forward,
            };
            damageable.TakeDamage(in info);
            Log($"IMPACTO! Aplicado {flujoMeleeDamage} daño");
        }
    }

    // ---------- Llamarada Densa (proyectil) ----------

    private IEnumerator PerformLlamarada()
    {
        StopAgent();
        FaceTargetInstant();

        // Si está lejos, se acerca primero: la bola solo sale dentro de llamaradaRange
        if (DistanceToPlayer() > llamaradaRange)
        {
            Log("Demasiado lejos para disparar, acercándose...");
            yield return ApproachPlayer(llamaradaRange, 3f);
        }

        if (!HasValidPlayer())
        {
            FinishAction(0.5f);
            yield break;
        }

        if (chargeVfx != null && firePoint != null)
            activeChargeVfx = Instantiate(chargeVfx, firePoint.position, Quaternion.identity, firePoint);

        // Carga
        float chargeTimer = 0f;
        while (chargeTimer < llamaradaChargeTime)
        {
            if (HasValidPlayer()) FaceTargetInstant();

            chargeTimer += Time.deltaTime;
            yield return null;
        }

        ClearChargeVfx();
        SetTrigger(FireBallHash);

        if (castVfx != null && firePoint != null)
            Destroy(Instantiate(castVfx, firePoint.position, Quaternion.identity), vfxLifetime);

        if (!HasValidPlayer())
        {
            Debug.LogWarning("[LavaGolem] Llamarada cancelada: jugador no válido");
            FinishAction(3f);
            yield break;
        }

        if (fireballPrefab == null || firePoint == null)
        {
            Debug.LogError("[LavaGolem] fireballPrefab o firePoint NO asignados en el Inspector");
            FinishAction(3f);
            yield break;
        }

        Vector3 targetPos = player.transform.position + Vector3.up * 0.5f;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        GameObject proj = Instantiate(fireballPrefab, firePoint.position, Quaternion.LookRotation(dir));

        if (proj.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(llamaradaDamage, dir);
        else
            Debug.LogError("[LavaGolem] El prefab de la fireball no tiene componente E_Projectile");

        yield return new WaitForSeconds(FireBallDuration);

        FinishAction(llamaradaCooldown + Random.Range(0f, cooldownVariance));
        Log("Llamarada Densa completada");
    }

    // ---------- Rotación y utilidades de posición ----------

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void FaceTargetInstant()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private void FacePlayerSmooth()
    {
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * closeTurnSpeed);
    }

    // Centro de la hitbox del Punch en coordenadas del mundo
    private Vector3 GetFlujoHitCenter()
    {
        return transform.TransformPoint(flujoHitBoxPos);
    }

    private float DistanceToPlayer()
    {
        if (!HasValidPlayer()) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.transform.position);
    }

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitBoxPrefab == null || !debug) return;

        GameObject box = Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }

    // ---------- Gizmos ----------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, flujoImpactRadius);
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, flujoCineticoRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, llamaradaRange);

        if (safeZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(safeZone.position, 1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(safeZone.position, maxChaseDistance);
        }
    }
}