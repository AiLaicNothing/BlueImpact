using System.Collections;
using UnityEngine;

public class Undead : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private bool instantDetection;

    [Header("Rotation")]
    [SerializeField] private float facingAngleThreshold = 15f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Hit")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 hitBoxPos = new Vector3(0f, 0f, 1f);
    [SerializeField] private float hitTime = 0.35f;
    [SerializeField] private float attackAnimDuration = 0.8f;
    [SerializeField] private float recoveryTime = 0.2f;

    [Header("Consecutive Cuts")]
    [SerializeField] private float consecutiveCutsChargeTime = 1f; //Duration of charging the attack
    [SerializeField] private float consecutiveCutsDuration = 2.5f; //How long last the attack
    [SerializeField] private float consecutiveCutsHitInterval = 0.4f; //Every x secs do damage
    [SerializeField] private float consecutiveCutsMoveSpeed = 6f; // how fast it move
    [SerializeField] private float consecutiveCutsCooldown = 8f; // CD for him not spamming it
    [SerializeField] private float specialRecoveryTime;
    [Range(0f, 1f)]
    [SerializeField] private float consecutiveCutsHpThreshold = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float consecutiveCutsChance = 0.3f;
    [SerializeField] private GameObject consecutiveCutsSFX;
    private Vector3 consecutiveCutsForward;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float detectionDelay = 0.25f;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Temp")]
    [SerializeField] private GameObject hitBoxPrefab;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool isPerformingAction;
    private bool canUseConsecutiveCuts = true;

    private float detectionTimer;
    private int patrolIndex = 0;
    private int patrolDir = 1;

    private Coroutine attackRoutine;
    private Coroutine consecutiveCutsRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = 4.5f;
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
                RotateToVelocity();
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
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
            }
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0)
            return;

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
    // ATTACK
    // =========================================================

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer()) return;

        float distance = DistanceToPlayer();
        float hpPercent = CurrentHp / stats.maxHp;

        if (hpPercent <= consecutiveCutsHpThreshold && canUseConsecutiveCuts)
        {
            if (Random.value <= consecutiveCutsChance && consecutiveCutsRoutine == null)
            {
                Debug.Log("[Undead] Use consecutive Attacks");

                consecutiveCutsRoutine = StartCoroutine(PerformConsecutiveCuts());
                return;
            }
        }

        if (distance <= attackRange && HasLineOfSightToPlayer() && IsFacingTarget() && attackRoutine == null)
        {
            Debug.Log("[Undead] Use Attacks");
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

        agent.isStopped = true;
        agent.ResetPath();
        isPerformingAction = true;

        FaceTargetInstant();

        float elapsed = 0f;

        while (elapsed < hitTime)
        {
            if (!HasValidPlayer())
            {
                isPerformingAction = false;
                attackRoutine = null;
                if (agent != null) agent.isStopped = false;
                yield break;
            }

            FaceTargetInstant();
            elapsed += Time.deltaTime;
            yield return null;
        }

        FaceTargetInstant();
        DoHit();

        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        yield return new WaitForSeconds(recoveryTime);

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

    private IEnumerator PerformConsecutiveCuts()
    {
        if (!HasValidPlayer())
        {
            consecutiveCutsRoutine = null;
            yield break;
        }

        canUseConsecutiveCuts = false;
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (consecutiveCutsSFX != null)
        {
            Instantiate(consecutiveCutsSFX, transform.position, Quaternion.identity);
        }

        // Charge phase: stand still, face the player.
        float chargeTimer = consecutiveCutsChargeTime;

        while (chargeTimer > 0f)
        {
            if (!HasValidPlayer())
            {
                isPerformingAction = false;
                consecutiveCutsRoutine = null;

                if (agent != null) agent.isStopped = false;

                StartCoroutine(ConsecutiveCutsCooldownRoutine());
                yield break;
            }

            FaceTargetInstant();
            chargeTimer -= Time.deltaTime;
            yield return null;
        }

        // Lock direction once, right before the dash.
        consecutiveCutsForward = GetAttackForward();
        consecutiveCutsForward.y = 0f;

        if (consecutiveCutsForward.sqrMagnitude < 0.0001f)
        {
            consecutiveCutsForward = transform.forward;
            consecutiveCutsForward.y = 0f;
        }

        consecutiveCutsForward.Normalize();

        // Optional: keep the enemy facing the locked direction once.
        transform.rotation = Quaternion.LookRotation(consecutiveCutsForward);

        // Move phase: go straight forward without tracking the player.
        float moveTimer = consecutiveCutsDuration;
        float hitTimer = 0f;

        while (moveTimer > 0f)
        {
            if (!HasValidPlayer())
                break;

            if (agent != null)
            {
                agent.Move(consecutiveCutsForward * consecutiveCutsMoveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += consecutiveCutsForward * consecutiveCutsMoveSpeed * Time.deltaTime;
            }

            hitTimer += Time.deltaTime;
            if (hitTimer >= consecutiveCutsHitInterval)
            {
                DoHit();
                hitTimer = 0f;
            }

            moveTimer -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(specialRecoveryTime);

        isPerformingAction = false;
        consecutiveCutsRoutine = null;

        if (agent != null)
        {
            agent.isStopped = false;
        }

        StartCoroutine(ConsecutiveCutsCooldownRoutine());
    }

    private IEnumerator ConsecutiveCutsCooldownRoutine()
    {
        yield return new WaitForSeconds(consecutiveCutsCooldown);
        canUseConsecutiveCuts = true;
    }

    private void DoHit()
    {
        Vector3 attackForward = transform.forward;
        attackForward.y = 0f;

        if (attackForward.sqrMagnitude < 0.0001f) attackForward = Vector3.forward;

        attackForward.Normalize();

        Vector3 center = transform.position + attackForward * hitBoxPos.z + Vector3.up * hitBoxPos.y;

        Quaternion rot = Quaternion.LookRotation(attackForward, Vector3.up);

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, rot);

        ShowHitbox(center, hitBoxSize, rot);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null) continue;

            DamageInfo info = new DamageInfo
            {
                damage = stats.damage,
            };

            damageable.TakeDamage(in info);
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

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitBoxPrefab == null) return;

        GameObject box = Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }
}