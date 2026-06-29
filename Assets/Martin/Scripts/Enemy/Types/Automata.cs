using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Automata : EnemyBase
{
    [Header("Core")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float attackRange = 8f;
    [Range(0f, 360f)]
    [SerializeField] private float facingAngleThreshold;
    [SerializeField] private float turnSpeed;

    [Header("Attack-1")]
    [SerializeField] private int ammountPerAttack1;
    [SerializeField] private float timeBtwShoot;

    [Header("Attack-2")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDistance;
    [SerializeField] private int ammountPerAttack2;
    [SerializeField] private float attack2ConeAngle = 20f;

    [Header("Attack-3")]
    [SerializeField] private GameObject stunProj;

    [Header("Targeting")]
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private float attackBuffer = 0.75f;
    [SerializeField] private bool instantDetection;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float detectionDelay = 0.25f;

    [Header("Patrol")]
    [SerializeField] private float stopDistance = 0.5f;
    private int patrolIndex = 0;
    private int patrolDir = 1;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool isPerformingAction;
    private float detectionTimer;

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
        if (isDead) return;

        base.Update();

        if (isStunned || IsStaggered) return;
        if (agent == null || !agent.enabled) return;

        UpdateTarget();
        HandleDetection();

        if (isPerformingAction) return;

        HandleActions();
        HandleMovement();
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

    private bool HasLineOfSightToPlayer()
    {
        if (!HasValidPlayer()) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != player.transform && hit.transform.root != player.transform.root) return false;
        }

        return true;
    }

    // =========================================================
    // DETECTION
    // =========================================================

    private void HandleDetection()
    {
        if (safeZone == null)
            return;

        if (instantDetection)
        {
            hasDetectedPlayer = true;
            isFollowingPlayer = true;
            return;
        }

        if (!HasValidPlayer())
        {
            detectionTimer = 0f;
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
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

            if (distance > attackRange || !HasLineOfSightToPlayer())
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
                anim.Play("Walk");
                RotateToVelocity();
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
                anim.Play("Idle");
                RotateToPlayer();
            }
        }
        else
        {
            if (hasPatrol)
            {
                agent.isStopped = false;
                HandlePatrol();
                RotateToVelocity();
            }
            else
            {
                agent.ResetPath();
                anim.Play("Idle");
            }
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        Transform posDesired = patrolZones[patrolIndex];
        agent.SetDestination(posDesired.position);
        anim.Play("Walk");
        RotateToVelocity();

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
    // ATTACK
    // =========================================================

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer()) return;

        float roll = Random.value;

        if (roll < 0.4f) StartCoroutine(PerformAttack_1());

        else if (roll < 0.8f) StartCoroutine(PerformAttack_2());

        else StartCoroutine(PerformAttack_3());
    }

    private IEnumerator PerformAttack_1()
    {
        isPerformingAction = true;

        anim.Play("Disparo_1");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        for (int i = 0; i < ammountPerAttack1; i++)
        {
            if (!HasValidPlayer()) break;

            FaceTargetInstant();
            FireProjectile(projectilePrefab, GetAttackForward());

            if (i < ammountPerAttack1 - 1) yield return new WaitForSeconds(timeBtwShoot);
        }

        yield return new WaitForSeconds(1.5f);
        isPerformingAction = false;
    }

    private IEnumerator PerformAttack_2()
    {
        isPerformingAction = true;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

        Vector3 dashDir = GetAttackForward();
        float moved = 0f;

        anim.Play("Dash");

        while (moved < dashDistance)
        {
            float step = dashForce * Time.deltaTime;
            transform.position += dashDir * step;
            moved += step;
            yield return null;
        }

        if (agent != null && agent.enabled) agent.Warp(transform.position);

        FaceTargetInstant();

        anim.Play("Disparo_dash");

        int count = Mathf.Max(1, ammountPerAttack2);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : (i / (count - 1f)) - 0.5f;
            float angle = t * attack2ConeAngle;
            Vector3 shotDir = Quaternion.AngleAxis(angle, Vector3.up) * dashDir;

            FireProjectile(projectilePrefab, shotDir);
        }

        yield return new WaitForSeconds(1.5f);
        isPerformingAction = false;
    }

    private IEnumerator PerformAttack_3()
    {
        isPerformingAction = true;

        anim.Play("Disparo_3");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();
        FireProjectile(stunProj, GetAttackForward());

        yield return new WaitForSeconds(1.5f);
        isPerformingAction = false;
    }

    private void FireProjectile(GameObject prefab, Vector3 direction)
    {
        if (prefab == null) return;

        Transform origin = firePoint != null ? firePoint : transform;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

        direction.Normalize();

        GameObject proj = Instantiate(prefab, origin.position, Quaternion.LookRotation(direction));

        var projectile = proj.GetComponent<E_Projectile>();

        if (projectile != null)
        {
            projectile.InitProj(stats.damage, direction);
        }
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void RotateToVelocity()
    {
        if (isPerformingAction) return;

        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateToPlayer()
    {
        if (isPerformingAction) return;
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
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

    // =========================================================
    // TEMP VISUAL
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!debug) return;

        if (safeZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(safeZone.position, 1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(safeZone.position, maxChaseDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}