using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Goblin_Range : EnemyBase
{
    [Header("Core")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float attackRange = 8f;

    [Header("Escape / Reposition")]
    [SerializeField] private float escapeRange = 3f;
    [SerializeField] private float repositionRange = 6f;
    [SerializeField] private int tryReposition = 8;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float repathDelay = 0.25f;

    [Header("Targeting")]
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private float attackBuffer = 0.75f;
    [SerializeField] private bool instantDetection;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 12f;

    [Header("OnHitData")]
    [SerializeField] private float hitTime = 0.35f;
    [SerializeField] private float attackAnimDuration = 0.8f;
    [SerializeField] private float recoveryTime = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float detectionDelay = 0.25f;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance = 0.5f;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool isPerformingAction;
    private bool isEscaping;
    private bool isRepositioning;
    private bool hasMoveDestination;

    private Vector3 currentMoveDestination;
    private float detectionTimer;
    private float nextRepathTime;

    private int patrolIndex = 0;
    private int patrolDir = 1;

    private Coroutine attackRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.25f;
            agent.autoBraking = true;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (isStunned || IsStaggered) return;
        if (agent == null || !agent.enabled) return;

        UpdateTarget();
        HandleDetection();
        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
    }

    // ==================================================
    // TARGETING
    // ==================================================

    private void UpdateTarget()
    {
        if (!HasValidPlayer())
        {
            player = FindFirstObjectByType<PlayerControl>();
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (!HasValidPlayer()) return false;

        Vector3 origin = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * targetRayHeight;

        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != player.transform &&
                hit.transform.root != player.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    // ==================================================
    // DETECTION
    // ==================================================

    private void HandleDetection()
    {
        if (safeZone == null) return;

        if (instantDetection)
        {
            hasDetectedPlayer = true;
            isFollowingPlayer = true;
            return;
        }

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            detectionTimer = 0f;
            return;
        }

        float distHome = Vector3.Distance(transform.position, safeZone.position);
        float distPlayer = DistanceToPlayer();

        if (!hasDetectedPlayer)
        {
            if (distPlayer <= detectionRange && HasLineOfSightToPlayer())
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
            if (distHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                detectionTimer = 0f;
                isEscaping = false;
                isRepositioning = false;
                hasMoveDestination = false;
            }
        }
    }

    // ==================================================
    // MOVEMENT
    // ==================================================

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = DistanceToPlayer();

            if (distance < escapeRange)
            {
                if (!isEscaping || !hasMoveDestination || Time.time >= nextRepathTime)
                {
                    StartEscape();
                }

                agent.isStopped = false;
                agent.SetDestination(currentMoveDestination);
                RotateAwayFromTarget();
                return;
            }

            if (distance > attackRange + attackBuffer)
            {
                isEscaping = false;
                isRepositioning = false;
                hasMoveDestination = false;

                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
                RotateToTargetSmooth();
                return;
            }

            if (!HasLineOfSightToPlayer())
            {
                if (!isRepositioning || !hasMoveDestination || Time.time >= nextRepathTime)
                {
                    StartReposition();
                }

                agent.isStopped = false;
                agent.SetDestination(currentMoveDestination);
                RotateToTargetSmooth();
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            RotateToTargetSmooth();
            return;
        }

        if (hasPatrol)
        {
            agent.isStopped = false;
            HandlePatrol();
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void StartEscape()
    {
        if (!HasValidPlayer()) return;

        Vector3 targetPos = player.transform.position;
        Vector3 awayDir = (transform.position - targetPos).normalized;

        Vector3 desired = transform.position + awayDir * (escapeRange + repositionRange);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, repositionRange, NavMesh.AllAreas))
        {
            currentMoveDestination = hit.position;
        }
        else
        {
            currentMoveDestination = desired;
        }

        hasMoveDestination = true;
        isEscaping = true;
        isRepositioning = false;
        nextRepathTime = Time.time + repathDelay;
    }

    private void StartReposition()
    {
        if (!HasValidPlayer()) return;

        Vector3 center = player.transform.position;
        currentMoveDestination = FindValidPosAround(center, repositionRange);

        hasMoveDestination = true;
        isRepositioning = true;
        isEscaping = false;
        nextRepathTime = Time.time + repathDelay;
    }

    private Vector3 FindValidPosAround(Vector3 center, float radius)
    {
        Vector3 bestPos = transform.position;
        float bestScore = Mathf.Infinity;

        for (int i = 0; i < tryReposition; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized;
            Vector3 candidate = center + new Vector3(rand.x, 0f, rand.y) * radius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                float dist = Mathf.Abs(Vector3.Distance(hit.position, center) - radius);

                if (dist < bestScore)
                {
                    bestScore = dist;
                    bestPos = hit.position;
                }
            }
        }

        return bestPos;
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

    // ==================================================
    // ATTACK
    // ==================================================

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer()) return;

        float distance = DistanceToPlayer();

        if (distance <= attackRange && HasLineOfSightToPlayer() && !isEscaping &&!isRepositioning && attackRoutine == null)
        {
            attackRoutine = StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        if (!HasValidPlayer())
        {
            attackRoutine = null;
            yield break;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        isPerformingAction = true;

        float elapsed = 0f;

        while (elapsed < hitTime)
        {
            if (!HasValidPlayer() || isEscaping || isRepositioning)
            {
                isPerformingAction = false;
                attackRoutine = null;
                if (agent != null) agent.isStopped = false;
                yield break;
            }

            RotateToTargetSmooth();
            elapsed += Time.deltaTime;
            yield return null;
        }

        Shoot();

        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        yield return new WaitForSeconds(recoveryTime);

        isEscaping = false;
        isRepositioning = false;
        hasMoveDestination = false;

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
        }

        isPerformingAction = false;
        attackRoutine = null;

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    private void Shoot()
    {
        if (!HasValidPlayer() || projectilePrefab == null || firePoint == null) return;

        if (!HasLineOfSightToPlayer()) return;

        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));

        var projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            projectile.InitProj(stats.damage, dir);
        }
    }

    // ==================================================
    // ROTATION
    // ==================================================

    private void RotateToTargetSmooth()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * 60f * Time.deltaTime);
    }

    private void RotateAwayFromTarget()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = transform.position - player.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * 60f * Time.deltaTime);
    }
}

