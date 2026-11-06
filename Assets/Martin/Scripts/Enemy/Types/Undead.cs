using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Undead : EnemyBase
{
    private enum State { Idle, Patrol, Chase, ReturnHome, BasicAttack, ConsecutiveCuts }

    [Header("Detection")]
    [SerializeField] private bool instantDetection;
    [SerializeField, Min(0.1f)] private float detectionRange = 8f;
    [SerializeField, Min(0.1f)] private float maxChaseDistance = 15f;
    [SerializeField, Min(0f)] private float detectionDelay = 0.25f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float attackRange = 2.5f;
    [SerializeField, Min(0.1f)] private float arrivalDistance = 0.5f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;
    [SerializeField, Min(0f)] private float turnSpeed = 15f; // Original Slerp-style responsiveness
    [SerializeField, Range(0f, 180f)] private float facingAngle = 15f;

    [Header("Basic Attack")]
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float basicRecovery = 0.2f;
    [SerializeField, Range(0f, 0.25f)] private float exitBlend = 0.08f;
    [Tooltip("Only a fallback if an Animator state is missing or never finishes.")]
    [SerializeField, Min(0.25f)] private float animationSafetyTimeout = 5f;

    [Header("Consecutive Cuts")]
    [SerializeField, Min(0.1f)] private float cutsMoveSpeed = 6f;
    [SerializeField, Min(0f)] private float cutsCooldown = 8f;
    [SerializeField, Min(0f)] private float cutsRecovery = 0.3f;
    [SerializeField, Range(0f, 1f)] private float cutsHpThreshold = 0.75f;
    [SerializeField, Range(0f, 1f)] private float cutsChance = 0.3f;
    [Tooltip("Optional effect played when Consecutive Cuts starts.")]
    [SerializeField] private GameObject cutsChargeVfx; // Name kept for existing prefab references.

    [Header("Cuts Wall Check")]
    [Tooltip("Put walls/solid obstacles here, but NOT the enemy or ground layers.")]
    [SerializeField, Min(0.05f)] private float wallCheckRadius = 0.35f;
    [SerializeField, Min(0f)] private float wallCheckHeight = 1f;
    [SerializeField, Min(0f)] private float wallExtraDistance = 0.12f;

    [Header("Hitbox")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 hitBoxOffset = new Vector3(0f, 1f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;
    [Tooltip("Prevents accidental duplicate special-hit events very close together.")]
    [SerializeField, Min(0f)] private float minCutsHitInterval = 0.08f;

    [Header("Runtime (read only)")]
    [SerializeField] private State state;

    private const string CutsAnimationState = "Take 001";
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly Collider[] hitBuffer = new Collider[24];

    private Vector3 spawnPosition;
    private Vector3 desiredFacing;
    private Vector3 cutsDirection;
    private Coroutine actionRoutine;
    private float detectionTimer;
    private float nextRepathTime;
    private float nextAttackTime;
    private float nextCutsTime;
    private float lastCutsHitTime;
    private int lastCutsHitFrame;
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool basicHitUsed;
    private bool cutsMoving;
    private bool animationWasFound;

    private bool IsAttacking => state == State.BasicAttack || state == State.ConsecutiveCuts;
    private Vector3 Home => HomePoint != null ? HomePoint.position :
        (Application.isPlaying ? spawnPosition : transform.position);

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;
        desiredFacing = Flat(transform.forward);
        rotateToMovement = false; // EnemyBase handles translation; this class handles facing.

        // EnemyBase adds FreezeRotationX/Z but does not clear an existing Y lock.
        // This enemy needs free Y rotation to turn with its dynamic Rigidbody.
        if (rb != null)
        {
            rb.constraints = (rb.constraints |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ) &
                ~RigidbodyConstraints.FreezeRotationY;
        }

        if (anim != null && anim.applyRootMotion)
        {
            Debug.LogWarning($"[Undead] {name}: Disable Apply Root Motion on the Animator. " +
                "This enemy uses Rigidbody movement and rotation.", this);
        }
    }

    protected override void Start()
    {
        base.Start();
        state = HasPatrol() ? State.Patrol : State.Idle;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (isStunned || isStaggered)
        {
            CancelAttack(true);
            return;
        }

        DetectPlayer();
        if (IsAttacking) return;

        if (isInReaction || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (state == State.Chase) ChasePlayer();
        else UpdatePassiveMovement();

        if (!IsAttacking) UpdateLocomotionAnimation();
    }

    protected override void FixedUpdate()
    {
        // During cuts, suppress EnemyBase's navigation braking so the dynamic
        // Rigidbody can move at the configured attack speed.
        if (state == State.ConsecutiveCuts && cutsMoving && rb != null &&
            !isDead && !isStunned && !isStaggered && !isInReaction)
        {
            MoveCutsInPhysics();
            return;
        }

        base.FixedUpdate();
        if (rb == null || isDead || isStunned || isStaggered || isInReaction) return;
        Vector3 dir = Flat(desiredFacing);
        if (dir.sqrMagnitude < 0.001f) return;
        // Preserve the original script's Slerp turn-speed behavior.
        // Previously turnSpeed = 15 meant a quick blend, NOT 15 degrees/second.
        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            Quaternion.LookRotation(dir),
            Mathf.Clamp01(turnSpeed * Time.fixedDeltaTime)));
    }

    protected override void OnDisable()
    {
        CancelAttack(false);
        base.OnDisable();
    }

    protected override void Die()
    {
        CancelAttack(false);
        base.Die();
    }

    // -------------------------------------------------------------
    // Detection and ordinary movement
    // -------------------------------------------------------------

    private void DetectPlayer()
    {
        if (!HasValidPlayer() || Flat(player.transform.position - Home).sqrMagnitude >
            maxChaseDistance * maxChaseDistance)
        {
            detectionTimer = 0f;
            if (IsAttacking) CancelAttack(false);
            if (state == State.Chase) state = State.ReturnHome;
            return;
        }

        if (state == State.Chase || IsAttacking) return;

        if (instantDetection ||
            (Flat(player.transform.position - transform.position).sqrMagnitude <=
                detectionRange * detectionRange && CanSeePlayer()))
        {
            detectionTimer += Time.deltaTime;
            if (instantDetection || detectionTimer >= detectionDelay)
            {
                detectionTimer = 0f;
                state = State.Chase;
            }
        }
        else detectionTimer = 0f;
    }

    private bool CanSeePlayer()
    {
        if (!HasValidPlayer()) return false;
        Vector3 a = transform.position + Vector3.up * targetRayHeight;
        Vector3 b = player.transform.position + Vector3.up * targetRayHeight;
        return !Physics.Linecast(a, b, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private void ChasePlayer()
    {
        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        bool inRange = toPlayer.sqrMagnitude <= attackRange * attackRange && CanSeePlayer();

        if (!inRange)
        {
            MoveTo(player.transform.position);
            FaceMovement();
            return;
        }

        StopMoving();
        SetFacing(toPlayer);
        if (Time.time < nextAttackTime || !IsFacingPlayer()) return;

        bool special = stats != null && stats.maxHp > 0f &&
            CurrentHp / stats.maxHp <= cutsHpThreshold &&
            Time.time >= nextCutsTime && Random.value < cutsChance;

        StartAttack(special);
    }

    private bool HasPatrol() => hasPatrol && patrolZones != null && patrolZones.Length > 0;

    private void UpdatePassiveMovement()
    {
        if (state == State.ReturnHome)
        {
            if (Flat(Home - transform.position).sqrMagnitude > arrivalDistance * arrivalDistance)
            {
                MoveTo(Home);
                FaceMovement();
                return;
            }
            state = HasPatrol() ? State.Patrol : State.Idle;
        }

        if (state != State.Patrol || !HasPatrol())
        {
            state = State.Idle;
            StopMoving();
            return;
        }

        patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolZones.Length - 1);
        Transform point = patrolZones[patrolIndex];
        if (point == null) { StopMoving(); return; }

        if (Flat(point.position - transform.position).sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            if (patrolZones.Length == 1) { StopMoving(); return; }
            patrolIndex += patrolDirection;
            if (patrolIndex >= patrolZones.Length)
            {
                patrolIndex = patrolZones.Length - 2;
                patrolDirection = -1;
            }
            else if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolDirection = 1;
            }
            point = patrolZones[patrolIndex];
            if (point == null) { StopMoving(); return; }
        }

        MoveTo(point.position);
        FaceMovement();
    }

    private void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        if (Time.time < nextRepathTime || agent.pathPending) return;
        if (!agent.hasPath || (agent.destination - destination).sqrMagnitude > 0.25f)
        {
            agent.SetDestination(destination);
            nextRepathTime = Time.time + repathInterval;
        }
    }

    private void StopMoving()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
    }

    // -------------------------------------------------------------
    // Attacks: one coroutine, two animation-driven patterns
    // -------------------------------------------------------------

    private void StartAttack(bool special)
    {
        if (anim == null || stats == null || !HasValidPlayer()) return;

        StopMoving();
        basicHitUsed = false;
        lastCutsHitFrame = -1;
        lastCutsHitTime = -999f;
        SetFacing(player.transform.position - transform.position);

        state = special ? State.ConsecutiveCuts : State.BasicAttack;
        if (special) nextCutsTime = Time.time + cutsCooldown;
        actionRoutine = StartCoroutine(PerformAttack(special));
    }

    private IEnumerator PerformAttack(bool special)
    {
        if (special)
        {
            // "Take 001" is the ONLY special attack animation.
            // Lock the forward direction once, then move for the entire clip.
            if (cutsChargeVfx != null)
                Destroy(Instantiate(cutsChargeVfx, transform.position, Quaternion.identity), 3f);

            cutsDirection = Flat(player.transform.position - transform.position);
            if (cutsDirection.sqrMagnitude < 0.001f) cutsDirection = Flat(transform.forward);
            cutsDirection = cutsDirection.sqrMagnitude > 0.001f ? cutsDirection.normalized : Vector3.forward;
            SetFacing(cutsDirection);

            PlayStateFromStart(CutsAnimationState);
            cutsMoving = true;
            yield return WaitForAnimation(Animator.StringToHash(CutsAnimationState), State.ConsecutiveCuts, false);
            StopCutsMovement();
            if (state != State.ConsecutiveCuts || !CanContinueAttack()) yield break;
        }
        else
        {
            PlayStateFromStart("Attack");
            yield return WaitForAnimation(AttackHash, State.BasicAttack, true);
            if (state != State.BasicAttack || !CanContinueAttack()) yield break;
        }

        if (anim != null && anim.isActiveAndEnabled)
            anim.CrossFadeInFixedTime("Idle", exitBlend, 0, 0f);
        yield return new WaitForSeconds(Mathf.Max(exitBlend,
            special ? cutsRecovery : basicRecovery));

        if (IsAttacking && !isDead) EndAction();
    }

    private bool CanContinueAttack() => !isDead && !isStunned && !isStaggered &&
                                        HasValidPlayer();

    private void PlayStateFromStart(string name)
    {
        if (anim == null || string.IsNullOrEmpty(name)) return;
        PlayAnim(name);
        // Force a restart at normalized time zero, even if the same state was active.
        anim.Play(Animator.StringToHash(name), 0, 0f);
    }

    private IEnumerator WaitForAnimation(int hash, State expected, bool blendBeforeEnd)
    {
        animationWasFound = false;
        float elapsed = 0f;
        yield return null;
        while (state == expected && CanContinueAttack() && elapsed < animationSafetyTimeout)
        {
            AnimatorStateInfo clip = anim.GetCurrentAnimatorStateInfo(0);
            if (clip.shortNameHash == hash)
            {
                animationWasFound = true;
                float remaining = Mathf.Max(0f, 1f - clip.normalizedTime) *
                    clip.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                if (clip.normalizedTime >= 1f || (blendBeforeEnd && remaining <= exitBlend))
                    yield break;
            }
            else if (animationWasFound) yield break;
            else if (elapsed >= 0.35f) break; // Missing state: fail fast, not after the full timeout.

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!animationWasFound && state == expected && anim != null)
            Debug.LogWarning($"[Undead] {name}: Animator state '{(expected == State.ConsecutiveCuts ? CutsAnimationState : "Attack")}' was not found on layer 0.", this);
    }

    private void EndAction()
    {
        StopCutsMovement();
        nextAttackTime = Time.time + attackCooldown;
        actionRoutine = null;
        if (IsAttacking) state = HasValidPlayer() ? State.Chase : State.ReturnHome;
    }

    private void CancelAttack(bool blendToIdle)
    {
        if (!IsAttacking) return;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        EndAction();
        if (blendToIdle && anim != null && !isDead && anim.isActiveAndEnabled)
            anim.CrossFadeInFixedTime("Idle", exitBlend, 0, 0f);
    }

    // -------------------------------------------------------------
    // Forward cuts: physics-controlled movement + wall checks
    // -------------------------------------------------------------

    private void MoveCutsInPhysics()
    {
        float travel = cutsMoveSpeed * Time.fixedDeltaTime + wallExtraDistance;
        Vector3 origin = rb.position + Vector3.up * wallCheckHeight;

        // SphereCast detects a wall before we reach it; CheckSphere covers the
        // case where the cast begins touching/intersecting wall geometry.
        bool wall = Physics.SphereCast(origin, wallCheckRadius, cutsDirection,
                        out _, travel, obstacleLayer, QueryTriggerInteraction.Ignore) ||
                    Physics.CheckSphere(origin + cutsDirection * travel,
                        wallCheckRadius, obstacleLayer, QueryTriggerInteraction.Ignore);

        // Also avoid leaving walkable NavMesh, such as running off a ledge.
        bool navBlocked = agent == null || !agent.enabled || !agent.isOnNavMesh ||
            NavMesh.Raycast(agent.nextPosition,
                agent.nextPosition + cutsDirection * travel, out _, agent.areaMask);

        if (wall || navBlocked)
        {
            StopCutsMovement(); // The cuts ANIMATION continues, movement stops.
            return;
        }

        rb.MoveRotation(Quaternion.LookRotation(cutsDirection));
        Vector3 velocity = Flat(rb.linearVelocity);
        rb.AddForce(cutsDirection * cutsMoveSpeed - velocity,
            ForceMode.VelocityChange);
    }

    private void StopCutsMovement()
    {
        cutsMoving = false;
        if (rb == null) return;
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);
    }

    // -------------------------------------------------------------
    // Animation Event: one hit for Basic, one per event during Cuts
    // -------------------------------------------------------------

    public void DoHit()
    {
        if (isDead || isStunned || isStaggered || stats == null || !HasValidPlayer()) return;
        if (state == State.BasicAttack)
        {
            if (basicHitUsed) return;
            basicHitUsed = true;
        }
        else if (state == State.ConsecutiveCuts)
        {
            if (lastCutsHitFrame == Time.frameCount ||
                Time.time - lastCutsHitTime < minCutsHitInterval) return;
            lastCutsHitFrame = Time.frameCount;
            lastCutsHitTime = Time.time;
        }
        else return;

        // Prevent overlap-box hits through walls or from duplicate player colliders.
        if (!CanSeePlayer()) return;
        Vector3 direction = state == State.ConsecutiveCuts ? cutsDirection : Flat(transform.forward);
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
        direction.Normalize();

        Quaternion rotation = Quaternion.LookRotation(direction);
        Vector3 center = transform.position + rotation * hitBoxOffset;
        int count = Physics.OverlapBoxNonAlloc(center, hitBoxSize * 0.5f,
            hitBuffer, rotation, hitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.GetComponentInParent<PlayerControl>() != player) continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            DamageInfo info = new DamageInfo { damage = stats.damage, hitDirection = direction };
            target.TakeDamage(in info);
            break;
        }
    }

    // -------------------------------------------------------------
    // Rotation, animation and gizmos
    // -------------------------------------------------------------

    private void FaceMovement()
    {
        // Prefer the actual navigation direction while walking around obstacles.
        Vector3 direction = agent != null && agent.enabled && agent.isOnNavMesh
            ? Flat(agent.desiredVelocity)
            : Vector3.zero;

        // An agent may have no desired velocity while a path is pending or
        // while stationary. During a chase, keep turning toward the player.
        if (direction.sqrMagnitude < 0.001f && state == State.Chase && HasValidPlayer())
            direction = Flat(player.transform.position - transform.position);

        SetFacing(direction);
    }

    private void SetFacing(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude > 0.001f) desiredFacing = direction.normalized;
    }

    private bool IsFacingPlayer()
    {
        Vector3 toward = Flat(player.transform.position - transform.position);
        return toward.sqrMagnitude < 0.001f ||
            Vector3.Angle(Flat(transform.forward), toward) <= facingAngle;
    }

    private void UpdateLocomotionAnimation()
    {
        if (anim == null) return;
        bool walking = agent != null && !agent.isStopped && rb != null &&
            Flat(rb.linearVelocity).sqrMagnitude > 0.09f;
        string stateName = walking ? "Walk" : "Idle";
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) ||
            (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(stateName)))
            return;
        anim.CrossFadeInFixedTime(stateName, 0.12f, 0, 0f);
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Home, maxChaseDistance);
        Vector3 f = Flat(transform.forward);
        if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(f.normalized);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + rotation * hitBoxOffset,
            rotation, Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, hitBoxSize);
        Gizmos.matrix = old;
    }
}

