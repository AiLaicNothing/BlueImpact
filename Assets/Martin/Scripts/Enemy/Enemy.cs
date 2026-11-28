using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for all NORMAL enemies. Inherits everything from EnemyBase
/// (damage, stagger, reactions, VFX, death, level objectives, spawners, taunt...).
///
/// Movement: NO NavMeshAgent. Paths come from NavMesh.CalculatePath and the dynamic
/// Rigidbody follows the path corners (through EnemyBase.GetDesiredVelocity()).
/// If the prefab still has a NavMeshAgent it is disabled automatically.
///
/// Subclasses only implement UpdateCombat() (and their attack coroutines).
/// </summary>
public abstract class Enemy : EnemyBase
{
    public enum AIState { Idle, Patrol, ReturnHome, Combat, Action }

    // Animation event ids (send them with EnemyAnimationEvents or AnimEvent(string)).
    public const string EvtHit = "Hit";
    public const string EvtShoot = "Shoot";
    public const string EvtEnd = "End";

    // ================================================================
    // INSPECTOR
    // ================================================================

    [Header("Movement")]
    [Min(0.1f)][SerializeField] protected float moveSpeed = 3.5f;
    [Tooltip("Max turn speed, degrees per second.")]
    [Min(1f)][SerializeField] protected float turnSpeed = 540f;
    [Tooltip("How quickly it eases into the target direction (like the old Slerp turnSpeed = 15). Higher = snappier.")]
    [Min(0.1f)][SerializeField] protected float turnResponsiveness = 12f;
    [Tooltip("Extra distance before leaving attack range again. Stops chase/attack flicker at the range edge.")]
    [Min(0f)][SerializeField] protected float rangeHysteresis = 0.5f;
    [Tooltip("Face the movement direction while walking (unless facing is locked).")]
    [SerializeField] protected bool autoFaceMovement = true;
    [Min(0.05f)][SerializeField] protected float arrivalDistance = 0.5f;

    [Header("Pathfinding (NavMesh queries, no agent)")]
    [Tooltip("Agent Type ID used to query the baked NavMesh. 0 = Humanoid.")]
    [SerializeField] protected int navAgentTypeId = 0;
    [SerializeField] protected int navAreaMask = NavMesh.AllAreas;
    [Tooltip("Max distance used to project the enemy / target onto the NavMesh.")]
    [Min(0.1f)][SerializeField] protected float navSampleRadius = 2f;
    [Min(0.05f)][SerializeField] protected float repathInterval = 0.25f;
    [Tooltip("Repath when the destination moved more than this.")]
    [Min(0.05f)][SerializeField] protected float repathTargetMove = 0.5f;
    [Tooltip("Repath at least this often, even if the destination is still (recovers from pushes).")]
    [Min(0.1f)][SerializeField] protected float maxPathAge = 1f;
    [Min(0.05f)][SerializeField] protected float cornerReachDistance = 0.35f;
    [Min(0f)][SerializeField] protected float stoppingDistance = 0.15f;

    [Header("Separation (optional, keeps enemies from stacking)")]
    [Tooltip("Layer(s) of other enemies. Leave empty to disable.")]
    [SerializeField] protected LayerMask separationLayers;
    [Min(0.1f)][SerializeField] protected float separationRadius = 1.2f;
    [Min(0f)][SerializeField] protected float separationStrength = 0.6f;

    [Header("Detection")]
    [SerializeField] protected bool instantDetection;
    [Min(0.1f)][SerializeField] protected float detectionRange = 8f;
    [Min(0f)][SerializeField] protected float detectionDelay = 0.25f;
    [Tooltip("Leash: lose the player when this far from home.")]
    [Min(0.1f)][SerializeField] protected float maxChaseDistance = 15f;
    [Tooltip("Walls / environment that block sight and travel. Do NOT include player or enemies.")]
    [SerializeField] protected LayerMask sightBlockingLayers;
    [SerializeField] protected float eyeHeight = 1.2f;
    [Tooltip("Height on the player used for aiming and line of sight.")]
    [SerializeField] protected float targetAimHeight = 1.2f;
    [Tooltip("Being hit makes the enemy chase the player immediately.")]
    [SerializeField] protected bool aggroOnHit = true;
    [Min(0f)][SerializeField] protected float patrolWaitTime = 0f;

    [Header("Attacks (shared)")]
    [SerializeField] protected LayerMask playerHitLayers = ~0;
    [Tooltip("ON: hits/shots happen on Animation Events (Hit / Shoot). OFF: the fallback times in each attack are used.")]
    [SerializeField] protected bool useAnimationEvents = true;
    [Range(0f, 0.3f)][SerializeField] protected float exitBlend = 0.08f;
    [Tooltip("Fallback when an Animator state never finishes.")]
    [Min(0.25f)][SerializeField] protected float animationSafetyTimeout = 5f;
    [Tooltip("Duration used when an attack's Animator state does not exist.")]
    [Min(0.1f)][SerializeField] protected float missingAnimationDuration = 0.8f;

    [Header("Animation States")]
    [SerializeField] protected string idleState = "Idle";
    [SerializeField] protected string moveState = "Walk";
    [Tooltip("Optional looping state while staggered. Leave empty to skip.")]
    [SerializeField] protected string staggerState = "";

    [Header("AI Runtime (Read Only)")]
    [SerializeField] protected AIState state;
    [SerializeField] protected bool hasTarget;

    // ================================================================
    // RUNTIME
    // ================================================================

    protected Coroutine actionRoutine;
    /// <summary>Multiplies moveSpeed (e.g. a faster retreat). Reset when an action ends.</summary>
    protected float speedMultiplier = 1f;

    private Vector3 spawnPosition;
    private float detectionTimer;
    private int patrolIndex;
    private int patrolDirection = 1;
    private float patrolWaitTimer;

    // Pathfinding
    private NavMeshPath navPath;
    private readonly Vector3[] corners = new Vector3[64];
    private int cornerCount;
    private int cornerIndex;
    private bool hasDestination;
    private Vector3 destination;
    private Vector3 lastPathTarget;
    private float nextRepathTime;
    private float lastPathTime;
    private bool manualControl;
    private Vector3 manualVelocity;
    private Vector3 lastDesiredVelocity;

    // Facing
    private Vector3 desiredFacing;
    private bool facingLocked;

    // Actions / animation events
    private readonly HashSet<string> receivedEvents = new HashSet<string>();
    private int actionAnimHash;
    private bool actionAnimEntered;
    private bool actionAnimMissing;
    private float actionAnimStartTime;
    private int actionAnimStartFrame;

    private readonly Collider[] hitBuffer = new Collider[24];
    private readonly Collider[] separationBuffer = new Collider[16];

    public bool HasTarget => hasTarget;
    public float HpPercent => stats != null && stats.maxHp > 0f ? currentHp / stats.maxHp : 1f;

    protected bool IsBusy => actionRoutine != null;
    protected bool IsMoving => rb != null && Flat(rb.linearVelocity).sqrMagnitude > 0.09f;
    protected Vector3 Home => HomePoint != null ? HomePoint.position : spawnPosition;
    protected NavMeshQueryFilter NavFilter => new NavMeshQueryFilter { agentTypeID = navAgentTypeId, areaMask = navAreaMask };

    // ================================================================
    // ABSTRACT / HOOKS
    // ================================================================

    /// <summary>Called every frame while the enemy has a target and is not running an action.</summary>
    protected abstract void UpdateCombat();

    /// <summary>Called when an action is interrupted (stun, stagger, death, target lost).</summary>
    protected virtual void OnActionCancelled() { }

    /// <summary>Animation event received while an action runs. Default stores it for ConsumeEvent().</summary>
    protected virtual void OnAnimEvent(string id) { receivedEvents.Add(id); }

    /// <summary>Can the AI brain run (choose moves / attacks)?</summary>
    protected virtual bool CanThink() => !isStunned && !isStaggered;

    /// <summary>Position projected onto the NavMesh for path queries.</summary>
    protected virtual Vector3 GetNavQueryPosition() => rb != null ? rb.position : transform.position;

    // ================================================================
    // UNITY LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();

        navPath = new NavMeshPath();
        spawnPosition = transform.position;
        desiredFacing = Flat(transform.forward);
        if (desiredFacing.sqrMagnitude < 0.001f) desiredFacing = Vector3.forward;
        rotateToMovement = false; // This class handles facing.

        // No agent: we only query the NavMesh. Disable a leftover agent on old prefabs.
        if (agent != null)
        {
            if (debug) Debug.LogWarning($"[Enemy] {name}: NavMeshAgent found and disabled. You can remove it from the prefab.", this);
            agent.enabled = false;
            agent = null;
        }

        if (rb != null)
        {
            // Smooth visuals for a physics-driven body (MoveRotation without it looks jittery).
            if (rb.interpolation == RigidbodyInterpolation.None)
                rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.constraints = (rb.constraints |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ) &
                ~RigidbodyConstraints.FreezeRotationY;
        }

        // A freshly added component has "What Is Ground = Nothing": the enemy would never be
        // grounded, never move and never turn (only spin). Fall back to every layer but its own.
        if (whatIsGround.value == 0)
        {
            whatIsGround = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
            if (debug) Debug.LogWarning($"[Enemy] {name}: 'What Is Ground' was empty. Using all layers except its own. Set it in the Inspector.", this);
        }

        if (anim != null && anim.applyRootMotion)
            Debug.LogWarning($"[Enemy] {name}: disable Apply Root Motion. Movement is Rigidbody-driven.", this);
    }

    protected override void OnDisable()
    {
        CancelAction(false);
        base.OnDisable();
    }

    protected override void Start()
    {
        base.Start();
        state = HasPatrol() ? AIState.Patrol : AIState.Idle;
    }

    protected override void Update()
    {
        base.Update(); // Ground check.
        if (isDead) return;

        if (!CanThink())
        {
            if (IsBusy) CancelAction(false);
            StopMoving();
            if (isStaggered && !string.IsNullOrEmpty(staggerState)) PlayLoop(staggerState);
            return;
        }

        UpdateDetection();

        if (!IsBusy)
        {
            if (hasTarget && HasValidPlayer())
            {
                state = AIState.Combat;
                UpdateCombat();
            }
            else UpdatePassive();
        }

        UpdatePathing();

        if (!IsBusy) PlayLoop(IsMoving && hasDestination ? moveState : idleState);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // Drives the Rigidbody with GetDesiredVelocity().
        if (isDead || rb == null) return;

        // Y rotation is free (so MoveRotation works), but collisions must not spin the body.
        if (!rb.isKinematic) rb.angularVelocity = Vector3.zero;

        if (isInReaction || isStunned || isStaggered) return;

        // Turn toward the target even if the ground check fails for a frame.
        ApplyFacing(CanDriveMovement() ? lastDesiredVelocity : Vector3.zero);
    }

    // ================================================================
    // ENEMYBASE HOOKS
    // ================================================================

    protected override Vector3 GetDesiredVelocity()
    {
        lastDesiredVelocity = ComputeDesiredVelocity();
        return lastDesiredVelocity;
    }

    protected override bool UseInstantVelocity => manualControl;

    protected override void OnDamaged(in DamageInfo info)
    {
        if (aggroOnHit && !hasTarget && HasValidPlayer()) AcquireTarget();
    }

    protected override void OnReactionEnded() => InvalidatePath();

    protected override void TriggerStagger()
    {
        if (isStaggered) return;
        CancelAction(false);
        StopMoving();
        base.TriggerStagger();
    }

    protected override void Die()
    {
        if (isDead) return;
        CancelAction(false);
        StopMoving();
        base.Die();
    }

    // Works without a groundCheck child (uses the Rigidbody position).
    protected override void CheckGround()
    {
        if (groundCheck != null) { base.CheckGround(); return; }

        Vector3 origin = (rb != null ? rb.position : transform.position) + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f,
            whatIsGround, QueryTriggerInteraction.Ignore);
    }

    // ================================================================
    // AI: DETECTION, PATROL, RETURN HOME
    // ================================================================

    protected virtual void UpdateDetection()
    {
        if (!HasValidPlayer())
        {
            if (hasTarget) LoseTarget();
            detectionTimer = 0f;
            return;
        }

        if (hasTarget)
        {
            if (FlatDistance(transform.position, Home) > maxChaseDistance) LoseTarget();
            return;
        }

        if (state == AIState.ReturnHome) return;

        bool seen = FlatDistanceToPlayer() <= detectionRange && CanSeePlayer();
        if (!seen) { detectionTimer = 0f; return; }

        detectionTimer += Time.deltaTime;
        if (instantDetection || detectionTimer >= detectionDelay) AcquireTarget();
    }

    protected virtual void AcquireTarget()
    {
        hasTarget = true;
        detectionTimer = 0f;
        if (!IsBusy) state = AIState.Combat;
    }

    protected virtual void LoseTarget()
    {
        hasTarget = false;
        detectionTimer = 0f;
        CancelAction(true);
        state = AIState.ReturnHome;
    }

    protected virtual void UpdatePassive()
    {
        switch (state)
        {
            case AIState.ReturnHome:
                if (FlatDistance(transform.position, Home) > arrivalDistance) MoveTo(Home);
                else
                {
                    StopMoving();
                    state = HasPatrol() ? AIState.Patrol : AIState.Idle;
                }
                break;

            case AIState.Patrol:
                UpdatePatrol();
                break;

            default:
                if (HasPatrol()) state = AIState.Patrol;
                else { state = AIState.Idle; StopMoving(); }
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (!HasPatrol()) { state = AIState.Idle; StopMoving(); return; }

        patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolZones.Length - 1);
        Transform point = patrolZones[patrolIndex];
        if (point == null) { AdvancePatrol(); return; }

        if (FlatDistance(transform.position, point.position) > arrivalDistance)
        {
            patrolWaitTimer = 0f;
            MoveTo(point.position);
            return;
        }

        StopMoving();
        if (patrolZones.Length == 1) return;

        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer >= patrolWaitTime)
        {
            patrolWaitTimer = 0f;
            AdvancePatrol();
        }
    }

    private void AdvancePatrol()
    {
        if (patrolZones.Length <= 1) return;
        patrolIndex += patrolDirection;
        if (patrolIndex >= patrolZones.Length) { patrolIndex = patrolZones.Length - 2; patrolDirection = -1; }
        else if (patrolIndex < 0) { patrolIndex = 1; patrolDirection = 1; }
    }

    protected bool HasPatrol() => hasPatrol && patrolZones != null && patrolZones.Length > 0;

    protected bool CanSeePlayer()
    {
        if (!HasValidPlayer()) return false;
        if (sightBlockingLayers.value == 0) return true;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        return !Physics.Linecast(origin, PlayerAimPoint, sightBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    // ================================================================
    // PATHFINDING (NavMesh.CalculatePath -> Rigidbody)
    // ================================================================

    /// <summary>Walk to a world position using NavMesh paths. Safe to call every frame.</summary>
    protected void MoveTo(Vector3 worldDestination)
    {
        manualControl = false;
        if (!hasDestination) nextRepathTime = 0f;
        hasDestination = true;
        destination = worldDestination;
    }

    /// <summary>Stop path following (the Rigidbody brakes to a stop).</summary>
    protected void StopMoving()
    {
        hasDestination = false;
        cornerCount = 0;
        cornerIndex = 0;
        manualControl = false;
        manualVelocity = Vector3.zero;
    }

    /// <summary>Drive the Rigidbody with an exact horizontal velocity (dashes, lunges).</summary>
    protected void SetManualVelocity(Vector3 velocity)
    {
        manualControl = true;
        hasDestination = false;
        cornerCount = 0;
        manualVelocity = Flat(velocity);
    }

    protected void ClearManualVelocity(bool hardStop = true)
    {
        manualControl = false;
        manualVelocity = Vector3.zero;
        if (hardStop && rb != null)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }
    }

    /// <summary>Forces a new path on the next frame (e.g. after being knocked away).</summary>
    protected void InvalidatePath()
    {
        cornerCount = 0;
        nextRepathTime = 0f;
    }

    private void UpdatePathing()
    {
        if (manualControl || !hasDestination) return;
        if (Time.time < nextRepathTime) return;

        bool targetMoved = (destination - lastPathTarget).sqrMagnitude > repathTargetMove * repathTargetMove;
        bool stale = Time.time - lastPathTime >= maxPathAge;
        if (cornerCount == 0 || targetMoved || stale) RecalculatePath();
    }

    private void RecalculatePath()
    {
        nextRepathTime = Time.time + repathInterval;
        lastPathTime = Time.time;
        lastPathTarget = destination;
        cornerCount = 0;
        cornerIndex = 0;

        NavMeshQueryFilter filter = NavFilter;
        if (!NavMesh.SamplePosition(GetNavQueryPosition(), out NavMeshHit from, navSampleRadius, filter)) return;
        if (!NavMesh.SamplePosition(destination, out NavMeshHit to, navSampleRadius, filter)) return;
        if (!NavMesh.CalculatePath(from.position, to.position, filter, navPath)) return;
        if (navPath.status == NavMeshPathStatus.PathInvalid) return;

        // A partial path still brings the enemy as close as possible.
        cornerCount = navPath.GetCornersNonAlloc(corners);
        cornerIndex = cornerCount > 1 ? 1 : 0;
    }

    private Vector3 ComputeDesiredVelocity()
    {
        if (manualControl) return manualVelocity;
        if (!hasDestination || cornerCount == 0) return Vector3.zero;

        Vector3 position = rb.position;

        // Skip corners we already reached.
        while (cornerIndex < cornerCount - 1 &&
               FlatDistance(position, corners[cornerIndex]) <= cornerReachDistance)
            cornerIndex++;

        Vector3 toCorner = Flat(corners[cornerIndex] - position);
        float distance = toCorner.magnitude;
        bool lastCorner = cornerIndex >= cornerCount - 1;

        if (lastCorner && distance <= stoppingDistance) return Vector3.zero;
        if (distance < 0.0001f) return Vector3.zero;

        float speed = moveSpeed * speedMultiplier;
        if (lastCorner) speed = Mathf.Min(speed, distance * 4f); // Smooth arrival.

        Vector3 desired = toCorner / distance * speed;

        if (separationLayers.value != 0 && separationStrength > 0f)
        {
            desired += ComputeSeparation() * (moveSpeed * separationStrength);
            desired = Vector3.ClampMagnitude(desired, speed);
        }
        return desired;
    }

    private Vector3 ComputeSeparation()
    {
        int count = Physics.OverlapSphereNonAlloc(rb.position, separationRadius,
            separationBuffer, separationLayers, QueryTriggerInteraction.Ignore);

        Vector3 push = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider other = separationBuffer[i];
            if (other == null || other.attachedRigidbody == rb) continue;

            Vector3 away = Flat(rb.position - other.transform.position);
            float distance = away.magnitude;
            if (distance < 0.001f || distance > separationRadius) continue;
            push += away / distance * (1f - distance / separationRadius);
        }
        return Vector3.ClampMagnitude(push, 1f);
    }

    /// <summary>
    /// True if the enemy can travel 'distance' in 'direction' without leaving the NavMesh
    /// or hitting a wall (sightBlockingLayers). Use for dashes / lunges.
    /// </summary>
    protected bool CanTravel(Vector3 direction, float distance, float radius = 0.35f)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.0001f || distance <= 0f) return true;
        direction.Normalize();

        NavMeshQueryFilter filter = NavFilter;
        if (!NavMesh.SamplePosition(GetNavQueryPosition(), out NavMeshHit start, navSampleRadius, filter))
            return false;
        if (NavMesh.Raycast(start.position, start.position + direction * distance, out _, filter))
            return false;

        if (sightBlockingLayers.value != 0)
        {
            Vector3 origin = transform.position + Vector3.up * Mathf.Max(radius + 0.05f, eyeHeight * 0.6f);
            if (Physics.SphereCast(origin, radius, direction, out _, distance,
                    sightBlockingLayers, QueryTriggerInteraction.Ignore))
                return false;
        }
        return true;
    }

    // ================================================================
    // FACING
    // ================================================================

    protected void SetFacing(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude > 0.001f) desiredFacing = direction.normalized;
    }

    protected void FacePlayer()
    {
        if (HasValidPlayer()) SetFacing(player.transform.position - transform.position);
    }

    /// <summary>Hold a direction; auto-facing movement is ignored until UnlockFacing().</summary>
    protected void LockFacing(Vector3 direction)
    {
        SetFacing(direction);
        facingLocked = true;
    }

    protected void UnlockFacing() => facingLocked = false;

    protected bool IsFacing(Vector3 direction, float toleranceDegrees)
    {
        direction = Flat(direction);
        return direction.sqrMagnitude < 0.001f ||
               Vector3.Angle(Flat(transform.forward), direction) <= toleranceDegrees;
    }

    protected bool IsFacingPlayer(float toleranceDegrees) =>
        HasValidPlayer() && IsFacing(player.transform.position - transform.position, toleranceDegrees);

    private void ApplyFacing(Vector3 desiredVelocity)
    {
        if (!facingLocked && autoFaceMovement && !manualControl && desiredVelocity.sqrMagnitude > 0.04f)
            desiredFacing = desiredVelocity.normalized;

        if (desiredFacing.sqrMagnitude < 0.001f) return;

        // Eased turn (exponential Slerp), capped at turnSpeed degrees/second.
        Quaternion target = Quaternion.LookRotation(desiredFacing, Vector3.up);
        float t = 1f - Mathf.Exp(-turnResponsiveness * Time.fixedDeltaTime);
        Quaternion eased = Quaternion.Slerp(rb.rotation, target, t);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, eased, turnSpeed * Time.fixedDeltaTime));
    }

    /// <summary>
    /// Range check with hysteresis: enters at 'range', only leaves at 'range + rangeHysteresis'.
    /// Keep one bool per range in the subclass.
    /// </summary>
    protected bool InRange(float distance, float range, ref bool wasInRange)
    {
        wasInRange = distance <= (wasInRange ? range + rangeHysteresis : range);
        return wasInRange;
    }

    // ================================================================
    // ACTIONS (attacks) + ANIMATION EVENTS
    // ================================================================

    /// <summary>Runs an attack/maneuver coroutine. The AI brain pauses until it finishes.</summary>
    protected void StartAction(IEnumerator routine)
    {
        CancelAction(false);
        receivedEvents.Clear();
        state = AIState.Action;
        actionRoutine = StartCoroutine(ActionWrapper(routine));
    }

    private IEnumerator ActionWrapper(IEnumerator routine)
    {
        yield return routine;
        actionRoutine = null;
        ResetActionState();
        state = hasTarget ? AIState.Combat : AIState.Idle;
    }

    /// <summary>Stops the current action (stun, stagger, death, target lost).</summary>
    protected void CancelAction(bool blendToIdle)
    {
        if (actionRoutine == null) return;

        StopCoroutine(actionRoutine);
        actionRoutine = null;
        ResetActionState();
        if (manualControl) ClearManualVelocity();
        state = hasTarget ? AIState.Combat : AIState.Idle;
        OnActionCancelled();

        if (blendToIdle && !isDead) CrossFadeTo(idleState);
    }

    private void ResetActionState()
    {
        receivedEvents.Clear();
        speedMultiplier = 1f;
        facingLocked = false;
        if (manualControl) ClearManualVelocity(false);
    }

    /// <summary>Animation Event entry point (or use EnemyAnimationEvents on a child Animator).</summary>
    public void AnimEvent(string id)
    {
        if (isDead || !IsBusy || string.IsNullOrEmpty(id)) return;
        OnAnimEvent(id);
    }

    // Convenience names for existing clips.
    public void DoHit() => AnimEvent(EvtHit);
    public void Shoot() => AnimEvent(EvtShoot);
    public void EndAttack() => AnimEvent(EvtEnd);

    // Old event names already placed on existing clips (Automata / FlyingGolem).
    public void ShootBurst() => AnimEvent(EvtShoot);
    public void ShootSpread() => AnimEvent(EvtShoot);
    public void FireMagicVolley() => AnimEvent(EvtShoot);
    public void FireMagicBullet() => AnimEvent(EvtShoot);
    public void ThrowBomb() => AnimEvent(EvtShoot);
    public void SpawnMagicCircles() { }
    public void SpawnChargeVfx() { }

    /// <summary>True once per received event.</summary>
    protected bool ConsumeEvent(string id) => receivedEvents.Remove(id);

    /// <summary>
    /// Event-driven when useAnimationEvents is on; otherwise true when elapsed >= fallbackTime.
    /// Guard it with your own "done" flag.
    /// </summary>
    protected bool EventOrTime(string id, float elapsed, float fallbackTime)
    {
        if (useAnimationEvents) return ConsumeEvent(id);
        return elapsed >= fallbackTime;
    }

    /// <summary>Starts an attack animation from frame 0 and tracks it for ActionAnimFinished().</summary>
    protected void PlayActionAnim(string stateName)
    {
        actionAnimHash = Animator.StringToHash(stateName);
        actionAnimEntered = false;
        actionAnimStartTime = Time.time;
        actionAnimStartFrame = Time.frameCount;
        actionAnimMissing = anim == null || !anim.isActiveAndEnabled || string.IsNullOrEmpty(stateName) ||
                            !anim.HasState(0, actionAnimHash);

        if (actionAnimMissing)
        {
            if (debug && anim != null)
                Debug.LogWarning($"[{GetType().Name}] {name}: Animator state '{stateName}' not found.", this);
            return;
        }

        currentAnimHash = actionAnimHash;
        anim.Play(actionAnimHash, 0, 0f);
    }

    /// <summary>True when the clip started with PlayActionAnim has (almost) finished.</summary>
    protected bool ActionAnimFinished()
    {
        float elapsed = Time.time - actionAnimStartTime;
        if (ConsumeEvent(EvtEnd)) return true;
        if (actionAnimMissing) return elapsed >= missingAnimationDuration;
        if (elapsed >= animationSafetyTimeout) return true;
        if (Time.frameCount == actionAnimStartFrame) return false;

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == actionAnimHash)
        {
            actionAnimEntered = true;
            float remaining = Mathf.Max(0f, 1f - info.normalizedTime) *
                              info.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
            return remaining <= exitBlend;
        }
        return actionAnimEntered && !anim.IsInTransition(0);
    }

    /// <summary>Plays a clip and waits until it ends. onTick receives the elapsed time every frame.</summary>
    protected IEnumerator PlayAndWait(string stateName, Action<float> onTick = null)
    {
        PlayActionAnim(stateName);
        float start = Time.time;
        do
        {
            onTick?.Invoke(Time.time - start);
            yield return null;
        }
        while (!ActionAnimFinished());
        onTick?.Invoke(Time.time - start); // Last chance for a late event.
    }

    protected void CrossFadeTo(string stateName)
    {
        if (anim == null || !anim.isActiveAndEnabled || string.IsNullOrEmpty(stateName)) return;
        int hash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, hash)) return;
        currentAnimHash = hash;
        anim.CrossFadeInFixedTime(hash, exitBlend, 0, 0f);
    }

    // ================================================================
    // ATTACK HELPERS
    // ================================================================

    protected Vector3 PlayerAimPoint =>
        HasValidPlayer() ? player.transform.position + Vector3.up * targetAimHeight : transform.position + transform.forward;

    protected float FlatDistanceToPlayer() =>
        HasValidPlayer() ? FlatDistance(transform.position, player.transform.position) : float.MaxValue;

    /// <summary>Flat, normalized direction to the player (transform.forward if none).</summary>
    protected Vector3 DirectionToPlayer()
    {
        Vector3 direction = HasValidPlayer() ? Flat(player.transform.position - transform.position) : Vector3.zero;
        if (direction.sqrMagnitude < 0.001f) direction = Flat(transform.forward);
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    /// <summary>3D aim direction from a point to the player's chest.</summary>
    protected Vector3 AimDirection(Vector3 origin)
    {
        Vector3 direction = PlayerAimPoint - origin;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    /// <summary>OverlapBox in front of the enemy. Damages the player once. Returns true on hit.</summary>
    protected bool MeleeHit(Vector3 localOffset, Vector3 size, Vector3 forward, float damageMultiplier = 1f)
    {
        if (!HasValidPlayer() || stats == null || !CanSeePlayer()) return false;

        forward = Flat(forward);
        if (forward.sqrMagnitude < 0.001f) forward = Flat(transform.forward);
        forward.Normalize();

        Quaternion rotation = Quaternion.LookRotation(forward);
        Vector3 center = transform.position + rotation * localOffset;
        int count = Physics.OverlapBoxNonAlloc(center, size * 0.5f, hitBuffer, rotation,
            playerHitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.GetComponentInParent<PlayerControl>() != player) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            DamageInfo info = new DamageInfo
            {
                damage = stats.damage * damageMultiplier,
                hitDirection = forward
            };
            damageable.TakeDamage(in info);
            return true;
        }
        return false;
    }

    /// <summary>Spawns E_Projectile or E_ProjExplosive and initializes it with stats.damage.</summary>
    protected GameObject FireProjectile(GameObject prefab, Vector3 origin, Vector3 direction, float damageMultiplier = 1f)
    {
        if (prefab == null) return null;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        direction.Normalize();

        GameObject instance = Instantiate(prefab, origin, Quaternion.LookRotation(direction));
        float damage = stats != null ? stats.damage * damageMultiplier : 0f;

        if (instance.TryGetComponent(out E_Projectile projectile)) projectile.InitProj(damage, direction);
        else if (instance.TryGetComponent(out E_ProjExplosive explosive)) explosive.InitProj(damage, direction);
        else if (debug) Debug.LogWarning($"[{GetType().Name}] {prefab.name} has no E_Projectile / E_ProjExplosive.", this);

        return instance;
    }

    protected void SpawnTimedVfx(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, rotation);
        if (duration > 0f) Destroy(effect, duration);
    }

    // ================================================================
    // UTILITIES + GIZMOS
    // ================================================================

    protected static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    protected static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    protected static void DrawHitBoxGizmo(Transform t, Vector3 localOffset, Vector3 size, Color color)
    {
        Vector3 forward = Flat(t.forward);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward);

        Gizmos.color = color;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(t.position + rotation * localOffset, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = old;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!debug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        Gizmos.DrawWireSphere(Application.isPlaying ? Home : transform.position, maxChaseDistance);

        if (Application.isPlaying && cornerCount > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < cornerCount - 1; i++) Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }
}
