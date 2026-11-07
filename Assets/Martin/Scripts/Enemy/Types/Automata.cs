using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Automata : EnemyBase
{
    private enum Attack { None, Burst, Dash, Spread }
    private enum AttackTestMode { Random, BurstOnly, DashOnly }

    [Header("Detection")]
    [SerializeField, Min(0.5f)] private float detectionRange = 11f;
    [SerializeField, Min(0f)] private float detectionDelay = 0.4f;
    [SerializeField] private bool instantDetection = false;
    [SerializeField, Min(1f)] private float maxChaseDistance = 25f;
    [Tooltip("Environment only. Leave empty to test detection without line-of-sight blockers.")]
    [SerializeField] private LayerMask sightBlockingLayers;

    [Header("Chase")]
    [SerializeField, Min(0.5f)] private float attackRange = 8f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 15f;
    [SerializeField, Range(1f, 180f)] private float facingTolerance = 20f;

    [Header("Attack Selection")]
    [SerializeField, Range(0f, 1f)] private float burstChance = 0.6f;
    [SerializeField] private AttackTestMode attackTestMode = AttackTestMode.Random;
    [SerializeField, Min(0f)] private float attackCooldown = 0.9f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.25f;
    [SerializeField, Min(0.5f)] private float animationTimeout = 5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float aimHeight = 1.2f;

    [Header("Attack 1")]
    [SerializeField, Min(1)] private int burstCount = 3;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.14f;

    [Header("Attack 2")]
    [Tooltip("Maximum dash speed. Actual movement follows the Dash animation timeline.")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 10f;
    [Tooltip("Normalized Dash animation time at which travel should finish. Match the Dash -> Disparo_3 transition Exit Time.")]
    [SerializeField, Range(0.5f, 1f)] private float dashMotionEndNormalized = 0.95f;
    [SerializeField, Min(0.1f)] private float dashMaxDistance = 4f;
    [SerializeField, Min(0.1f)] private float dashStopDistance = 2f;
    [SerializeField, Min(1)] private int spreadCount = 5;
    [SerializeField, Range(0f, 180f)] private float spreadAngle = 40f;

    [Header("Dash Safety")]
    [SerializeField, Min(0.05f)] private float edgeMargin = 0.3f;
    [Tooltip("Optional solid walls not baked into NavMesh. Exclude player, floor and Automata.")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField, Min(0.05f)] private float wallRadius = 0.35f;
    [SerializeField, Min(0.1f)] private float wallHeight = 1f;
    [SerializeField] private bool logDash;
    [SerializeField] private float lastSafeDash;
    [SerializeField] private string lastDashStatus;
    [Header("Runtime Detection")]
    [SerializeField] private bool detectedPlayer;
    [SerializeField] private float detectionTimer;

    private Vector3 spawnPosition;
    private Attack attack;
    private Vector3 facing;
    private Vector3 dashDirection;
    private Vector3 dashOrigin;
    private float safeDashLength;
    private float nextPathTime;
    private float nextAttackTime;
    private bool dashActive;
    private bool usedBurstEvent;
    private bool usedSpreadEvent;
    private Coroutine actionRoutine;
    private Coroutine burstRoutine;

    private bool Busy => attack != Attack.None;
    private bool CanFight => !isDead && !isStunned && !isStaggered &&
                             !isInReaction && HasValidPlayer();

    protected override void Awake()
    {
        rotateToMovement = false;
        base.Awake(); // This script turns even when agent is stopped.
        spawnPosition = transform.position;
        facing = Flat(transform.forward).normalized;
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (!HasValidPlayer())
        {
            detectedPlayer = false;
            detectionTimer = 0f;
            CancelAttack();
            StopNavigation();
            return;
        }

        if (isStunned || isStaggered || isInReaction)
        {
            CancelAttack();
            return;
        }

        UpdateDetection();

        if (!detectedPlayer)
        {
            CancelAttack();
            StopNavigation();
            PlayLoop("Idle");
            return;
        }

        if (Busy)
        {
            if (attack == Attack.Burst || attack == Attack.Spread)
                Face(player.transform.position - transform.position);
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 toPlayer = Flat(player.transform.position - transform.position);
        float distance = toPlayer.magnitude;

        if (distance > attackRange)
        {
            agent.isStopped = false;

            if (!agent.hasPath || Time.time >= nextPathTime)
            {
                agent.SetDestination(player.transform.position);
                nextPathTime = Time.time + repathInterval;
            }
            Face(agent.desiredVelocity.sqrMagnitude > 0.01f? agent.desiredVelocity : toPlayer);
            PlayLoop("Walk");
            return;
        }

        StopNavigation();
        Face(toPlayer);
        PlayLoop("Idle");

        if (Time.time >= nextAttackTime &&
            Vector3.Angle(Flat(transform.forward), toPlayer) <= facingTolerance)
            BeginAttack();
    }

    private void UpdateDetection()
    {
        if (detectedPlayer)
        {
            if (Flat(player.transform.position - spawnPosition).sqrMagnitude > maxChaseDistance * maxChaseDistance)
            {
                detectedPlayer = false;
                detectionTimer = 0f;
            }
            return;
        }

        Vector3 toPlayer = Flat(player.transform.position - transform.position);

        if (toPlayer.sqrMagnitude > detectionRange * detectionRange)
        {
            detectionTimer = 0f;
            return;
        }

        Vector3 eyes = transform.position + Vector3.up * aimHeight;

        Vector3 target = player.transform.position + Vector3.up * aimHeight;

        if (sightBlockingLayers.value != 0 &&
            Physics.Linecast(eyes, target, sightBlockingLayers, QueryTriggerInteraction.Ignore))
        {
            detectionTimer = 0f;
            return;
        }

        detectionTimer += Time.deltaTime;

        if (instantDetection || detectionTimer >= detectionDelay)
        {
            detectedPlayer = true;
            detectionTimer = 0f;
        }
    }

    protected override void FixedUpdate()
    {
        if (dashActive && CanFight)
        {
            DashStep();
            Turn(dashDirection);
            return;
        }
        if (dashActive) StopDash();

        base.FixedUpdate();

        if (!isDead && !isStunned && !isStaggered && !isInReaction)
            Turn(facing);
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

    private void StopNavigation()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
    }

    private void BeginAttack()
    {
        if (anim == null || rb == null || stats == null || projectilePrefab == null) return;

        bool useDash = attackTestMode == AttackTestMode.DashOnly ||
            (attackTestMode == AttackTestMode.Random && Random.value >= burstChance);

        lastSafeDash = 0f;
        if (useDash)
        {
            dashDirection = Flat(player.transform.position - rb.position).normalized;
            float room = Flat(player.transform.position - rb.position).magnitude - dashStopDistance;
            safeDashLength = FindSafeDashDistance(dashDirection, Mathf.Min(dashMaxDistance, room));
            lastSafeDash = safeDashLength;

            // Random mode can switch to Burst. Debug mode NEVER silently changes the attack.
            if (safeDashLength < 0.2f && attackTestMode != AttackTestMode.DashOnly)
            {
                SetDashStatus($"Dash unavailable ({safeDashLength:F2} m); using Burst");
                useDash = false;
            }
            else if (safeDashLength < 0.2f) SetDashStatus($"Dash Only: animation will play, but travel is blocked ({safeDashLength:F2} m)");
            else SetDashStatus($"Dash selected, safe travel = {safeDashLength:F2} m");
        }
        else if (logDash) Debug.Log($"[Automata] {name}: Burst selected (mode {attackTestMode}, chance {burstChance:F2})", this);

        StopNavigation();
        usedBurstEvent = usedSpreadEvent = false;
        attack = useDash ? Attack.Dash : Attack.Burst;
        actionRoutine = StartCoroutine(AttackSequence(useDash));
    }

    private void SetDashStatus(string status)
    {
        lastDashStatus = status;
        if (logDash) Debug.Log($"[Automata] {name}: {status}", this);
    }

    private IEnumerator AttackSequence(bool useDash)
    {
        if (!useDash)
        {
            PlayAnim("Disparo_1");
            yield return WaitForClip("Disparo_1");
            while (burstRoutine != null && CanFight) yield return null;
        }
        else
        {
            dashOrigin = rb.position;
            PlayAnim("Dash");
            dashActive = true;

            yield return WaitForDashToSpread();
            StopDash();
            if (!CanFight || attack != Attack.Dash) yield break;

            attack = Attack.Spread;
            Face(player.transform.position - transform.position);

            bool alreadyEnteringSpread =
                anim.GetCurrentAnimatorStateInfo(0).IsName("Disparo_3") ||
                (anim.IsInTransition(0) &&
                 anim.GetNextAnimatorStateInfo(0).IsName("Disparo_3"));

            if (!alreadyEnteringSpread)
                anim.CrossFadeInFixedTime("Disparo_3", 0.12f, 0, 0f);

            yield return WaitForClip("Disparo_3");
        }

        if (!CanFight || !Busy) yield break;

        PlayAnim("Idle");

        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);

        FinishAttack();
    }
    private IEnumerator WaitForDashToSpread()
    {
        yield return null;
        float elapsed = 0f;
        bool enteredDash = false;

        while (CanFight && attack == Attack.Dash && elapsed < animationTimeout)
        {
            AnimatorStateInfo current = anim.GetCurrentAnimatorStateInfo(0);
            bool enteringSpread = anim.IsInTransition(0) &&
                anim.GetNextAnimatorStateInfo(0).IsName("Disparo_3");

            if (enteringSpread || current.IsName("Disparo_3"))
                yield break;

            if (current.IsName("Dash"))
            {
                enteredDash = true;
                if (current.normalizedTime >= 1f)
                    yield break; // Fallback if no transition is configured.
            }
            else if (enteredDash)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForClip(string state)
    {
        yield return null;
        float timer = 0f;
        bool entered = false;
        while (CanFight && Busy && timer < animationTimeout)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(state))
            {
                entered = true;
                if (info.normalizedTime >= 1f) yield break;
            }
            else if (entered) yield break;
            timer += Time.deltaTime;
            yield return null;
        }
        if (!entered && !isDead)
            Debug.LogWarning($"[Automata] Animator state '{state}' did not start.", this);
    }

    private void FinishAttack()
    {
        StopDash();
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = actionRoutine = null;
        attack = Attack.None;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void CancelAttack()
    {
        if (!Busy) return;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        FinishAttack();
    }

    public void ShootBurst()
    {
        if (attack != Attack.Burst || usedBurstEvent || !CanFight) return;
        usedBurstEvent = true;
        burstRoutine = StartCoroutine(BurstShots());
    }

    private IEnumerator BurstShots()
    {
        yield return null; 
        for (int i = 0; i < burstCount && attack == Attack.Burst && CanFight; i++)
        {
            Fire(Aim());
            if (i + 1 < burstCount) yield return new WaitForSeconds(burstInterval);
        }
        burstRoutine = null;
    }

    public void ShootSpread()
    {
        if (attack != Attack.Spread || usedSpreadEvent || !CanFight) return;
        usedSpreadEvent = true;
        Vector3 forward = Aim();
        for (int i = 0; i < spreadCount; i++)
        {
            float t = spreadCount == 1 ? 0f : (float)i / (spreadCount - 1) - 0.5f;
            Fire(Quaternion.AngleAxis(t * spreadAngle, Vector3.up) * forward);
        }
    }

    private Vector3 Aim()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 to = player.transform.position + Vector3.up * aimHeight - origin;
        return to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;
    }

    private void Fire(Vector3 direction)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        direction.Normalize();
        GameObject shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        if (shot.TryGetComponent(out E_Projectile proj)) proj.InitProj(stats.damage, direction);
    }

    // NavMesh is the authoritative ledge/floor check. No whatIsGround dependency.
    private float FindSafeDashDistance(Vector3 direction, float request)
    {
        if (request <= 0f || direction.sqrMagnitude < 0.01f ||
            agent == null || !agent.enabled || !agent.isOnNavMesh) return 0f;


        Vector3 start = agent.nextPosition;
        float safe = request;

        if (NavMesh.Raycast(start, start + direction * (request + edgeMargin), out NavMeshHit edge, agent.areaMask))
        {
            float edgeDistance = Mathf.Max(0f, Vector3.Dot(Flat(edge.position - start), direction));
            safe = Mathf.Min(safe, edgeDistance - edgeMargin);
        }

        if (wallLayers.value != 0 && safe > 0f &&
            Physics.SphereCast(rb.position + Vector3.up * wallHeight,wallRadius, direction, out RaycastHit wall,
                safe + edgeMargin, wallLayers, QueryTriggerInteraction.Ignore))
        {
            safe = Mathf.Min(safe, wall.distance - edgeMargin);
        }
        return Mathf.Max(0f, safe);
    }

    private void DashStep()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || anim == null)
        {
            EndDashEarly("Dash stopped: navigation/Animator unavailable");
            return;
        }

        if (anim.IsInTransition(0) &&
            anim.GetNextAnimatorStateInfo(0).IsName("Disparo_3"))
        {
            StopDash();
            return;
        }

        AnimatorStateInfo dashState = anim.GetCurrentAnimatorStateInfo(0);
        if (!dashState.IsName("Dash"))
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }

        float traveled = Mathf.Max(0f,
            Vector3.Dot(Flat(rb.position - dashOrigin), dashDirection));
        float remaining = safeDashLength - traveled;
        float space = Flat(player.transform.position - rb.position).magnitude -
                      dashStopDistance;

        if (space <= 0.01f)
        {
            EndDashEarly("Dash ended: reached player stop distance");
            return;
        }
        if (remaining <= 0.01f)
        {
            StopDash(); 
            return;
        }

        float progress = Mathf.Clamp01(
            dashState.normalizedTime / Mathf.Max(0.01f, dashMotionEndNormalized));
        float desiredDistance = safeDashLength * progress;
        float catchUp = Mathf.Max(0f, desiredDistance - traveled);
        float step = Mathf.Min(catchUp, dashSpeed * Time.fixedDeltaTime,
                               remaining, space);

        if (step <= 0.0001f)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }

        if (NavMesh.Raycast(agent.nextPosition, agent.nextPosition + dashDirection * (step + edgeMargin),
                out _, agent.areaMask) ||
            (wallLayers.value != 0 && Physics.SphereCast(
                rb.position + Vector3.up * wallHeight, wallRadius,
                dashDirection, out _, step + 0.02f,
                wallLayers, QueryTriggerInteraction.Ignore)))
        {
            EndDashEarly("Dash ended: NavMesh boundary or wall");
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        float speed = step / Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(
            dashDirection.x * speed, velocity.y, dashDirection.z * speed);
    }

    private void EndDashEarly(string reason)
    {
        SetDashStatus(reason);
        StopDash();
        if (CanFight && attack == Attack.Dash && anim != null && !anim.IsInTransition(0))
            anim.CrossFadeInFixedTime("Disparo_3", 0.12f, 0, 0f);
    }

    private void StopDash()
    {
        if (!dashActive) return;

        dashActive = false;
        if (rb == null || isInReaction || isStunned || isStaggered) return;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);
    }

    private void Face(Vector3 direction)
    {
        direction = Flat(direction);

        if (direction.sqrMagnitude > 0.001f) facing = direction.normalized;
    }

    private void Turn(Vector3 direction)
    {
        if (rb == null || direction.sqrMagnitude < 0.001f) return;

        float t = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(direction), t));
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}
