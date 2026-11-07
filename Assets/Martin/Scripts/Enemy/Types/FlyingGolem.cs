using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FlyingGolem : EnemyBase
{
    private enum AIState { Idle, Patrol, Chase, Attack, ReturnHome }
    private enum FlightState { Hover, Falling, Recovering }
    private enum AttackType { None, MagicVolley, Bomb }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 18f;
    [SerializeField] private float detectionDelay = 0.2f;
    [SerializeField] private float maxChaseDistance = 28f;
    [SerializeField] private bool instantDetection;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Navigation / Combat")]
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float facingAngle = 15f;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float stoppingDistance = 0.75f;
    [Tooltip("Slerp responsiveness (like the original script), not degrees/second.")]
    [SerializeField] private float turnSpeed = 15f;
    [SerializeField] private float destinationRefresh = 0.2f;
    [SerializeField] private float patrolStopDistance = 1.5f;

    [Header("Hover")]
    [Tooltip("Child Model Transform. Do not assign the enemy root itself.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float hoverHeight = 4f;
    [SerializeField] private float hoverBobAmplitude = 0.2f;
    [SerializeField] private float hoverBobSpeed = 2f;
    [SerializeField] private float hoverSmooth = 8f;
    [Tooltip("Optional; otherwise found on the same root as this script.")]
    [SerializeField] private CapsuleCollider bodyCapsule;
    [SerializeField] private BoxCollider bodyBox;

    [Header("Attacks")]
    [Range(0f, 1f)][SerializeField] private float magicVolleyChance = 0.6f;
    [SerializeField] private string magicVolleyState = "Bomb";
    [SerializeField] private string bombState = "Attack 0";
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRecovery = 0.5f;
    [SerializeField] private float attackExitBlend = 0.08f;
    [SerializeField] private float attackSafetyTimeout = 4f;

    [Header("Magic Volley - Animation Events")]
    [SerializeField] private GameObject magicCircleVfx;
    [SerializeField] private Vector3 circleOffset = new Vector3(1f, 0f, 0.5f);
    [SerializeField] private GameObject magicBulletPrefab;
    [Min(1)][SerializeField] private int bulletAmount = 3;
    [Min(0f)][SerializeField] private float timeBetweenShots = 0.2f;

    [Header("Bomb - Animation Events")]
    [SerializeField] private GameObject chargeVfx;
    [SerializeField] private Vector3 bombPositionOffset;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float bombSpeed = 12f;

    [Header("Stagger Fall / Recovery")]
    [SerializeField] private float minimumFallTime = 0.35f;
    [SerializeField] private float landedHoldTime = 0.15f;
    [SerializeField] private float maxFallTime = 3f;
    [SerializeField] private float riseDuration = 0.35f;
    [SerializeField] private float recoveryNavMeshRadius = 7f;

    private AIState aiState;
    private FlightState flightState;
    private AttackType currentAttack;
    private Coroutine attackRoutine;
    private Coroutine volleyRoutine;
    private Coroutine riseRoutine;

    private Vector3 homePosition;
    private Vector3 desiredFacing;
    private Vector3 initialVisualLocalPosition;
    private Vector3 initialCapsuleCenter;
    private Vector3 initialBoxCenter;
    private Vector3 leftCirclePosition;
    private Vector3 rightCirclePosition;
    private Collider mainBodyCollider;

    private bool hasTarget;
    private bool circlesSpawned;
    private bool volleyStarted;
    private bool bombThrown;
    private bool chargeSpawned;
    private bool endAttackRequested;
    private float detectionTimer;
    private float nextDestinationTime;
    private float nextAttackTime;
    private float hoverClock;
    private float fallStartedAt;
    private float landedTimer;
    private int patrolIndex;
    private int patrolDirection = 1;

    // -----------------------------------------------------------------
    // Setup: all movement components must be on ONE root.
    // -----------------------------------------------------------------
    protected override void Awake()
    {
        isGroundEnemy = false;
        rotateToMovement = false; // We turn the Rigidbody ourselves.
        base.Awake();

        if (agent == null || rb == null || agent.transform != transform || rb.transform != transform)
        {
            Debug.LogError("[FlyingGolem] Put FlyingGolem, Rigidbody and NavMeshAgent on the SAME root.", this);
            enabled = false;
            return;
        }

        if (visualRoot == null && anim != null)
            visualRoot = anim.transform;
        if (visualRoot == null || visualRoot == transform)
        {
            Debug.LogError("[FlyingGolem] Assign a child Model as Visual Root.", this);
            enabled = false;
            return;
        }

        // Do not use colliders from a moving visual child as the main body.
        if (bodyCapsule == null || bodyCapsule.transform != transform)
            bodyCapsule = GetComponent<CapsuleCollider>();
        if (bodyBox == null || bodyBox.transform != transform)
            bodyBox = GetComponent<BoxCollider>();
        mainBodyCollider = bodyCapsule != null ? (Collider)bodyCapsule : bodyBox;
        if (mainBodyCollider == null)
        {
            Debug.LogError("[FlyingGolem] A CapsuleCollider or BoxCollider is required on the root.", this);
            enabled = false;
            return;
        }

        homePosition = transform.position;
        desiredFacing = transform.forward;
        initialVisualLocalPosition = visualRoot.localPosition;
        if (bodyCapsule != null) initialCapsuleCenter = bodyCapsule.center;
        if (bodyBox != null) initialBoxCenter = bodyBox.center;

        // The collider stays at the same hovering height, without following
        // the small visual bob every frame (less physics jitter).
        if (bodyCapsule != null) bodyCapsule.center = initialCapsuleCenter + Vector3.up * hoverHeight;
        if (bodyBox != null) bodyBox.center = initialBoxCenter + Vector3.up * hoverHeight;

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;

        EnterHoverPhysics();
    }

    protected override void Start()
    {
        base.Start();
        if (enabled) UpdateHoverVisual(true);
    }

    protected override void Update()
    {
        if (isDead || !enabled) return;
        base.Update(); // Same Rigidbody/NavMesh sync as the other enemies.

        if (flightState == FlightState.Hover)
            UpdateHoverVisual();
        else if (flightState == FlightState.Falling)
        {
            CheckFallRecovery();
            return;
        }
        else return; // Recovering.

        if (isStunned || isStaggered || isInReaction)
        {
            if (aiState == AIState.Attack) CancelAttack();
            return;
        }
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        DetectPlayer();
        if (aiState == AIState.Attack) return;

        if (hasTarget) UpdateCombat();
        else UpdatePatrolOrReturn();

        // Never overwrite the Attack animation on the frame it begins.
        if (aiState != AIState.Attack)
            PlayLoop(Flat(rb.linearVelocity).sqrMagnitude > 0.04f ? "Patrol" : "Idle");
    }

    protected override void FixedUpdate()
    {
        if (isDead || rb == null) return;

        if (flightState == FlightState.Falling)
        {
            // Preserve EnemyBase's reduced gravity during air juggling,
            // without letting navigation brake a falling enemy.
            if (isAirHung && rb.linearVelocity.y < 0f)
                rb.AddForce(-Physics.gravity * (1f - airGravityMultiplier), ForceMode.Acceleration);
            return;
        }
        if (flightState != FlightState.Hover) return;

        base.FixedUpdate(); // Rigidbody follows NavMesh desiredVelocity.
        if (isInReaction && currentReaction == EnemyReactionType.Push)
        {
            // Hover enemies have no ground contact, so apply recoil braking here.
            Vector3 horizontal = Flat(rb.linearVelocity);
            rb.AddForce(-horizontal * groundPushBraking, ForceMode.Acceleration);
        }

        if (isStunned || isStaggered || isInReaction) return;
        Vector3 direction = Flat(desiredFacing);
        if (direction.sqrMagnitude < 0.001f) return;
        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation, Quaternion.LookRotation(direction.normalized),
            Mathf.Clamp01(turnSpeed * Time.fixedDeltaTime)));
    }

    // The root sits at ground-level NavMesh height while its collider floats.
    // Ground detection must follow the collider when the ROOT falls.
    protected override void CheckGround()
    {
        if (flightState != FlightState.Falling || mainBodyCollider == null)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = mainBodyCollider.bounds;
        Vector3 origin = bounds.center - Vector3.up * (bounds.extents.y - 0.1f);
        isGrounded = Physics.Raycast(origin, Vector3.down,
            groundCheckDistance + 0.2f, whatIsGround, QueryTriggerInteraction.Ignore);
    }

    // Critical: EnemyBase must NOT re-enable navigation before rising.
    protected override void EnableAgentIfNeeded()
    {
        if (flightState == FlightState.Hover)
            base.EnableAgentIfNeeded();
    }

    protected override void TriggerStagger()
    {
        if (isStaggered) return;
        CancelAttack();
        if (!immuneToMovement) EnterFallPhysics(); // Before attack forces apply.
        base.TriggerStagger();
    }

    // -----------------------------------------------------------------
    // Detection and navigation
    // -----------------------------------------------------------------
    private void DetectPlayer()
    {
        if (!HasValidPlayer())
        {
            hasTarget = false;
            detectionTimer = 0f;
            if (aiState == AIState.Attack) CancelAttack();
            return;
        }

        Vector3 home = HomePoint != null ? HomePoint.position : homePosition;
        if (hasTarget && HorizontalDistance(transform.position, home) > maxChaseDistance)
        {
            hasTarget = false;
            detectionTimer = 0f;
            aiState = AIState.ReturnHome;
            return;
        }
        if (aiState == AIState.ReturnHome || hasTarget) return;

        bool seen = HorizontalDistance(transform.position, player.transform.position) <= detectionRange &&
                    HasClearShot();
        detectionTimer = seen ? detectionTimer + Time.deltaTime : 0f;
        if (instantDetection || detectionTimer >= detectionDelay)
        {
            hasTarget = true;
            aiState = AIState.Chase;
        }
    }

    private void UpdateCombat()
    {
        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        if (toPlayer.sqrMagnitude > attackRange * attackRange || !HasClearShot())
        {
            NavigateTo(player.transform.position);
            if (Flat(agent.desiredVelocity).sqrMagnitude > 0.01f)
                desiredFacing = Flat(agent.desiredVelocity).normalized;
            else if (toPlayer.sqrMagnitude > 0.01f)
                desiredFacing = toPlayer.normalized;
            aiState = AIState.Chase;
            return;
        }

        StopNavigation();
        if (toPlayer.sqrMagnitude > 0.01f)
            desiredFacing = toPlayer.normalized;
        if (Time.time >= nextAttackTime &&
            Vector3.Angle(Flat(transform.forward), toPlayer) <= facingAngle)
            StartAttack();
    }

    private void UpdatePatrolOrReturn()
    {
        Vector3 home = HomePoint != null ? HomePoint.position : homePosition;
        if (aiState == AIState.ReturnHome)
        {
            NavigateTo(home);
            if (HorizontalDistance(transform.position, home) <= patrolStopDistance)
                aiState = AIState.Idle;
        }
        else if (hasPatrol && patrolZones != null && patrolZones.Length > 0)
        {
            aiState = AIState.Patrol;
            Transform point = patrolZones[patrolIndex];
            if (point == null) { StopNavigation(); return; }
            NavigateTo(point.position);
            if (patrolZones.Length > 1 &&
                HorizontalDistance(transform.position, point.position) <= patrolStopDistance)
            {
                patrolIndex += patrolDirection;
                if (patrolIndex >= patrolZones.Length)
                { patrolIndex = patrolZones.Length - 2; patrolDirection = -1; }
                else if (patrolIndex < 0)
                { patrolIndex = 1; patrolDirection = 1; }
            }
        }
        else
        {
            aiState = AIState.Idle;
            StopNavigation();
        }

        if (agent != null && !agent.isStopped && Flat(agent.desiredVelocity).sqrMagnitude > 0.01f)
            desiredFacing = Flat(agent.desiredVelocity).normalized;
    }

    private void NavigateTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        if (Time.time < nextDestinationTime) return;
        nextDestinationTime = Time.time + destinationRefresh;
        agent.SetDestination(destination);
    }

    private void StopNavigation()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
    }

    private bool HasClearShot()
    {
        if (!HasValidPlayer()) return false;
        Vector3 origin = visualRoot.position + Vector3.up * targetRayHeight;
        Vector3 target = player.transform.position + Vector3.up * targetRayHeight;
        return !Physics.Linecast(origin, target, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    // -----------------------------------------------------------------
    // Animation-driven attacks (no Animator triggers or transition graph).
    // -----------------------------------------------------------------
    private void StartAttack()
    {
        if (anim == null || stats == null) return;
        aiState = AIState.Attack;
        StopNavigation();
        currentAttack = Random.value < magicVolleyChance ? AttackType.MagicVolley : AttackType.Bomb;
        circlesSpawned = volleyStarted = bombThrown = chargeSpawned = endAttackRequested = false;
        attackRoutine = StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        string state = currentAttack == AttackType.MagicVolley ? magicVolleyState : bombState;
        PlayAnim(state);
        yield return null;

        float elapsed = 0f;
        while (aiState == AIState.Attack && !endAttackRequested &&
               !isDead && !isStunned && !isStaggered)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            float remaining = Mathf.Max(0f, 1f - info.normalizedTime) *
                info.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
            if (remaining <= attackExitBlend) break;

            elapsed += Time.deltaTime;
            if (elapsed > attackSafetyTimeout) break;
            yield return null;
        }
        if (aiState != AIState.Attack || isDead || isStunned || isStaggered) yield break;

        anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);
        float wait = 0f;
        while (volleyRoutine != null && wait < 3f && !isDead && !isStunned && !isStaggered)
        {
            wait += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(attackRecovery);
        if (aiState != AIState.Attack || isDead || isStunned || isStaggered) yield break;

        attackRoutine = null;
        currentAttack = AttackType.None;
        aiState = AIState.Chase;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void CancelAttack()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        if (volleyRoutine != null) StopCoroutine(volleyRoutine);
        attackRoutine = volleyRoutine = null;
        if (aiState != AIState.Attack) return;
        currentAttack = AttackType.None;
        aiState = AIState.Chase;
        nextAttackTime = Time.time + attackCooldown;
        if (anim != null && !isDead) PlayAnim("Idle");
    }

    private bool CanReceiveEvent(AttackType attack) =>
        aiState == AIState.Attack && currentAttack == attack &&
        !isDead && !isStunned && !isStaggered && HasValidPlayer();

    public void SpawnMagicCircles()
    {
        if (!CanReceiveEvent(AttackType.MagicVolley) || circlesSpawned) return;
        circlesSpawned = true;
        Vector3 center = visualRoot.position + transform.forward * circleOffset.z +
                         Vector3.up * circleOffset.y;
        Vector3 side = transform.right * Mathf.Abs(circleOffset.x);
        leftCirclePosition = center - side;
        rightCirclePosition = center + side;
        SpawnVfx(magicCircleVfx, leftCirclePosition);
        SpawnVfx(magicCircleVfx, rightCirclePosition);
    }

    public void FireMagicVolley()
    {
        if (!CanReceiveEvent(AttackType.MagicVolley) || volleyStarted) return;
        if (!circlesSpawned) SpawnMagicCircles();
        volleyStarted = true;
        volleyRoutine = StartCoroutine(MagicVolleyRoutine());
    }

    // Compatibility with an older event name already placed on the clip.
    public void FireMagicBullet() => FireMagicVolley();

    private IEnumerator MagicVolleyRoutine()
    {
        yield return null;
        for (int i = 0; i < Mathf.Max(1, bulletAmount); i++)
        {
            if (!CanReceiveEvent(AttackType.MagicVolley)) break;
            SpawnBullet(leftCirclePosition);
            SpawnBullet(rightCirclePosition);
            if (i + 1 < bulletAmount && timeBetweenShots > 0f)
                yield return new WaitForSeconds(timeBetweenShots);
        }
        volleyRoutine = null;
    }

    public void SpawnChargeVfx()
    {
        if (!CanReceiveEvent(AttackType.Bomb) || chargeSpawned) return;
        chargeSpawned = true;
        SpawnVfx(chargeVfx, visualRoot.TransformPoint(bombPositionOffset));
    }

    public void ThrowBomb()
    {
        if (!CanReceiveEvent(AttackType.Bomb) || bombThrown || bombPrefab == null) return;
        bombThrown = true;
        Vector3 origin = visualRoot.TransformPoint(bombPositionOffset);
        Vector3 direction = AimFrom(origin);
        GameObject bomb = Instantiate(bombPrefab, origin, Quaternion.LookRotation(direction));
        if (bomb.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(stats.damage, direction);
        else if (bomb.TryGetComponent(out Rigidbody bombBody))
            bombBody.linearVelocity = direction * bombSpeed;
    }

    public void EndAttack()
    {
        if (aiState == AIState.Attack) endAttackRequested = true;
    }

    private void SpawnBullet(Vector3 origin)
    {
        if (magicBulletPrefab == null) return;
        Vector3 direction = AimFrom(origin);
        GameObject bullet = Instantiate(magicBulletPrefab, origin, Quaternion.LookRotation(direction));
        if (bullet.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(stats.damage, direction);
    }

    private Vector3 AimFrom(Vector3 origin)
    {
        Vector3 direction = HasValidPlayer()
            ? player.transform.position + Vector3.up * targetRayHeight - origin
            : transform.forward;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    private void SpawnVfx(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, transform.rotation);
        Destroy(effect, 5f);
    }

    // -----------------------------------------------------------------
    // Flight physics: keep root and raised collider together during a fall.
    // -----------------------------------------------------------------
    private void EnterHoverPhysics()
    {
        flightState = FlightState.Hover;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = (rb.constraints & ~(RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY)) |
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void UpdateHoverVisual(bool instant = false)
    {
        hoverClock += Time.deltaTime;
        float bob = Mathf.Sin(hoverClock * hoverBobSpeed) * hoverBobAmplitude;
        Vector3 target = initialVisualLocalPosition + Vector3.up * (hoverHeight + bob);
        visualRoot.localPosition = instant ? target : Vector3.Lerp(
            visualRoot.localPosition, target,
            1f - Mathf.Exp(-hoverSmooth * Time.deltaTime));
    }

    private void EnterFallPhysics()
    {
        if (flightState != FlightState.Hover) return;
        flightState = FlightState.Falling;
        fallStartedAt = Time.time;
        landedTimer = 0f;
        isGrounded = false;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.useGravity = true;
        // Preserve current velocity so player launch/knockdown applies naturally.
    }

    private void CheckFallRecovery()
    {
        if (isStaggered || isStunned || isInReaction || isAirHung) return;
        if (Time.time - fallStartedAt < minimumFallTime) return;
        landedTimer = isGrounded ? landedTimer + Time.deltaTime : 0f;
        if (landedTimer < landedHoldTime && Time.time - fallStartedAt < maxFallTime) return;

        if (!NavMesh.SamplePosition(rb.position, out NavMeshHit hit,
                recoveryNavMeshRadius, agent.areaMask)) return;
        if (riseRoutine == null) riseRoutine = StartCoroutine(RiseToHover(hit.position));
    }

    private IEnumerator RiseToHover(Vector3 navPosition)
    {
        flightState = FlightState.Recovering;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true; // Only during the controlled rise.

        Vector3 start = rb.position;
        float duration = Mathf.Max(0.01f, riseDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.fixedDeltaTime)
        {
            rb.MovePosition(Vector3.Lerp(start, navPosition, Mathf.Clamp01(elapsed / duration)));
            yield return new WaitForFixedUpdate();
        }
        if (isDead) yield break;

        rb.position = navPosition;
        hoverClock = 0f;
        EnterHoverPhysics();
        riseRoutine = null;
        EnableAgentIfNeeded(); // Reconnect only after the root reaches NavMesh height.
        aiState = hasTarget ? AIState.Chase : AIState.ReturnHome;
    }

    private static Vector3 Flat(Vector3 vector) { vector.y = 0f; return vector; }
    private static float HorizontalDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
