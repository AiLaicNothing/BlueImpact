using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FlyingGolem : EnemyBase
{
    private enum AIState { Idle, Patrol, Chase, Attack, ReturnHome }
    private enum FlightState { Hover, Falling, Recovering }
    private enum AttackType { None, MagicBullet, Bomb }

    [Header("Detection / Combat")]
    [SerializeField] private float detectionRange = 18f;
    [SerializeField] private float detectionDelay = 0.2f;
    [SerializeField] private float maxChaseDistance = 28f;
    [SerializeField] private bool instantDetection;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float facingAngle = 15f;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Navigation / Turning")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float stoppingDistance = 0.75f;
    [SerializeField] private float turnSpeed = 15f; // Slerp responsiveness, NOT degrees per second.
    [SerializeField] private float destinationRefresh = 0.2f;
    [SerializeField] private float patrolStopDistance = 1.5f;

    [Header("Hover / Colliders")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float hoverHeight = 4f;
    [SerializeField] private float hoverBobAmplitude = 0.2f;
    [SerializeField] private float hoverBobSpeed = 2f;
    [SerializeField] private float hoverSmooth = 8f;
    [Tooltip("Only root-mounted colliders are offset. Child colliders follow their Transform.")]
    [SerializeField] private CapsuleCollider bodyCapsule;
    [SerializeField] private BoxCollider bodyBox;

    [Header("Attack Selection / Timing")]
    [Range(0f, 1f)][SerializeField] private float magicVolleyChance = 0.6f;
    [SerializeField] private string magicVolleyState = "Bomb";  // Original first attack clip.
    [SerializeField] private string bombState = "Attack 0";    // Original second attack clip.
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRecovery = 0.5f;
    [SerializeField] private float attackExitBlend = 0.08f;
    [SerializeField] private float attackSafetyTimeout = 4f;

    [Header("Attack 1 - Magic Volley")]
    [SerializeField] private GameObject magicCircleVfx;
    [SerializeField] private Vector3 circleOffset = new Vector3(1f, 0f, 0.5f);
    [SerializeField] private GameObject magicBulletPrefab;
    [Min(1)][SerializeField] private int bulletAmount = 3;
    [Min(0f)][SerializeField] private float timeBetweenShots = 0.2f;

    [Header("Attack 2 - Bomb")]
    [SerializeField] private GameObject chargeVfx;
    [SerializeField] private Vector3 bombPositionOffset;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float bombSpeed = 12f;

    [Header("Fall / Hover Recovery")]
    [SerializeField] private float fallForce = 8f;
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

    private Vector3 originalVisualPosition;
    private Vector3 originalCapsuleCenter;
    private Vector3 originalBoxCenter;
    private Vector3 desiredFacing;
    private Vector3 homePosition;
    private Vector3 leftCirclePosition;
    private Vector3 rightCirclePosition;
    private float hoverClock;
    private float detectionTimer;
    private float nextDestinationTime;
    private float nextAttackTime;
    private float fallStartTime;
    private float groundedTime;
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool hasTarget;
    private bool circleSpawned;
    private bool bombThrown;
    private bool chargeVfxSpawned;
    private bool volleyStarted;
    private bool endAttackRequested;

    protected override void Awake()
    {
        // This is a flying-looking enemy with a ground-level NavMesh root.
        // The dynamic Rigidbody remains in control of its movement at all times.
        isGroundEnemy = false;
        rotateToMovement = false; // This subclass handles facing itself.
        base.Awake();

        homePosition = transform.position;
        desiredFacing = transform.forward;
        if (visualRoot != null) originalVisualPosition = visualRoot.localPosition;
        if (bodyCapsule == null) bodyCapsule = GetComponent<CapsuleCollider>();
        if (bodyBox == null) bodyBox = GetComponent<BoxCollider>();
        if (bodyCapsule != null) originalCapsuleCenter = bodyCapsule.center;
        if (bodyBox != null) originalBoxCenter = bodyBox.center;

        if (agent != null)
        {
            agent.updatePosition = false; // Never compete with the dynamic Rigidbody.
            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.acceleration = acceleration;
            agent.stoppingDistance = stoppingDistance;
        }

        EnterHoverPhysics();
    }

    protected override void Start()
    {
        base.Start();
        if (rb != null && NavMesh.SamplePosition(rb.position, out NavMeshHit hit, 8f,
                agent != null ? agent.areaMask : NavMesh.AllAreas))
        {
            // The root follows the ground NavMesh. Only the mesh/collider floats.
            rb.position = hit.position;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(hit.position);
        }
        if (visualRoot == null)
            Debug.LogWarning($"[FlyingGolem] {name} needs a Visual Root for hovering.", this);
        else
        {
            visualRoot.localPosition = originalVisualPosition + Vector3.up * hoverHeight;
            UpdateHoverVisual();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        UpdateHoverVisual();
        if (isStaggered && !immuneToMovement && flightState == FlightState.Hover)
            EnterFallMode();

        if (flightState == FlightState.Falling)
        {
            CheckFallRecovery();
            return;
        }
        if (flightState == FlightState.Recovering) return;

        if (isStunned || isInReaction)
        {
            if (aiState == AIState.Attack) CancelAttack();
            return;
        }
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        DetectPlayer();
        if (aiState == AIState.Attack) return;

        if (hasTarget) UpdateCombat();
        else UpdatePatrolOrReturn();

        UpdateLocomotionAnimation();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (isDead || rb == null) return;
        // Do not counteract player launch/knockdown forces during an active reaction.
        if (flightState == FlightState.Falling && !isInReaction && fallForce > 0f)
            rb.AddForce(Vector3.down * fallForce, ForceMode.Acceleration);
        if (flightState != FlightState.Hover || isStunned || isInReaction || isStaggered) return;

        Vector3 horizontal = Flat(desiredFacing);
        if (horizontal.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(horizontal.normalized);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target,
            Mathf.Clamp01(turnSpeed * Time.fixedDeltaTime)));
    }

    protected override void TriggerStagger()
    {
        if (isStaggered) return;
        CancelAttack();
        base.TriggerStagger();
        if (!immuneToMovement) EnterFallMode();
    }

    protected override void EnableAgentIfNeeded()
    {
        // EnemyBase should reconnect only after the visual/body have risen to hover height.
        if (flightState != FlightState.Hover) return;
        base.EnableAgentIfNeeded();
    }

    // --------------------------------------------------------
    // Detection, pursuit, patrol
    // --------------------------------------------------------
    private void DetectPlayer()
    {
        if (!HasValidPlayer())
        {
            hasTarget = false;
            detectionTimer = 0f;
            return;
        }

        Vector3 home = HomePoint != null ? HomePoint.position : homePosition;
        if (HorizontalDistance(transform.position, home) > maxChaseDistance)
        {
            hasTarget = false;
            detectionTimer = 0f;
            aiState = AIState.ReturnHome;
            return;
        }

        if (hasTarget) return;
        bool visible = HorizontalDistance(transform.position, player.transform.position)
            <= detectionRange && ClearShot();
        detectionTimer = visible ? detectionTimer + Time.deltaTime : 0f;
        if (instantDetection || detectionTimer >= detectionDelay)
        {
            hasTarget = true;
            aiState = AIState.Chase;
        }
    }

    private void UpdateCombat()
    {
        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        if (toPlayer.sqrMagnitude > 0.01f) desiredFacing = toPlayer.normalized;

        if (toPlayer.sqrMagnitude > attackRange * attackRange || !ClearShot())
        {
            ResumeNavigation(player.transform.position);
            if (agent.desiredVelocity.sqrMagnitude > 0.01f)
                desiredFacing = Flat(agent.desiredVelocity).normalized;
            aiState = AIState.Chase;
            return;
        }

        StopNavigation();
        aiState = AIState.Chase;
        if (Time.time >= nextAttackTime &&
            Vector3.Angle(Flat(transform.forward), toPlayer) <= facingAngle)
            BeginAttack();
    }

    private void UpdatePatrolOrReturn()
    {
        Vector3 home = HomePoint != null ? HomePoint.position : homePosition;
        if (aiState == AIState.ReturnHome)
        {
            ResumeNavigation(home);
            if (HorizontalDistance(transform.position, home) <= patrolStopDistance)
                aiState = AIState.Idle;
        }
        else if (hasPatrol && patrolZones != null && patrolZones.Length > 0)
        {
            aiState = AIState.Patrol;
            Transform point = patrolZones[patrolIndex];
            if (point == null) { patrolIndex = 0; StopNavigation(); return; }
            ResumeNavigation(point.position);
            if (HorizontalDistance(transform.position, point.position) <= patrolStopDistance
                && patrolZones.Length > 1)
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

        if (agent != null && !agent.isStopped && agent.desiredVelocity.sqrMagnitude > 0.01f)
            desiredFacing = Flat(agent.desiredVelocity).normalized;
    }

    private void ResumeNavigation(Vector3 destination)
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

    private bool ClearShot()
    {
        if (!HasValidPlayer()) return false;
        Vector3 start = (visualRoot != null ? visualRoot.position : transform.position)
            + Vector3.up * targetRayHeight;
        Vector3 end = player.transform.position + Vector3.up * targetRayHeight;
        // obstacleLayer should contain environment only, not enemy/player layers.
        return !Physics.Linecast(start, end, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private void UpdateLocomotionAnimation()
    {
        if (anim == null) return;
        bool moving = agent != null && !agent.isStopped && rb != null &&
            Flat(rb.linearVelocity).sqrMagnitude > 0.09f;
        string clip = moving ? "Patrol" : "Idle";
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(clip) ||
            (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(clip))) return;
        anim.CrossFadeInFixedTime(clip, 0.12f, 0, 0f);
    }

    // --------------------------------------------------------
    // Attack: only the animation events create projectiles.
    // --------------------------------------------------------
    private void BeginAttack()
    {
        if (anim == null || stats == null) return;
        aiState = AIState.Attack;
        StopNavigation();
        currentAttack = Random.value < magicVolleyChance
            ? AttackType.MagicBullet : AttackType.Bomb;
        circleSpawned = bombThrown = volleyStarted = endAttackRequested = chargeVfxSpawned = false;
        attackRoutine = StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        string stateName = currentAttack == AttackType.MagicBullet
            ? magicVolleyState : bombState;
        int hash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, hash))
        {
            Debug.LogError($"[FlyingGolem] Animator state '{stateName}' not found on layer 0.", this);
            attackRoutine = null;
            FinishAttack();
            yield break;
        }
        PlayAnim(stateName);
        anim.Play(hash, 0, 0f); // Force restart, including consecutive attacks of same kind.
        yield return null;

        bool entered = false;
        float elapsed = 0f;
        while (aiState == AIState.Attack && !endAttackRequested &&
            !isStunned && !isStaggered && !isDead)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == hash)
            {
                entered = true;
                float remaining = Mathf.Max(0f, 1f - info.normalizedTime) *
                    info.length / Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                if (remaining <= attackExitBlend) break;
            }
            else if (entered) break;

            elapsed += Time.deltaTime;
            if (elapsed >= attackSafetyTimeout) break;
            yield return null;
        }

        if (aiState != AIState.Attack || isDead || isStunned || isStaggered) yield break;
        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == hash)
            anim.CrossFadeInFixedTime("Idle", attackExitBlend, 0, 0f);

        // Allow the volley started by its animation event to finish its bursts.
        float extraWait = 0f;
        while (volleyRoutine != null && extraWait < 2f)
        {
            extraWait += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(Mathf.Max(attackRecovery, attackExitBlend));
        if (aiState != AIState.Attack || isDead || isStunned || isStaggered) yield break;

        attackRoutine = null;
        FinishAttack();
    }

    private void FinishAttack()
    {
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
        nextAttackTime = Time.time + attackCooldown;
        currentAttack = AttackType.None;
        aiState = AIState.Chase;
        if (anim != null && !isDead)
            anim.CrossFadeInFixedTime("Idle", 0.12f, 0, 0f);
    }

    public void SpawnMagicCircles()
    {
        if (!EventAllowed(AttackType.MagicBullet) || circleSpawned) return;
        circleSpawned = true;
        Vector3 origin = (visualRoot != null ? visualRoot.position : transform.position) +
            transform.forward * circleOffset.z + Vector3.up * circleOffset.y;
        Vector3 right = transform.right * Mathf.Abs(circleOffset.x);
        leftCirclePosition = origin - right;
        rightCirclePosition = origin + right;
        SpawnVfx(magicCircleVfx, leftCirclePosition);
        SpawnVfx(magicCircleVfx, rightCirclePosition);
    }

    public void FireMagicBullet()
    {
        if (!EventAllowed(AttackType.MagicBullet) || volleyStarted) return;
        volleyStarted = true;
        if (!circleSpawned) SpawnMagicCircles();
        volleyRoutine = StartCoroutine(MagicBulletRoutine());
    }

    private IEnumerator MagicBulletRoutine()
    {
        // Yield once so the caller can store the Coroutine handle before completion.
        yield return null;
        for (int i = 0; i < bulletAmount; i++)
        {   
            if (!EventAllowed(AttackType.MagicBullet)) break;
            SpawnBullet(leftCirclePosition);
            SpawnBullet(rightCirclePosition);
            if (i + 1 < bulletAmount)
                yield return new WaitForSeconds(timeBetweenShots);
        }
        volleyRoutine = null;
    }

    public void SpawnChargeVfx()
    {
        if (!EventAllowed(AttackType.Bomb) || chargeVfxSpawned) return;
        chargeVfxSpawned = true;
        SpawnVfx(chargeVfx, transform.TransformPoint(bombPositionOffset));
    }

    public void ThrowBomb()
    {
        if (!EventAllowed(AttackType.Bomb) || bombThrown || bombPrefab == null) return;
        bombThrown = true;
        Vector3 origin = transform.TransformPoint(bombPositionOffset);
        Vector3 direction = GetAimDirection(origin);
        GameObject bomb = Instantiate(bombPrefab, origin, Quaternion.LookRotation(direction));

        // E_Projectile controls its own projectile motion; other bombs use a Rigidbody.
        if (bomb.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(stats.damage, direction);
        else if (bomb.TryGetComponent(out Rigidbody bombBody))
            bombBody.linearVelocity = direction * bombSpeed;
    }

    /// <summary>Optional Animation Event near the end of either attack.</summary>
    public void EndAttack()
    {
        if (aiState == AIState.Attack) endAttackRequested = true;
    }

    private bool EventAllowed(AttackType type) =>
        aiState == AIState.Attack && currentAttack == type &&
        !isDead && !isStunned && !isStaggered && HasValidPlayer();

    private void SpawnBullet(Vector3 origin)
    {
        if (magicBulletPrefab == null) return;
        Vector3 direction = GetAimDirection(origin);
        GameObject bullet = Instantiate(magicBulletPrefab, origin, Quaternion.LookRotation(direction));
        if (bullet.TryGetComponent(out E_Projectile projectile))
            projectile.InitProj(stats.damage, direction);
    }

    private Vector3 GetAimDirection(Vector3 origin)
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

    // --------------------------------------------------------
    // Hover and fall: dynamic body throughout; no agent-driven Transform.
    // --------------------------------------------------------
    private void EnterHoverPhysics()
    {
        flightState = FlightState.Hover;
        if (rb == null) return;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = (rb.constraints |
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ) &
            ~RigidbodyConstraints.FreezeRotationY;
    }

    private void UpdateHoverVisual()
    {
        if (visualRoot == null) return;
        if (flightState == FlightState.Hover) hoverClock += Time.deltaTime;
        float bob = flightState == FlightState.Hover
            ? Mathf.Sin(hoverClock * hoverBobSpeed) * hoverBobAmplitude : 0f;
        float offsetY = hoverHeight + bob;
        Vector3 target = originalVisualPosition + Vector3.up * offsetY;
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, target,
            1f - Mathf.Exp(-hoverSmooth * Time.deltaTime));

        // If a collider is mounted on the root, move its center with the model.
        // Child colliders should NOT be offset again.
        if (bodyCapsule != null && bodyCapsule.transform == transform)
            bodyCapsule.center = originalCapsuleCenter + Vector3.up *
                (visualRoot.localPosition.y - originalVisualPosition.y);
        if (bodyBox != null && bodyBox.transform == transform)
            bodyBox.center = originalBoxCenter + Vector3.up *
                (visualRoot.localPosition.y - originalVisualPosition.y);
    }

    private void EnterFallMode()
    {
        if (flightState != FlightState.Hover) return;
        flightState = FlightState.Falling;
        CancelAttack();
        fallStartTime = Time.time;
        groundedTime = 0f;
        if (rb != null)
        {
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            rb.useGravity = true;
            // Gravity starts the fall; do not cancel a possible launcher hit.
        }
    }

    private void CheckFallRecovery()
    {
        if (isStaggered || isStunned || isInReaction || isAirHung) return;
        if (Time.time - fallStartTime < minimumFallTime) return;
        groundedTime = isGrounded ? groundedTime + Time.deltaTime : 0f;
        bool landed = groundedTime >= landedHoldTime;
        bool timedOut = Time.time - fallStartTime >= maxFallTime;
        if (!landed && !timedOut) return;
        // If we cannot find a NavMesh point, remain fallen and retry later.
        if (!NavMesh.SamplePosition(rb.position, out NavMeshHit navHit,
            recoveryNavMeshRadius, agent != null ? agent.areaMask : NavMesh.AllAreas))
            return;
        if (riseRoutine == null) riseRoutine = StartCoroutine(RiseToHover(navHit.position));
    }

    private IEnumerator RiseToHover(Vector3 to)
    {
        flightState = FlightState.Recovering;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        Vector3 from = rb.position;
        float elapsed = 0f;
        while (elapsed < riseDuration && !isDead)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(Vector3.Lerp(from, to,
                Mathf.Clamp01(elapsed / Mathf.Max(0.01f, riseDuration))));
            yield return new WaitForFixedUpdate();
        }
        if (isDead) yield break;
        rb.MovePosition(to);
        rb.linearVelocity = Vector3.zero;
        EnterHoverPhysics();
        riseRoutine = null;
        EnableAgentIfNeeded();
        aiState = hasTarget ? AIState.Chase : AIState.ReturnHome;
    }

    private static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }
    private static float HorizontalDistance(Vector3 a, Vector3 b) =>
        Flat(a - b).magnitude;

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
