using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Goblin_Range : EnemyBase
{
    private enum State { Idle, Patrol, Approach, Hold, Attack, Retreat, ReturnHome }

    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.2f;
    [SerializeField, Range(0.01f, 0.25f)] private float attackExitBlend = 0.08f;
    [SerializeField, Min(0.5f)] private float attackSafetyTimeout = 3f;
    [SerializeField, Range(1f, 90f)] private float attackFacingAngle = 20f;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 10f;
    [SerializeField, Min(0.1f)] private float maxChaseDistance = 15f;
    [SerializeField, Min(0f)] private float detectionDelay = 0.25f;
    [SerializeField] private bool instantDetection;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float preferredAttackDistance = 7f;
    [SerializeField, Min(1f)] private float turnSpeed = 540f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float arrivalDistance = 0.6f;

    [Header("Retreat")]
    [SerializeField, Min(0.1f)] private float retreatTriggerDistance = 3f;
    [SerializeField, Min(0f)] private float retreatPreparation = 0.3f;
    [SerializeField, Min(0.05f)] private float minRetreatDuration = 0.65f;
    [SerializeField, Min(0.05f)] private float maxRetreatDuration = 1.35f;
    [SerializeField, Min(0f)] private float retreatCooldown = 2.5f;
    [SerializeField, Min(0.5f)] private float retreatTravelDistance = 4f;
    [SerializeField, Min(0.5f)] private float retreatSafeDistance = 5f;

    [Header("Runtime (for debugging)")]
    [SerializeField] private State state;
    [SerializeField] private bool detectedPlayer;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly float[] RetreatAngles = { 0f, 40f, -40f, 80f, -80f, 130f, -130f };

    private Vector3 startPosition;
    private Vector3 facingDirection;
    private Vector3 retreatDestination;
    private float detectionTimer;
    [SerializeField] private float closeTimer;
    private float nextAttackTime;
    private float nextRetreatTime;
    private float retreatStartTime;
    private float retreatEndTime;
    private float nextRepathTime;
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool shotThisAttack;
    private Coroutine attackRoutine;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        facingDirection = transform.forward;

        // EnemyBase moves the dynamic Rigidbody using agent.desiredVelocity.
        rotateToMovement = false; // This subclass handles facing.
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.15f;
        }
    }

    protected override void Start()
    {
        base.Start();
        state = hasPatrol && patrolZones != null && patrolZones.Length > 0
            ? State.Patrol : State.Idle;
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
        TrackRetreatPressure(); // Also counts during attacks and normal recoil.

        if (agent == null || !agent.enabled || !agent.isOnNavMesh || isInReaction)
            return;

        if (!detectedPlayer)
        {
            if (state == State.Attack) CancelAttack(true);
            if (state != State.Idle && state != State.Patrol && state != State.ReturnHome)
                state = State.ReturnHome;
            UpdatePassiveMovement();
        }
        else if (state == State.Attack)
        {
            if (!shotThisAttack) FacePlayer();
        }
        else if (state == State.Retreat)
        {
            UpdateRetreat();
        }
        else
        {
            UpdateCombat();
        }

        if (state != State.Attack)
            UpdateLocomotionAnimation();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (rb == null || isDead || isStunned || isStaggered || isInReaction) return;

        Vector3 direction = Flat(facingDirection);
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.RotateTowards(
            rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
    }

    private void OnValidate()
    {
        maxRetreatDuration = Mathf.Max(minRetreatDuration, maxRetreatDuration);
        preferredAttackDistance = Mathf.Min(preferredAttackDistance, attackRange);
        retreatSafeDistance = Mathf.Max(retreatSafeDistance, retreatTriggerDistance + 0.1f);
    }

    // Detection is limited by the home leash, even when instantDetection is on.
    private Vector3 Home => HomePoint != null ? HomePoint.position : startPosition;

    private void DetectPlayer()
    {
        if (!HasValidPlayer() || FlatDistance(player.transform.position, Home) > maxChaseDistance)
        {
            detectedPlayer = false;
            detectionTimer = 0f;
            return;
        }

        if (detectedPlayer) return;
        if (instantDetection ||
            (FlatDistance(transform.position, player.transform.position) <= detectionRange && ClearShot()))
        {
            detectionTimer += Time.deltaTime;
            if (instantDetection || detectionTimer >= detectionDelay)
                detectedPlayer = true;
        }
        else detectionTimer = 0f;
    }

    private bool ClearShot()
    {
        if (!HasValidPlayer()) return false;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * targetRayHeight;
        Vector3 direction = player.transform.position + Vector3.up * targetRayHeight - origin;
        float distance = direction.magnitude;
        if (distance < 0.01f) return true;
        if (Physics.Raycast(origin, direction / distance, out RaycastHit hit,
                distance, obstacleLayer, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<PlayerControl>() != null;
        return true;
    }

    private void TrackRetreatPressure()
    {
        if (detectedPlayer && state != State.Retreat && HasValidPlayer() &&
            Time.time >= nextRetreatTime &&
            FlatDistance(transform.position, player.transform.position) <= retreatTriggerDistance)
            closeTimer += Time.deltaTime;
        else
            closeTimer = 0f;
    }

    private void UpdateCombat()
    {
        float distance = FlatDistance(transform.position, player.transform.position);

        if (Time.time >= nextRetreatTime && closeTimer >= retreatPreparation &&
            distance <= retreatTriggerDistance && StartRetreat())
            return;

        // No separate Reposition state: approaching uses NavMesh to navigate
        // around obstacles until the goblin has a clear shot.
        if (distance > attackRange ||
            (state == State.Approach && distance > preferredAttackDistance) ||
            !ClearShot())
        {
            state = State.Approach;
            MoveTo(player.transform.position);
            FaceMovement();
            return;
        }

        state = State.Hold;
        StopMoving();
        FacePlayer();
        if (Time.time >= nextAttackTime && IsFacingPlayer())
            StartAttack();
    }

    private bool StartRetreat()
    {
        closeTimer = 0f;
        if (!FindRetreatDestination(out retreatDestination))
        {
            nextRetreatTime = Time.time + 0.5f; // Retry later; never get stuck retreating.
            return false;
        }

        float duration = Random.Range(minRetreatDuration, maxRetreatDuration);
        retreatStartTime = Time.time;
        retreatEndTime = Time.time + duration;
        state = State.Retreat;
        MoveTo(retreatDestination);
        FaceMovement();
        return true;
    }

    private void UpdateRetreat()
    {
        float distance = FlatDistance(transform.position, player.transform.position);
        bool minimumElapsed = Time.time - retreatStartTime >= minRetreatDuration;
        bool arrived = FlatDistance(transform.position, retreatDestination) <= arrivalDistance;

        if (Time.time >= retreatEndTime ||
            (minimumElapsed && (arrived || distance >= retreatSafeDistance)))
        {
            state = State.Hold;
            nextRetreatTime = Time.time + retreatCooldown;
            StopMoving();
            return;
        }

        MoveTo(retreatDestination);
        FaceMovement();
    }

    // Only searches when a retreat starts; no repeated pathfinding every frame.
    private bool FindRetreatDestination(out Vector3 bestDestination)
    {
        bestDestination = transform.position;
        if (agent == null || !agent.isOnNavMesh || !HasValidPlayer()) return false;

        if (!NavMesh.SamplePosition(agent.nextPosition, out NavMeshHit start,
                1.5f, agent.areaMask)) return false;

        Vector3 away = Flat(transform.position - player.transform.position).normalized;
        if (away.sqrMagnitude < 0.01f) away = Flat(transform.forward).normalized;
        float currentDistance = FlatDistance(transform.position, player.transform.position);
        float bestGain = 0.3f;
        NavMeshPath path = new NavMeshPath();

        foreach (float angle in RetreatAngles)
        {
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * away;
            Vector3 requested = transform.position + direction * retreatTravelDistance;
            if (!NavMesh.SamplePosition(requested, out NavMeshHit target,
                    1.5f, agent.areaMask)) continue;

            float gain = FlatDistance(target.position, player.transform.position) - currentDistance;
            if (gain <= bestGain) continue;
            if (!NavMesh.CalculatePath(start.position, target.position, agent.areaMask, path) ||
                path.status != NavMeshPathStatus.PathComplete) continue;

            bestGain = gain;
            bestDestination = target.position;
        }
        return bestGain > 0.3f;
    }

    private void UpdatePassiveMovement()
    {
        if (state == State.ReturnHome)
        {
            if (FlatDistance(transform.position, Home) > arrivalDistance)
            {
                MoveTo(Home);
                FaceMovement();
                return;
            }
            state = hasPatrol && patrolZones != null && patrolZones.Length > 0
                ? State.Patrol : State.Idle;
        }

        if (!hasPatrol || patrolZones == null || patrolZones.Length == 0)
        {
            state = State.Idle;
            StopMoving();
            return;
        }

        state = State.Patrol;
        patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolZones.Length - 1);
        Transform point = patrolZones[patrolIndex];
        if (point == null) return;
        MoveTo(point.position);
        FaceMovement();
        if (FlatDistance(transform.position, point.position) <= arrivalDistance && patrolZones.Length > 1)
        {
            patrolIndex += patrolDirection;
            if (patrolIndex >= patrolZones.Length) { patrolIndex = patrolZones.Length - 2; patrolDirection = -1; }
            else if (patrolIndex < 0) { patrolIndex = 1; patrolDirection = 1; }
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        if (Time.time < nextRepathTime) return;
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

    private void StartAttack()
    {
        if (anim == null || stats == null || projectilePrefab == null || firePoint == null)
            return;
        state = State.Attack;
        shotThisAttack = false;
        StopMoving();
        attackRoutine = StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        // PlayAnim manages EnemyBase's animation hash. Explicitly restart at 0
        // so this also works with older versions of EnemyBase.PlayAnim().
        PlayAnim("Attack");
        anim.Play(AttackHash, 0, 0f);
        yield return null;

        float elapsed = 0f;
        bool enteredAttack = false;
        while (state == State.Attack && !isDead && !isStunned && !isStaggered)
        {
            AnimatorStateInfo current = anim.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == AttackHash)
            {
                enteredAttack = true;
                float remaining = Mathf.Max(0f, 1f - current.normalizedTime) *
                    current.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                if (remaining <= attackExitBlend) break;
            }
            else if (enteredAttack) break;

            elapsed += Time.deltaTime;
            if (elapsed >= attackSafetyTimeout) break; // Missing state/animation protection.
            if (!shotThisAttack) FacePlayer();
            yield return null;
        }

        if (state != State.Attack || isDead || isStunned || isStaggered) yield break;
        if (!shotThisAttack)
            Debug.LogWarning($"[Goblin_Range] {name}: Attack finished without Shoot Animation Event.", this);
        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackHash)
            anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);

        yield return new WaitForSeconds(Mathf.Max(attackRecovery, attackExitBlend));
        if (state != State.Attack || isDead || isStunned || isStaggered) yield break;

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
        state = State.Hold;
    }

    // Called by the Attack clip's Animation Event (via the child Animator relay).
    public void Shoot()
    {
        if (state != State.Attack || shotThisAttack || isDead || isStunned ||
            isStaggered || !HasValidPlayer()) return;

        shotThisAttack = true;
        if (projectilePrefab == null || firePoint == null || stats == null) return;

        Vector3 direction = player.transform.position + Vector3.up * targetRayHeight - firePoint.position;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();

        GameObject instance = Instantiate(projectilePrefab, firePoint.position,
            Quaternion.LookRotation(direction));
        if (instance.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(stats.damage, direction);
    }

    private void CancelAttack(bool blendToIdle)
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
        if (state != State.Attack) return;
        nextAttackTime = Time.time + attackCooldown;
        state = State.Hold;
        if (blendToIdle && anim != null && !isDead)
            anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);
    }

    private void FacePlayer()
    {
        if (HasValidPlayer()) SetFacing(player.transform.position - transform.position);
    }

    private void FaceMovement()
    {
        if (agent != null && agent.desiredVelocity.sqrMagnitude > 0.01f)
            SetFacing(agent.desiredVelocity);
    }

    private void SetFacing(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude > 0.001f)
            facingDirection = direction.normalized;
    }

    private bool IsFacingPlayer()
    {
        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        return toPlayer.sqrMagnitude < 0.001f ||
            Vector3.Angle(Flat(transform.forward), toPlayer) <= attackFacingAngle;
    }

    private void UpdateLocomotionAnimation()
    {
        if (anim == null) return;
        Vector3 horizontal = rb != null ? Flat(rb.linearVelocity) : Vector3.zero;
        string animation = agent != null && !agent.isStopped && horizontal.sqrMagnitude > 0.09f
            ? "Walk" : "Idle";
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(animation) ||
            (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(animation))) return;
        anim.CrossFadeInFixedTime(animation, 0.12f, 0, 0f);
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    private static float FlatDistance(Vector3 a, Vector3 b) { return Flat(a - b).magnitude; }
}



