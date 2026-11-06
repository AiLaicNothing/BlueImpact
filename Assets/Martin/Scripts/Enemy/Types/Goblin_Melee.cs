using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goblin_Melee : EnemyBase
{
    private enum State { Idle, Patrol, Chase, Attack, ReturnHome }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 8f;
    [SerializeField, Min(0.1f)] private float maxChaseDistance = 15f;
    [SerializeField, Min(0f)] private float detectionDelay = 0.25f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float attackRange = 2f;
    [SerializeField, Min(0.1f)] private float arrivalDistance = 0.5f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;
    [SerializeField, Range(0f, 180f)] private float attackFacingAngle = 15f;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.2f;
    [SerializeField, Range(0f, 0.3f)] private float attackExitBlend = 0.08f;
    [Tooltip("Fallback only; actual duration comes from the Attack state.")]
    [SerializeField, Min(0.25f)] private float attackSafetyTimeout = 3f;
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 hitBoxPos = new Vector3(0f, 1f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Runtime (read only)")]
    [SerializeField] private State state;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly Collider[] hitBuffer = new Collider[24];

    private Vector3 initialPosition;
    private Vector3 desiredFacing;
    private Vector3 attackFacing;
    private float detectionTimer;
    private float nextAttackTime;
    private float nextRepathTime;
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool hitUsed;
    private bool hitEventReceived;
    private bool endAttackRequested;
    private Coroutine attackRoutine;

    private Vector3 Home => HomePoint != null ? HomePoint.position :
        (Application.isPlaying ? initialPosition : transform.position);

    protected override void Awake()
    {
        base.Awake();
        initialPosition = transform.position;
        desiredFacing = Flat(transform.forward);
        rotateToMovement = false; // EnemyBase drives velocity, this script faces targets.
    }

    protected override void Start()
    {
        base.Start();
        state = hasPatrol && patrolZones != null && patrolZones.Length > 0
            ? State.Patrol : State.Idle;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // Full stagger/stun cancels attacks; a brief normal hit recoil does not.
        if (isStunned || isStaggered)
        {
            CancelAttack(true);
            return;
        }

        DetectPlayer();
        if (state == State.Attack) return;
        if (isInReaction || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (state == State.Chase) UpdateChase();
        else UpdatePassiveMovement();

        if (state != State.Attack) UpdateLocomotionAnimation();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (rb == null || isDead || isStunned || isStaggered || isInReaction) return;

        Vector3 forward = Flat(desiredFacing);
        if (forward.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(forward.normalized);
        rb.MoveRotation(Quaternion.RotateTowards(
            rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
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

    // ----------------------------------------------------------
    // Detection + chase
    // ----------------------------------------------------------

    private void DetectPlayer()
    {
        if (!HasValidPlayer() ||
            Flat(player.transform.position - Home).sqrMagnitude > maxChaseDistance * maxChaseDistance)
        {
            detectionTimer = 0f;
            if (state == State.Attack) CancelAttack(true);
            if (state == State.Chase) state = State.ReturnHome;
            return;
        }

        if (state == State.Chase || state == State.Attack) return;

        if (Flat(player.transform.position - transform.position).sqrMagnitude <=
                detectionRange * detectionRange && CanSeePlayer())
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionDelay)
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
        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 target = player.transform.position + Vector3.up * targetRayHeight;
        return !Physics.Linecast(origin, target, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private void UpdateChase()
    {
        if (!HasValidPlayer()) { state = State.ReturnHome; return; }

        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        bool canAttack = toPlayer.sqrMagnitude <= attackRange * attackRange && CanSeePlayer();

        if (!canAttack)
        {
            MoveTo(player.transform.position);
            FaceMovement();
            return;
        }

        StopMoving();
        SetFacing(toPlayer);
        if (Time.time >= nextAttackTime && IsFacingPlayer()) StartAttack();
    }

    // ----------------------------------------------------------
    // Return home / optional patrol
    // ----------------------------------------------------------

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
            state = hasPatrol && patrolZones != null && patrolZones.Length > 0
                ? State.Patrol : State.Idle;
        }

        if (state != State.Patrol || patrolZones == null || patrolZones.Length == 0)
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

    // ----------------------------------------------------------
    // Attack (Animation Event is responsible for the actual hit)
    // ----------------------------------------------------------

    private void StartAttack()
    {
        if (anim == null || stats == null || !HasValidPlayer()) return;
        state = State.Attack;
        hitUsed = false;
        hitEventReceived = false;
        endAttackRequested = false;
        StopMoving();

        attackFacing = Flat(player.transform.position - transform.position);
        if (attackFacing.sqrMagnitude < 0.001f) attackFacing = Flat(transform.forward);
        if (attackFacing.sqrMagnitude < 0.001f) attackFacing = Vector3.forward;
        attackFacing.Normalize();
        SetFacing(attackFacing); // Do not track the player through the attack.
        attackRoutine = StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        PlayAnim("Attack");
        // EnemyBase's older PlayAnim() may not restart the same state at frame 0.
        anim.Play(AttackHash, 0, 0f);
        yield return null;

        float elapsed = 0f;
        bool enteredAttack = false;
        while (state == State.Attack && !endAttackRequested &&
               !isDead && !isStunned && !isStaggered)
        {
            AnimatorStateInfo current = anim.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == AttackHash)
            {
                enteredAttack = true;
                float secondsLeft = Mathf.Max(0f, 1f - current.normalizedTime) *
                    current.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                if (secondsLeft <= attackExitBlend) break;
            }
            else if (enteredAttack) break;

            elapsed += Time.deltaTime;
            if (elapsed >= attackSafetyTimeout) break;
            yield return null;
        }

        if (state != State.Attack || isDead || isStunned || isStaggered) yield break;

        if (!hitEventReceived)
            Debug.LogWarning($"[Goblin_Melee] {name}: Attack finished without the DoHit Animation Event.", this);

        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackHash)
            anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);

        yield return new WaitForSeconds(Mathf.Max(attackRecovery, attackExitBlend));
        if (state != State.Attack || isDead || isStunned || isStaggered) yield break;

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
        state = State.Chase;
    }

    /// <summary>Required DoHit event at the contact frame of the non-looping Attack clip.</summary>
    public void DoHit()
    {
        if (state != State.Attack || isDead || isStunned || isStaggered) return;
        hitEventReceived = true;
        if (hitUsed) return;
        hitUsed = true; // Exactly one hit attempt per attack, even with duplicate events.
        if (!HasValidPlayer() || stats == null || !CanSeePlayer()) return;

        Quaternion rotation = Quaternion.LookRotation(attackFacing);
        Vector3 center = transform.position + rotation * hitBoxPos;
        int count = Physics.OverlapBoxNonAlloc(center, hitBoxSize * 0.5f,
            hitBuffer, rotation, hitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.GetComponentInParent<PlayerControl>() != player) continue;
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            DamageInfo info = new DamageInfo
            {
                damage = stats.damage,
                hitDirection = attackFacing
            };
            damageable.TakeDamage(in info);
            break;
        }
    }

    /// <summary>Optional Animation Event near the end of Attack.</summary>
    public void EndAttack()
    {
        if (state == State.Attack) endAttackRequested = true;
    }

    private void CancelAttack(bool blendToIdle)
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
        if (state != State.Attack) return;

        nextAttackTime = Time.time + attackCooldown;
        hitUsed = true;
        state = State.Chase;
        if (blendToIdle && anim != null && !isDead)
            anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);
    }

    // ----------------------------------------------------------
    // Facing + visuals
    // ----------------------------------------------------------

    private void FaceMovement()
    {
        if (agent != null) SetFacing(agent.desiredVelocity);
    }

    private void SetFacing(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude > 0.001f) desiredFacing = direction.normalized;
    }

    private bool IsFacingPlayer()
    {
        Vector3 direction = Flat(player.transform.position - transform.position);
        return direction.sqrMagnitude < 0.001f ||
            Vector3.Angle(Flat(transform.forward), direction) <= attackFacingAngle;
    }

    private void UpdateLocomotionAnimation()
    {
        if (anim == null) return;
        bool moving = agent != null && !agent.isStopped && rb != null &&
            Flat(rb.linearVelocity).sqrMagnitude > 0.09f;
        string desired = moving ? "Walk" : "Idle";
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(desired) ||
            (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(desired)))
            return;
        anim.CrossFadeInFixedTime(desired, 0.12f, 0, 0f);
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Home, maxChaseDistance);

        Vector3 forward = Flat(transform.forward);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward.normalized);
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + rotation * hitBoxPos,
            rotation, Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, hitBoxSize);
        Gizmos.matrix = previous;
    }
}

