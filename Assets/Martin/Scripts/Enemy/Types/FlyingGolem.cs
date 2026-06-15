using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FlyingGolem : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float facingAngleThreshold = 15f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Hover Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float hoverHeight = 4f;
    [SerializeField] private float hoverBobAmplitude = 0.2f;
    [SerializeField] private float hoverBobSpeed = 2f;
    [SerializeField] private float hoverSmooth = 8f;

    [Header("Movement")]
    [SerializeField] private float hoverMoveSpeed = 4.5f;
    [SerializeField] private float hoverStopDistance = 0.75f;
    [SerializeField] private float hoverAcceleration = 40f;

    [Header("Collider Sync")]
    [SerializeField] private CapsuleCollider bodyCapsule;
    [SerializeField] private BoxCollider bodyBox;

    [Header("Attack")]
    [Range(0f, 1f)]
    [SerializeField] private float firstAttackChance = 0.6f;
    [SerializeField] private float hitTime = 0.4f;
    [SerializeField] private float attackAnimDuration = 1.2f;
    [SerializeField] private float recoveryTime = 0.5f;

    [Header("Attack 1")]
    [SerializeField] private GameObject magicCircleVfx;
    [SerializeField] private Vector3 circleOffset;
    [SerializeField] private GameObject magicBulletPrefab;
    [SerializeField] private int bulletAmmount = 3;
    [SerializeField] private float timeBtwShoot = 0.2f;

    [Header("Attack 2")]
    [SerializeField] private GameObject chargeVfx;
    [SerializeField] private Vector3 bombPosOffset;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float chargeTime = 1f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 18f;
    [SerializeField] private float maxChaseDistance = 28f;
    [SerializeField] private float detectionDelay = 0.2f;
    [SerializeField] private bool instantDetection;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Fall / Recover")]
    [SerializeField] private float minFallTimeBeforeRecovery = 0.35f;
    [SerializeField] private float groundedRecoveryDelay = 0.15f;
    [SerializeField] private float fallForce = 8f;
    [SerializeField] private float maxFallTime = 3f;
    [SerializeField] private float recoverDuration = 0.25f;

    private bool isPerformingAction;
    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;

    private bool isFalling;
    private bool isRecovering;

    private float detectionTimer;
    private float fallStartedAt;
    private float groundedTimer;
    private float hoverTimer;

    private int patrolIndex = 0;
    private int patrolDir = 1;

    private Coroutine attackRoutine;
    private Coroutine recoverRoutine;

    private Vector3 visualBaseLocalPos;
    private Vector3 capsuleBaseCenter;
    private Vector3 boxBaseCenter;
    private bool hasCapsule;
    private bool hasBox;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.baseOffset = 0f;
            agent.speed = hoverMoveSpeed;
            agent.acceleration = hoverAcceleration;
            agent.stoppingDistance = hoverStopDistance;
            agent.autoBraking = true;
        }

        if (visualRoot != null)
        {
            visualBaseLocalPos = visualRoot.localPosition;
        }

        if (bodyCapsule == null) bodyCapsule = GetComponent<CapsuleCollider>();
        if (bodyBox == null) bodyBox = GetComponent<BoxCollider>();

        if (bodyCapsule != null)
        {
            hasCapsule = true;
            capsuleBaseCenter = bodyCapsule.center;
        }

        if (bodyBox != null)
        {
            hasBox = true;
            boxBaseCenter = bodyBox.center;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    protected override void Start()
    {
        base.Start();
        EnterHoverMode();
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        SyncAgentSettings();
        UpdateTarget();
        HandleDetection();

        if (isStaggered)
        {
            if (!isFalling) EnterFallMode();
            return;
        }

        if (isFalling)
        {
            HandleFallRecovery();
            return;
        }

        if (isStunned)
            return;

        HandleActions();

        if (isPerformingAction)
        {
            UpdateHoverVisual();
            return;
        }

        HandleMovement();
        UpdateHoverVisual();
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
            if (hit.transform != player.transform &&
                hit.transform.root != player.transform.root)
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
        if (safeZone != null)
        {
            float distHome = HorizontalDistance(transform.position, safeZone.position);

            if (hasDetectedPlayer && distHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                detectionTimer = 0f;
                return;
            }
        }

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

        float distPlayer = HorizontalDistance(transform.position, player.transform.position);

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
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void SyncAgentSettings()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.updateRotation = false;
        agent.baseOffset = 0f;
        agent.speed = hoverMoveSpeed;
        agent.acceleration = hoverAcceleration;
        agent.stoppingDistance = hoverStopDistance;
    }

    private void HandleMovement()
    {
        if (agent == null || !agent.enabled)
            return;

        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = HorizontalDistance(transform.position, player.transform.position);

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
                RotateToTarget();
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
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0)
            return;

        Transform posDesired = patrolZones[patrolIndex];
        if (posDesired == null)
            return;

        agent.SetDestination(posDesired.position);

        float dist = HorizontalDistance(transform.position, posDesired.position);

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

    private void UpdateHoverVisual()
    {
        if (visualRoot == null) return;

        hoverTimer += Time.deltaTime;

        float bob = Mathf.Sin(hoverTimer * hoverBobSpeed) * hoverBobAmplitude;
        float offsetY = hoverHeight + bob;

        Vector3 targetVisualPos = visualBaseLocalPos + Vector3.up * offsetY;
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetVisualPos, Time.deltaTime * hoverSmooth);

        SyncColliderToVisual(offsetY);
    }

    private void SyncColliderToVisual(float hoverOffsetY)
    {
        if (hasCapsule && bodyCapsule != null)
        {
            bodyCapsule.center = capsuleBaseCenter + Vector3.up * hoverOffsetY;
        }

        if (hasBox && bodyBox != null)
        {
            bodyBox.center = boxBaseCenter + Vector3.up * hoverOffsetY;
        }
    }

    // =========================================================
    // ATTACK
    // =========================================================

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || !HasValidPlayer())
            return;

        float distance = HorizontalDistance(transform.position, player.transform.position);

        if (distance <= attackRange &&
            HasLineOfSightToPlayer() &&
            IsFacingTarget() &&
            attackRoutine == null)
        {
            isPerformingAction = true;
            attackRoutine = StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        if (!HasValidPlayer())
        {
            isPerformingAction = false;
            attackRoutine = null;
            yield break;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

        float elapsed = 0f;

        while (elapsed < hitTime)
        {
            if (!HasValidPlayer() || isFalling || isStaggered)
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

        if (ChooseAttackIndex() == 0)
        {
            yield return StartCoroutine(PerformAttackOne());
        }
        else
        {
            yield return StartCoroutine(PerformAttackTwo());
        }

        float remainingTime = attackAnimDuration - hitTime;

        if (remainingTime > 0f) yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
        }

        isPerformingAction = false;
        attackRoutine = null;

        if (agent != null) agent.isStopped = false;
    }

    private int ChooseAttackIndex()
    {
        return Random.value <= firstAttackChance ? 0 : 1;
    }

    private IEnumerator PerformAttackOne()
    {
        Debug.Log("Flying Golem: attack 1");

        if (!HasValidPlayer()) yield break;

        Vector3 targetDir = GetFlatTargetDirection();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        float sideOffset = Mathf.Abs(circleOffset.x);
        Vector3 centerBase = transform.position + Vector3.up * circleOffset.y + transform.forward * circleOffset.z;

        Vector3 leftCirclePos = centerBase - right * sideOffset;
        Vector3 rightCirclePos = centerBase + right * sideOffset;

        Quaternion leftRot = GetLookRotationToTarget(leftCirclePos);
        Quaternion rightRot = GetLookRotationToTarget(rightCirclePos);

        SpawnVfx(magicCircleVfx, leftCirclePos, leftRot);
        SpawnVfx(magicCircleVfx, rightCirclePos, rightRot);

        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < bulletAmmount; i++)
        {
            if (!HasValidPlayer()) yield break;

            Vector3 leftBulletDir = (player.transform.position - leftCirclePos).normalized;
            Vector3 rightBulletDir = (player.transform.position - rightCirclePos).normalized;

            if (leftBulletDir.sqrMagnitude < 0.0001f) leftBulletDir = targetDir;
            if (rightBulletDir.sqrMagnitude < 0.0001f) rightBulletDir = targetDir;

            GameObject leftBullet = Instantiate(magicBulletPrefab, leftCirclePos, Quaternion.LookRotation(leftBulletDir));
            GameObject rightBullet = Instantiate(magicBulletPrefab, rightCirclePos, Quaternion.LookRotation(rightBulletDir));

            var leftProj = leftBullet.GetComponent<E_Projectile>();
            if (leftProj != null)
            {
                leftProj.InitProj(stats.damage, leftBulletDir);
            }

            var rightProj = rightBullet.GetComponent<E_Projectile>();
            if (rightProj != null)
            {
                rightProj.InitProj(stats.damage, rightBulletDir);
            }

            yield return new WaitForSeconds(timeBtwShoot);
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator PerformAttackTwo()
    {
        Debug.Log("Flying Golem: attack 2");

        if (!HasValidPlayer()) yield break;

        Vector3 targetDir = GetFlatTargetDirection();
        Vector3 spawnPos = transform.TransformPoint(bombPosOffset);

        SpawnVfx(chargeVfx, spawnPos, Quaternion.LookRotation(targetDir));

        yield return new WaitForSeconds(chargeTime);

        if (!HasValidPlayer()) yield break;

        Vector3 bombDir = (player.transform.position - spawnPos).normalized;
        bombDir.y = 0f;

        if (bombDir.sqrMagnitude < 0.0001f)
            bombDir = targetDir;

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.LookRotation(bombDir));

        Rigidbody bombRb = bomb.GetComponent<Rigidbody>();
        if (bombRb != null)
        {
            bombRb.linearVelocity = bombDir * stats.damage;
        }

        yield return new WaitForSeconds(0.2f);
    }

    // =========================================================
    // FALL / RECOVER
    // =========================================================

    private void EnterFallMode()
    {
        isFalling = true;
        isRecovering = false;
        fallStartedAt = Time.time;
        groundedTimer = 0f;

        if (recoverRoutine != null)
        {
            StopCoroutine(recoverRoutine);
            recoverRoutine = null;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();

            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            vel.y = 0f;
            rb.linearVelocity = vel;

            rb.AddForce(Vector3.down * fallForce, ForceMode.Impulse);
        }

        isPerformingAction = false;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private void HandleFallRecovery()
    {
        if (Time.time - fallStartedAt < minFallTimeBeforeRecovery) return;

        if (Time.time - fallStartedAt > maxFallTime)
        {
            StartRecovery();
            return;
        }

        if (!isGrounded)
        {
            groundedTimer = 0f;
            return;
        }

        groundedTimer += Time.deltaTime;

        if (groundedTimer >= groundedRecoveryDelay)
        {
            StartRecovery();
        }
    }

    private void StartRecovery()
    {
        if (isRecovering) return;

        if (recoverRoutine != null)
            StopCoroutine(recoverRoutine);

        recoverRoutine = StartCoroutine(RecoverToHover());
    }

    private IEnumerator RecoverToHover()
    {
        isRecovering = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = GetHoverSnapPosition(startPos);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.ResetPath();
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, recoverDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        hoverTimer = 0f;

        if (agent != null)
        {
            agent.Warp(targetPos);
            agent.nextPosition = targetPos;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.isStopped = false;
            agent.ResetPath();
        }

        if (visualRoot != null)
            visualRoot.localPosition = visualBaseLocalPos + Vector3.up * hoverHeight;

        SyncColliderToVisual(hoverHeight);

        isFalling = false;
        isRecovering = false;
        groundedTimer = 0f;

        recoverRoutine = null;
    }

    private void EnterHoverMode()
    {
        isFalling = false;
        isRecovering = false;
        groundedTimer = 0f;
        hoverTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.baseOffset = 0f;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.isStopped = false;
            agent.speed = hoverMoveSpeed;
            agent.acceleration = hoverAcceleration;
            agent.stoppingDistance = hoverStopDistance;
        }

        Vector3 snappedPos = GetHoverSnapPosition(transform.position);
        transform.position = snappedPos;

        if (agent != null) agent.Warp(snappedPos);

        if (visualRoot != null)
            visualRoot.localPosition = visualBaseLocalPos + Vector3.up * hoverHeight;

        SyncColliderToVisual(hoverHeight);
    }

    private Vector3 GetHoverSnapPosition(Vector3 sourcePosition)
    {
        Vector3 samplePoint = sourcePosition + Vector3.up * 10f;

        if (NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, 30f, NavMesh.AllAreas))
        {
            return new Vector3(sourcePosition.x, hit.position.y, sourcePosition.z);
        }

        return sourcePosition;
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void RotateToVelocity()
    {
        if (isFalling || isRecovering || isPerformingAction) return;
        if (agent == null) return;

        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateToTarget()
    {
        if (isFalling || isRecovering || isPerformingAction) return;
        if (!HasValidPlayer()) return;
        if (!HasLineOfSightToPlayer()) return;

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

    // =========================================================
    // LOS / HELPERS
    // =========================================================

    private bool HasLineOfSight(PlayerController target)
    {
        if (target == null) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = target.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != target.transform &&
                hit.transform.root != target.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 GetFlatTargetDirection()
    {
        if (!HasValidPlayer()) return transform.forward;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return transform.forward;

        return dir.normalized;
    }

    private Quaternion GetLookRotationToTarget(Vector3 fromPosition)
    {
        if (!HasValidPlayer()) return transform.rotation;

        Vector3 dir = player.transform.position - fromPosition;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        return Quaternion.LookRotation(dir.normalized);
    }

    private void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, position, rotation);
        Destroy(obj, 5f);
    }
}
