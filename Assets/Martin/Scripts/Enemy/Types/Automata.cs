using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Automata : EnemyBase
{
    private enum State { Idle, Patrol, Chase, ReturnHome, Burst, DashSpread }

    [Header("Projectile / Targeting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Range(1f, 90f)] private float facingAngleThreshold = 20f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Attack Selection / Timing")]
    [SerializeField, Range(0f, 1f)] private float burstChance = 0.6f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.2f;
    [SerializeField, Range(0f, 0.25f)] private float exitBlend = 0.08f;
    [SerializeField, Min(0.5f)] private float animationSafetyTimeout = 4f;

    [Header("Attack 1 - Disparo_1 (Stationary Burst)")]
    [SerializeField, Min(1)] private int burstCount = 3;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.12f;

    [Header("Attack 2 - Dash -> Disparo_3 (Spread)")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 10f;
    [SerializeField, Min(0.1f)] private float dashMaxDistance = 4f;

    [Tooltip("Minimum distance between the enemy root and player root after a dash. Increase for large colliders.")]
    [SerializeField, Min(0.1f)] private float dashStopDistance = 2f;
    [SerializeField, Min(1)] private int spreadCount = 5;
    [Tooltip("Total horizontal cone angle (degrees), not the half-angle.")]
    [SerializeField, Range(0f, 180f)] private float spreadConeAngle = 35f;

    [Header("Dash Safety")]
    [Tooltip("Walls/solid props, but NOT floor, player or this enemy.")]
    [SerializeField] private LayerMask dashBlockingLayers;
    [SerializeField, Min(0.05f)] private float dashCastRadius = 0.35f;
    [SerializeField, Min(0.1f)] private float dashCastHeight = 1f;
    [SerializeField, Min(0f)] private float dashWallPadding = 0.12f;
    [SerializeField, Min(0.05f)] private float dashEdgeLookahead = 0.35f;
    [SerializeField, Min(0.1f)] private float dashGroundProbeHeight = 1f;
    [SerializeField, Min(0.1f)] private float dashGroundProbeDepth = 2f;
    [SerializeField, Min(0f)] private float dashMaxGroundHeightDelta = 0.5f;

    [Header("Detection / Patrol")]
    [SerializeField] private bool instantDetection;
    [SerializeField, Min(0.1f)] private float detectionRange = 9f;
    [SerializeField, Min(0.1f)] private float maxChaseDistance = 15f;
    [SerializeField, Min(0f)] private float detectionDelay = 0.25f;
    [SerializeField, Min(0.1f)] private float patrolArrivalDistance = 0.6f;

    [Header("Navigation / Facing")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
    [Tooltip("Slerp responsiveness. A value near 12 matches the old Automata turning style.")]
    [SerializeField, Min(0.1f)] private float turnSpeed = 12f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

    [Header("Runtime (Debug)")]
    [SerializeField] private State state;

    private const string BurstAnimation = "Disparo_1";
    private const string DashAnimation = "Dash";
    private const string SpreadAnimation = "Disparo_3";

    private Vector3 homePosition;
    private Vector3 desiredFacing;
    private Vector3 dashDirection;
    private Vector3 dashStartPosition;
    private Vector3 lastDestination;
    private float detectionTimer;
    private float nextAttackTime;
    private float nextRepathTime;
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool detectedPlayer;
    private bool dashMoving;
    private bool burstEventUsed;
    private bool spreadEventUsed;
    private Coroutine attackRoutine;
    private Coroutine burstRoutine;

    private bool IsAttacking => state == State.Burst || state == State.DashSpread;
    private Vector3 Home => HomePoint != null ? HomePoint.position : homePosition;

    protected override void Awake()
    {
        base.Awake();
        homePosition = transform.position;
        desiredFacing = Flat(transform.forward);
        rotateToMovement = false;

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.2f;
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
            CancelAttack();
            return;
        }

        DetectPlayer();
        if (IsAttacking) return;
        if (isInReaction || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (detectedPlayer) UpdateChase();
        else UpdatePassiveMovement();

        if (!IsAttacking) UpdateLocomotionAnimation();
    }

    protected override void FixedUpdate()
    {
        if (dashMoving && CanAct() && !isInReaction && (!isGroundEnemy || isGrounded) &&
            agent != null && agent.enabled && agent.isOnNavMesh)
        {
            MoveDash();
            return;
        }

        base.FixedUpdate();
        if (rb == null || isDead || isStunned || isStaggered || isInReaction) return;

        Vector3 direction = Flat(desiredFacing);

        if (direction.sqrMagnitude < 0.001f) return;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(direction),
            Mathf.Clamp01(turnSpeed * Time.fixedDeltaTime)));
    }

    protected override void OnDisable()
    {
        CancelAttack();
        base.OnDisable();
    }

    protected override void Die()
    {
        CancelAttack();
        base.Die();
    }
    // Detection, chase, patrol
    private void DetectPlayer()
    {
        bool valid = HasValidPlayer() &&
            Flat(player.transform.position - Home).sqrMagnitude <= maxChaseDistance * maxChaseDistance;

        if (!valid)
        {
            detectionTimer = 0f;
            detectedPlayer = false;
            if (IsAttacking) CancelAttack();
            if (state == State.Chase) state = State.ReturnHome;
            return;
        }

        if (detectedPlayer) return;
        bool inSight = Flat(player.transform.position - transform.position).sqrMagnitude <=
            detectionRange * detectionRange && HasLineOfSight();

        if (instantDetection || inSight)
        {
            detectionTimer += Time.deltaTime;
            if (instantDetection || detectionTimer >= detectionDelay)
            {
                detectedPlayer = true;
                state = State.Chase;
                detectionTimer = 0f;
            }
        }
        else detectionTimer = 0f;
    }

    private bool HasLineOfSight()
    {
        if (!HasValidPlayer()) return false;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * targetRayHeight;
        Vector3 target = player.transform.position + Vector3.up * targetRayHeight;
        return !Physics.Linecast(origin, target, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private void UpdateChase()
    {
        state = State.Chase;
        Vector3 toward = Flat(player.transform.position - transform.position);
        bool canShoot = toward.sqrMagnitude <= attackRange * attackRange && HasLineOfSight();

        if (!canShoot)
        {
            MoveTo(player.transform.position);
            SetFacing(agent.desiredVelocity);
            return;
        }

        StopNavigation();
        SetFacing(toward);
        if (Time.time < nextAttackTime || !IsFacingPlayer()) return;

        bool canDash = toward.magnitude > dashStopDistance + 0.5f;
        StartAttack(!canDash || Random.value < burstChance);
    }

    private void UpdatePassiveMovement()
    {
        if (state == State.ReturnHome)
        {
            if (Flat(Home - transform.position).sqrMagnitude >
                patrolArrivalDistance * patrolArrivalDistance)
            {
                MoveTo(Home);
                SetFacing(agent.desiredVelocity);
                return;
            }
            state = HasPatrol() ? State.Patrol : State.Idle;
        }

        if (HasPatrol())
        {
            state = State.Patrol;
            patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolZones.Length - 1);
            Transform waypoint = patrolZones[patrolIndex];

            if (waypoint == null) { StopNavigation(); return; }

            if (Flat(waypoint.position - transform.position).sqrMagnitude <=
                patrolArrivalDistance * patrolArrivalDistance)
            {
                if (patrolZones.Length > 1)
                {
                    patrolIndex += patrolDirection;
                    if (patrolIndex >= patrolZones.Length) { patrolIndex = patrolZones.Length - 2; patrolDirection = -1; }
                    else if (patrolIndex < 0) { patrolIndex = 1; patrolDirection = 1; }
                }
                waypoint = patrolZones[patrolIndex];
            }
            if (waypoint != null)
            {
                MoveTo(waypoint.position);
                SetFacing(agent.desiredVelocity);
            }
        }
        else { state = State.Idle; StopNavigation(); }
    }

    private bool HasPatrol() => hasPatrol && patrolZones != null && patrolZones.Length > 0;

    private void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (agent.isStopped) agent.isStopped = false;

        if (!agent.hasPath || Time.time >= nextRepathTime ||
            Flat(destination - lastDestination).sqrMagnitude > 0.5f * 0.5f)
        {
            agent.SetDestination(destination);
            lastDestination = destination;
            nextRepathTime = Time.time + repathInterval;
        }
    }

    private void StopNavigation()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = true;

        if (agent.hasPath) agent.ResetPath();
    }
    // Attack sequencing: clip timings, not hardcoded delays
    private void StartAttack(bool burst)
    {
        if (!CanAct() || anim == null || projectilePrefab == null || stats == null) return;

        if (!burst && rb == null) burst = true;

        state = burst ? State.Burst : State.DashSpread;
        burstEventUsed = false;
        spreadEventUsed = false;
        StopNavigation();
        SetFacing(player.transform.position - transform.position);
        attackRoutine = StartCoroutine(PerformAttack(burst));
    }

    private IEnumerator PerformAttack(bool burst)
    {
        if (burst)
        {
            PlayState(BurstAnimation);
            yield return WaitForAnimation(BurstAnimation, State.Burst, true);
            if (!CanAct() || state != State.Burst) yield break;
        }
        else
        {
            dashDirection = Flat(player.transform.position - transform.position).normalized;
            if (dashDirection.sqrMagnitude < 0.001f) dashDirection = Flat(transform.forward).normalized;

            if (dashDirection.sqrMagnitude < 0.001f) dashDirection = Vector3.forward;

            SetFacing(dashDirection);
            dashStartPosition = rb.position;
            PlayState(DashAnimation);
            dashMoving = true;

            yield return WaitForAnimation(DashAnimation, State.DashSpread, false);
            StopDashMovement();

            if (!CanAct() || state != State.DashSpread) yield break;

            SetFacing(player.transform.position - transform.position);
            anim.CrossFadeInFixedTime(SpreadAnimation, 0.06f, 0, 0f);
            yield return WaitForAnimation(SpreadAnimation, State.DashSpread, true);

            if (!CanAct() || state != State.DashSpread) yield break;
        }

        if (anim != null && anim.isActiveAndEnabled) anim.CrossFadeInFixedTime("Idle", exitBlend, 0, 0f);

        if (burst) while (burstRoutine != null && CanAct() && state == State.Burst) yield return null;

        if (recoveryTime > 0f) yield return new WaitForSeconds(recoveryTime);

        FinishAttack();
    }

    private void PlayState(string stateName)
    {
        PlayAnim(stateName);
        anim.Play(Animator.StringToHash(stateName), 0, 0f);
    }

    private IEnumerator WaitForAnimation(string stateName, State expectedState, bool blendBeforeEnd)
    {
        int hash = Animator.StringToHash(stateName);
        bool entered = false;
        float elapsed = 0f;

        yield return null; 

        while (state == expectedState && CanAct() && elapsed < animationSafetyTimeout)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == hash)
            {
                entered = true;
                float length = info.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                float timeRemaining = Mathf.Max(0f, 1f - info.normalizedTime) * length;

                if (info.normalizedTime >= 1f || (blendBeforeEnd && timeRemaining <= exitBlend))
                    yield break;
            }
            else if (entered) yield break;
            else if (elapsed > 0.4f) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!entered && anim != null && isActiveAndEnabled)
            Debug.LogWarning($"[Automata] Animator state '{stateName}' was not found on layer 0.", this);
    }

    private bool CanAct() => !isDead && !isStunned && !isStaggered && HasValidPlayer();

    private void FinishAttack()
    {
        StopDashMovement();
        if (burstRoutine != null) { StopCoroutine(burstRoutine); burstRoutine = null; }

        attackRoutine = null;
        nextAttackTime = Time.time + attackCooldown;

        if (IsAttacking) state = HasValidPlayer() ? State.Chase : State.ReturnHome;
    }

    private void CancelAttack()
    {
        if (!IsAttacking) return;

        if (attackRoutine != null) StopCoroutine(attackRoutine);

        FinishAttack();
    }
    // Animation Events (on the Animator GameObject or via relay)
    /// <summary>ONE event on Disparo_1; fires the configured burst over time.</summary>
    public void ShootBurst()
    {
        if (state != State.Burst || burstEventUsed || !CanAct() || projectilePrefab == null) return;

        burstEventUsed = true;
        burstRoutine = StartCoroutine(BurstShots());
    }

    private IEnumerator BurstShots()
    {
        int count = Mathf.Max(1, burstCount);
        for (int i = 0; i < count && state == State.Burst && CanAct(); i++)
        {
            FireProjectile(AimAtPlayer());
            if (i + 1 < count) yield return new WaitForSeconds(Mathf.Max(0.01f, burstInterval));
            else yield return null; 
        }
        burstRoutine = null;
    }
    public void ShootSpread()
    {
        if (state != State.DashSpread || dashMoving || spreadEventUsed ||
            !CanAct() || projectilePrefab == null) return;
        spreadEventUsed = true;
        Vector3 forward = AimAtPlayer();
        int count = Mathf.Max(1, spreadCount);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
            Vector3 direction = Quaternion.AngleAxis(t * spreadConeAngle, Vector3.up) * forward;
            FireProjectile(direction);
        }
    }

    private Vector3 AimAtPlayer()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 target = player.transform.position + Vector3.up * targetRayHeight;
        Vector3 direction = target - origin;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private void FireProjectile(Vector3 direction)
    {
        if (projectilePrefab == null || stats == null) return;

        Vector3 position = firePoint != null ? firePoint.position : transform.position;

        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;

        direction.Normalize();
        GameObject projectileObject = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        E_Projectile projectile = projectileObject.GetComponent<E_Projectile>();

        if (projectile != null) projectile.InitProj(stats.damage, direction);
    }
    private void MoveDash()
    {
        if (rb == null || !HasValidPlayer()) { StopDashMovement(); return; }

        float moved = Mathf.Max(0f, Vector3.Dot(Flat(rb.position - dashStartPosition), dashDirection));
        float roomToPlayer = Flat(player.transform.position - rb.position).magnitude - dashStopDistance;
        float allowedStep = Mathf.Min(dashSpeed * Time.fixedDeltaTime, dashMaxDistance - moved, roomToPlayer);

        if (allowedStep <= 0.005f || !HasSafeDashGround(allowedStep) || DashWallAhead(allowedStep))
        { StopDashMovement(); return; }

        rb.MoveRotation(Quaternion.LookRotation(dashDirection));
        Vector3 currentHorizontal = Flat(rb.linearVelocity);
        Vector3 desiredHorizontal = dashDirection * (allowedStep / Time.fixedDeltaTime);
        rb.AddForce(desiredHorizontal - currentHorizontal, ForceMode.VelocityChange);
    }

    private bool HasSafeDashGround(float step)
    {
        Vector3 future = rb.position + dashDirection * (step + dashEdgeLookahead);

        if (agent == null || !agent.enabled || !agent.isOnNavMesh ||
            NavMesh.Raycast(rb.position, future, out _, agent.areaMask)) return false;

        if (!NavMesh.SamplePosition(future, out NavMeshHit point, 0.45f, agent.areaMask) ||
            Flat(point.position - future).magnitude > 0.35f ||
            Mathf.Abs(point.position.y - rb.position.y) > dashMaxGroundHeightDelta)
            return false;

        Vector3 origin = future + Vector3.up * dashGroundProbeHeight;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground,
                dashGroundProbeHeight + dashGroundProbeDepth, whatIsGround,
                QueryTriggerInteraction.Ignore)) return false;
        return Mathf.Abs(ground.point.y - rb.position.y) <= dashMaxGroundHeightDelta;
    }

    private bool DashWallAhead(float step)
    {
        Vector3 origin = rb.position + Vector3.up * dashCastHeight;
        return Physics.SphereCast(origin, dashCastRadius, dashDirection, out _,
                   step + dashWallPadding, dashBlockingLayers, QueryTriggerInteraction.Ignore) ||
               Physics.CheckSphere(origin + dashDirection * (step + dashWallPadding),
                   dashCastRadius, dashBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    private void StopDashMovement()
    {
        if (!dashMoving) return;

        dashMoving = false;

        if (rb == null || isInReaction) return;

        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    private void SetFacing(Vector3 dir)
    {
        dir = Flat(dir);

        if (dir.sqrMagnitude > 0.001f) desiredFacing = dir.normalized;
    }

    private bool IsFacingPlayer()
    {
        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        return toPlayer.sqrMagnitude < 0.001f || 
            Vector3.Angle(Flat(transform.forward), toPlayer) <= facingAngleThreshold;
    }

    private void UpdateLocomotionAnimation()
    {
        if (anim == null || !anim.isActiveAndEnabled) return;

        bool walking = agent != null && !agent.isStopped && rb != null &&
            Flat(rb.linearVelocity).sqrMagnitude > 0.09f;
        string clipName = walking ? "Walk" : "Idle";

        if (anim.GetCurrentAnimatorStateInfo(0).IsName(clipName) ||
            (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(clipName))) return;
        anim.CrossFadeInFixedTime(clipName, 0.12f, 0, 0f);
    }

    private static Vector3 Flat(Vector3 vector) { vector.y = 0f; return vector; }

}
