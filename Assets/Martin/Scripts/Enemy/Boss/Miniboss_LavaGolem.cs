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

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Flujo Cinético - Melee")]
    [SerializeField] private float flujoCineticoRange = 4f;
    [SerializeField] private Vector3 flujoHitBoxSize = new Vector3(3f, 3f, 3f);
    [SerializeField] private Vector3 flujoHitBoxPos = new Vector3(0f, 1f, 2.5f);
    [SerializeField] private int flujoMeleeDamage = 18;
    [SerializeField] private GameObject flujoVfx;

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
    [SerializeField] private float llamaradaRange = 10f;
    [SerializeField] private float llamaradaChargeTime = 5f;
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
    [SerializeField] private float animDampTime = 0.1f;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int TackleHash = Animator.StringToHash("Tackle");
    private static readonly int PunchHash = Animator.StringToHash("Punch");
    private static readonly int FireBallHash = Animator.StringToHash("FireBall");
    private bool animIsMoving;

    [Header("Debug")]
    [SerializeField] private GameObject hitBoxPrefab;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool returningHome;
    private bool isPerformingAction;
    private float detectionTimer;

    private int patrolIndex;
    private int patrolDir = 1;
    private float actionCooldown;
    private int attackCount;
    private int nextSpecialThreshold;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.25f;
        }

        if (golemAnim == null)
            golemAnim = GetComponentInChildren<Animator>();

        nextSpecialThreshold = Random.Range(2, 4);
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (isStunned || IsStaggered) return;
        if (agent == null || !agent.enabled) return;

        UpdateTarget();
        HandleDetection();

        UpdateAnimation();

        if (isPerformingAction) return;

        HandleActions();
        HandleMovement();
    }

    private void UpdateAnimation()
    {
        if (golemAnim == null) return;

        bool moving = agent.enabled && !agent.isStopped &&
                      agent.velocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;

        if (moving == animIsMoving) return;

        animIsMoving = moving;
        golemAnim.SetBool(IsMovingHash, moving);
    }

    private void UpdateTarget()
    {
        if (!HasValidPlayer())
        {
            player = FindFirstObjectByType<PlayerControl>();
        }
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
            {
                return false;
            }
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
                    Debug.Log($"[LavaGolem] Jugador detectado a {distPlayer:F2}m");
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else
        {
            if (playerDistHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                returningHome = true;
                detectionTimer = 0f;
                Debug.Log("[LavaGolem] Jugador perdido - retornando a spawn");
            }
        }
    }

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = DistanceToPlayer();

            agent.isStopped = false;
            agent.SetDestination(player.transform.position);

            if (distance <= flujoCineticoRange)
                FacePlayerSmooth();
            else
                RotateToVelocity();
        }
        else if (returningHome)
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
        else if (hasPatrol)
        {
            agent.isStopped = false;
            HandlePatrol();
            RotateToVelocity();
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        Transform posDesired = patrolZones[patrolIndex];
        agent.SetDestination(posDesired.position);

        float dist = Vector3.Distance(transform.position, posDesired.position);

        if (dist <= patrolStopDistance)
        {
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
    }

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer()) return;

        actionCooldown -= Time.deltaTime;

        if (actionCooldown <= 0f)
        {
            Debug.Log($"[LavaGolem] Decidiendo ataque - Contador: {attackCount}/{nextSpecialThreshold}");

            if (attackCount >= nextSpecialThreshold)
            {
                attackCount = 0;
                nextSpecialThreshold = Random.Range(2, 4);
                Debug.Log("[LavaGolem] ATAQUE ESPECIAL: Llamarada Densa");
                StartCoroutine(PerformLlamarada());
            }
            else
            {
                bool close = DistanceToPlayer() <= flujoCineticoRange;
                bool useFlujo = close ? Random.value < meleeChanceWhenClose : Random.value > 0.4f;

                if (useFlujo)
                {
                    Debug.Log("[LavaGolem] ATAQUE NORMAL: Flujo Cinético");
                    StartCoroutine(PerformFlujo());
                }
                else
                {
                    Debug.Log("[LavaGolem] ATAQUE NORMAL: Llamarada Densa");
                    StartCoroutine(PerformLlamarada());
                }

                attackCount++;
            }
        }
    }

    private IEnumerator PerformFlujo()
    {
        Debug.Log("[Flujo] Iniciando Flujo Cinético");
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float distance = DistanceToPlayer();
        Debug.Log($"[Flujo] Distancia al jugador: {distance:F2}m (rango necesario: {flujoCineticoRange}m)");

        // Si está muy lejos, acercarse primero
        if (distance > flujoCineticoRange)
        {
            Debug.Log("[Flujo] Demasiado lejos, acercándose...");
            if (agent != null)
            {
                agent.isStopped = false;
            }

            float timeout = 2f;
            float elapsed = 0f;

            while (distance > flujoCineticoRange && elapsed < timeout)
            {
                if (HasValidPlayer())
                {
                    agent.SetDestination(player.transform.position);
                    RotateToVelocity();
                    distance = DistanceToPlayer();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            Debug.Log($"[Flujo] En rango - Distancia final: {distance:F2}m");
        }

        // Tackle: el boss se lanza hacia el jugador mientras dura el clip
        Debug.Log($"[Flujo] Tackle ({TackleDuration:F3}s)");
        FaceTargetInstant();
        golemAnim.SetTrigger(TackleHash);
        yield return TackleDash();

        // Punch: el daño sale del Animation Event OnFlujoImpact en el frame de impacto
        Debug.Log($"[Flujo] Punch ({PunchDuration:F3}s)");
        golemAnim.SetTrigger(PunchHash);
        yield return new WaitForSeconds(PunchDuration);

        isPerformingAction = false;
        actionCooldown = flujoCooldown + Random.Range(0f, cooldownVariance);
        Debug.Log("[Flujo] Flujo Cinético completado");
    }

    private IEnumerator TackleDash()
    {
        float prevSpeed = agent.speed;
        float prevAccel = agent.acceleration;
        float prevStop = agent.stoppingDistance;

        agent.isStopped = false;
        agent.speed = tackleSpeed;
        agent.acceleration = 100f;          // arranque casi instantáneo
        agent.stoppingDistance = tackleStopDistance;

        float t = 0f;
        while (t < TackleDuration)
        {
            if (HasValidPlayer())
                agent.SetDestination(player.transform.position);

            RotateToVelocity();

            t += Time.deltaTime;
            yield return null;

            // Si ya llegó al jugador, detiene el dash
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= tackleStopDistance)
                break;
        }

        agent.ResetPath();
        agent.isStopped = true;
        agent.speed = prevSpeed;
        agent.acceleration = prevAccel;
        agent.stoppingDistance = prevStop;

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
        Vector3 center = GetFlujoHitCenter();

        if (flujoVfx != null)
            Instantiate(flujoVfx, center, Quaternion.identity);
        else
            Debug.LogWarning("[Flujo] flujoVfx NO asignado");

        Collider[] hits = Physics.OverlapBox(center, flujoHitBoxSize * 0.5f, transform.rotation);
        Debug.Log($"[Flujo] Hitbox check: {hits.Length} colisiones detectadas");

        ShowHitbox(center, flujoHitBoxSize, transform.rotation);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            DamageInfo info = new DamageInfo
            {
                damage = flujoMeleeDamage,
                hitDirection = transform.forward,
            };
            damageable.TakeDamage(in info);
            Debug.Log($"[Flujo] ¡IMPACTO! Aplicado {flujoMeleeDamage} daño");
        }
    }

    private IEnumerator PerformLlamarada()
    {
        Debug.Log("[Llamarada] Iniciando Llamarada Densa");
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

        // VFX de carga
        GameObject chargeEffect = null;
        if (chargeVfx != null && firePoint != null)
        {
            Debug.Log("[Llamarada] Instanciando VFX de carga");
            chargeEffect = Instantiate(chargeVfx, firePoint.position, Quaternion.identity);
            chargeEffect.transform.SetParent(firePoint);
        }
        else
        {
            Debug.LogWarning("[Llamarada] chargeVfx o firePoint NO asignados");
        }

        // Tiempo de carga
        Debug.Log($"[Llamarada] Cargando durante {llamaradaChargeTime}s...");
        float chargeTimer = 0f;
        while (chargeTimer < llamaradaChargeTime)
        {
            if (HasValidPlayer())
            {
                FaceTargetInstant();
            }

            chargeTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Llamarada] Carga completada!");

        // Disparar
        Debug.Log("[Llamarada] Reproduciendo animación 'Llamarada_Cast'");
        golemAnim.SetTrigger(FireBallHash);

        if (chargeEffect != null)
        {
            Destroy(chargeEffect);
        }

        if (castVfx != null && firePoint != null)
        {
            Debug.Log("[Llamarada] Instanciando VFX de disparo");
            Instantiate(castVfx, firePoint.position, Quaternion.identity);
        }

        if (!HasValidPlayer())
        {
            Debug.LogError("[Llamarada] Jugador NO válido!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        if (fireballPrefab == null)
        {
            Debug.LogError("[Llamarada] fireballPrefab NO asignado en Inspector!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        if (firePoint == null)
        {
            Debug.LogError("[Llamarada] firePoint NO asignado en Inspector!");
            isPerformingAction = false;
            actionCooldown = 3f;
            yield break;
        }

        Debug.Log("[Llamarada] Creando proyectil...");
        Vector3 targetPos = player.transform.position + Vector3.up * 0.5f;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        Debug.Log($"[Llamarada] Desde: {firePoint.position}, Hacia: {targetPos}, Dirección: {dir}");

        GameObject proj = Instantiate(fireballPrefab, firePoint.position, Quaternion.LookRotation(dir));
        Debug.Log($"[Llamarada] Proyectil instanciado: {proj.name}");

        var projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            Debug.Log($"[Llamarada] Inicializando proyectil con daño {llamaradaDamage}");
            projectile.InitProj(llamaradaDamage, dir);
        }
        else
        {
            Debug.LogError("[Llamarada] El prefab NO tiene componente E_Projectile!");
        }

        yield return new WaitForSeconds(FireBallDuration);

        isPerformingAction = false;
        actionCooldown = llamaradaCooldown + Random.Range(0f, cooldownVariance);
        Debug.Log("[Llamarada] Llamarada Densa completada");
    }

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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetFlujoHitCenter();

        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, flujoHitBoxSize);

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = prev;
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        // Detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Flujo range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, flujoCineticoRange);

        // Llamarada range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, llamaradaRange);

        // Safe zone
        if (safeZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(safeZone.position, 1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(safeZone.position, maxChaseDistance);
        }
    }
}