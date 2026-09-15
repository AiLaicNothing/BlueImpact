using System.Collections;
using UnityEngine;

public class Slime_HitAndRun : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float facingAngleThreshold = 15f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Hit")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 hitBoxPos = new Vector3(0f, 0f, 1f);
    [SerializeField] private float hitTime = 0.35f;
    [SerializeField] private float attackAnimDuration = 0.8f;
    [SerializeField] private float postAttackStunDuration = 0.3f;
    [SerializeField] private float attackCooldown = 0.2f;

    [Header("Hit-and-Run Pattern")]
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float retreatSpeed = 7f;
    [SerializeField] private float recoveryDuration = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float detectionDelay = 0.25f;

    [Header("Patrol")]
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private GameObject hitBoxPrefab;

    // =========================================================
    // STATE
    // =========================================================

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool returningHome;
    private bool isPerformingAction;

    // Hit-and-Run states
    private bool isRetreating;
    private bool isRecovering;

    private float detectionTimer;
    private float actionTimer;

    private int patrolIndex;
    private int patrolDir = 1;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (isStunned || IsStaggered) return;
        if (agent.enabled == false) return;

        UpdateTarget();
        HandleDetection();
        HandleActions();

        if (isPerformingAction || isRetreating || isRecovering) return;

        HandleMovement();

        if (agent.velocity.sqrMagnitude >= 0.01f)
        {
           // anim.Play("Walk");
        }
        else
        {
           // anim.Play("Idle");
        }
    }

    // =========================================================
    // TARGETING
    // =========================================================

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

    // =========================================================
    // DETECTION
    // =========================================================

    private void HandleDetection()
    {
        if (safeZone == null) return;

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            detectionTimer = 0f;
            return;
        }

        float playerDistHome = Vector3.Distance(player.transform.position, safeZone.position);
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
                isRetreating = false;
                isRecovering = false;
            }
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = DistanceToPlayer();

            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
                RotateToVelocity();
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
                RotateToTarget();
            }
        }
        else if (returningHome)
        {
            agent.isStopped = false;
            agent.SetDestination(spawnPos.position);

            RotateToVelocity();

            if (!agent.pathPending && agent.remainingDistance <= stopDistance)
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

        if (dist <= stopDistance)
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

    // =========================================================
    // ATTACK LOGIC
    // =========================================================

    private void HandleActions()
    {
        if (isPerformingAction || isRetreating || isRecovering)
            return;
        if (!hasDetectedPlayer)
            return;
        if (!HasValidPlayer())
            return;

        float distance = DistanceToPlayer();

        actionTimer += Time.deltaTime;

        if (distance <= attackRange && IsFacingTarget())
        {
            if (actionTimer >= attackCooldown)
            {
                StartCoroutine(PerformAttack());
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

       // anim.Play("Attack");

        yield return new WaitForSeconds(hitTime);

        FaceTargetInstant();
        DoHit();

        float remaining = attackAnimDuration - hitTime;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

     //   anim.Play("Idle");

        // Post-attack stun (aturdimiento tras golpe)
        yield return new WaitForSeconds(postAttackStunDuration);

        isPerformingAction = false;
        actionTimer = 0f;

        // TRANSICIÓN: Después del ataque, vamos a retirarnos
        if (HasValidPlayer() && isFollowingPlayer)
        {
            StartCoroutine(PerformRetreat());
        }
    }

    private void DoHit()
    {
        Vector3 attackForward = GetAttackForward();
        Vector3 center = transform.position + attackForward * hitBoxPos.z + Vector3.up * hitBoxPos.y;
        Quaternion rot = Quaternion.LookRotation(attackForward);

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, rot);

        ShowHitbox(center, hitBoxSize, rot);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                DamageInfo info = new DamageInfo
                {
                    damage = stats.damage,
                    hitDirection = transform.forward,
                };

                damageable.TakeDamage(in info);
            }
        }
    }

    // =========================================================
    // RETREAT BEHAVIOR
    // =========================================================

    private IEnumerator PerformRetreat()
    {
        isRetreating = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Calcular dirección de retirada: alejarse del jugador
        Vector3 retreatDir = GetRetreatDirection();

        // Calcular posición objetivo de retirada
        Vector3 retreatTarget = transform.position + retreatDir * retreatDistance;

        // Usar el agente para navegar a la posición de retroceso
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;
            agent.SetDestination(retreatTarget);

            // Esperar hasta que llegue al destino o timeout
            float timeout = 3f; // Máximo tiempo de retirada
            float elapsed = 0f;

            while (elapsed < timeout && !agent.pathPending && agent.remainingDistance > stopDistance)
            {
                RotateAwayFromPlayer();
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Asegurar que se detiene
            agent.ResetPath();
            agent.isStopped = true;
        }
        else
        {
            // Fallback si agent no está disponible: movimiento directo
            float moveDistance = 0f;
            while (moveDistance < retreatDistance)
            {
                float step = retreatSpeed * Time.deltaTime;
                transform.position += retreatDir * step;
                moveDistance += step;
                yield return null;
            }
        }

        isRetreating = false;

        // TRANSICIÓN: Después de retirarse, entra en recuperación
        if (HasValidPlayer() && isFollowingPlayer)
        {
            StartCoroutine(PerformRecovery());
        }
    }

    // =========================================================
    // RECOVERY BEHAVIOR
    // =========================================================

    private IEnumerator PerformRecovery()
    {
        isRecovering = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.speed = 4.5f; // Restaurar velocidad de persecución
        }

        // Durante la recuperación: solo mantener rotación hacia el jugador
        float recoveryElapsed = 0f;
        while (recoveryElapsed < recoveryDuration)
        {
            if (HasValidPlayer())
            {
                RotateToTarget();
            }

            recoveryElapsed += Time.deltaTime;
          //  anim.Play("Idle");
            yield return null;
        }

        isRecovering = false;

        // TRANSICIÓN: Recuperación completa, volver a perseguir
        if (HasValidPlayer() && isFollowingPlayer)
        {
            if (agent != null)
            {
                agent.isStopped = false;
            }
        }
    }

    // =========================================================
    // ROTATION & UTILITIES
    // =========================================================

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateToTarget()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateAwayFromPlayer()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = transform.position - player.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
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

    private bool IsFacingTarget()
    {
        if (!HasValidPlayer()) return false;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= facingAngleThreshold;
    }

    private Vector3 GetAttackForward()
    {
        if (HasValidPlayer())
        {
            Vector3 dir = player.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude < 0.01f) fallback = Vector3.forward;

        return fallback.normalized;
    }

    private Vector3 GetRetreatDirection()
    {
        if (HasValidPlayer())
        {
            // Dirección opuesta al jugador
            Vector3 dir = transform.position - player.transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
        }

        // Fallback: alejarse hacia adelante
        Vector3 fallback = transform.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude < 0.01f) fallback = Vector3.forward;

        return fallback.normalized;
    }

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitBoxPrefab == null || !debug) return;

        GameObject box = Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.green;
        if (safeZone != null)
        {
            Gizmos.DrawWireSphere(safeZone.position, 1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(safeZone.position, maxChaseDistance);
        }

        // Visualizar distancia de retroceso
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}