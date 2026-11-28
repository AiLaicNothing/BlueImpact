using System.Collections;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

public class FlyingGolem : Enemy
{
    private enum FlightState { Hover, Falling, Rising }

    [Header("Golem - Levitation")]
    [Min(0.1f)][SerializeField] private float hoverHeight = 2.5f;
    [Min(0f)][SerializeField] private float hoverBobAmplitude = 0.15f;
    [Min(0f)][SerializeField] private float hoverBobSpeed = 2f;
    [Tooltip("How strongly it corrects its height.")]
    [Min(0.1f)][SerializeField] private float hoverStiffness = 6f;
    [Min(0.1f)][SerializeField] private float maxVerticalSpeed = 4f;
    [Tooltip("Max distance down to look for the ground under the golem.")]
    [Min(1f)][SerializeField] private float groundProbeDistance = 10f;

    [Header("Golem - Fall / Rise")]
    [Tooltip("Time on the ground (after stagger ends) before rising.")]
    [Min(0f)][SerializeField] private float landedHoldTime = 0.3f;
    [Tooltip("Rise even if it never detects the ground after this long.")]
    [Min(0.5f)][SerializeField] private float maxFallTime = 4f;
    [Min(0.1f)][SerializeField] private float riseSpeed = 2.5f;
    [SerializeField] private string fallState = "";
    [SerializeField] private GameObject landVfx;

    [Header("Golem - Common")]
    [Min(0.5f)][SerializeField] private float attackRange = 9f;
    [Min(0f)][SerializeField] private float attackCooldown = 1.6f;
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 25f;
    [Tooltip("Chance of MagicBullets (the rest is MagicBomb).")]
    [Range(0f, 1f)][SerializeField] private float magicBulletsChance = 0.6f;
    [Min(0f)][SerializeField] private float attackRecovery = 0.3f;

    [Header("Golem - MagicBullets")]
    [SerializeField] private string magicBulletsState = "MagicBullets";
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform leftFirePoint;
    [SerializeField] private Transform rightFirePoint;
    [SerializeField] private GameObject magicCircleVfx;
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float bulletsStartTime = 0.35f;
    [Min(1)][SerializeField] private int shotsPerPoint = 3;
    [Min(0.01f)][SerializeField] private float shotInterval = 0.15f;
    [Tooltip("ON: left, right, left... OFF: both points fire together.")]
    [SerializeField] private bool alternatePoints = true;
    [Min(0f)][SerializeField] private float bulletDamageMultiplier = 0.5f;

    [Header("Golem - MagicBomb")]
    [SerializeField] private string magicBombState = "MagicBomb";
    [Tooltip("Use a prefab with E_ProjExplosive (explodes on collision, area damage).")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform bombFirePoint;
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float bombThrowTime = 0.45f;
    [Tooltip("Aim at the player's feet so it always hits the ground near them.")]
    [SerializeField] private bool aimBombAtFeet = true;
    [Min(0f)][SerializeField] private float bombDamageMultiplier = 1.5f;

    [Header("Golem - Runtime (read only)")]
    [SerializeField] private FlightState flightState = FlightState.Hover;

    private float groundHeight;
    private bool hasGroundHeight;
    private float hoverClock;
    private float fallStartTime;
    private float landedTimer;
    private float nextAttackTime;

    // ----------------------------------------------------------------
    // Setup
    // ----------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        hoverClock = Random.value * 10f;
        EnterHover();
    }

    protected override bool CanThink() => base.CanThink() && flightState == FlightState.Hover;
    protected override bool CanDriveMovement() => flightState == FlightState.Hover;

    // Path queries use the ground point below the golem.
    protected override Vector3 GetNavQueryPosition()
    {
        Vector3 p = rb != null ? rb.position : transform.position;
        return hasGroundHeight ? new Vector3(p.x, groundHeight, p.z) : p;
    }

    protected override void CheckGround()
    {
        Vector3 origin = (rb != null ? rb.position : transform.position) + Vector3.up * 0.2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundProbeDistance + 0.2f,
                whatIsGround, QueryTriggerInteraction.Ignore))
        {
            groundHeight = hit.point.y;
            hasGroundHeight = true;
            isGrounded = hit.distance <= groundCheckDistance + 0.25f;
        }
        else
        {
            isGrounded = false;
        }
    }

    // ----------------------------------------------------------------
    // Update / physics
    // ----------------------------------------------------------------

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (flightState == FlightState.Falling) CheckRecovery();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // Horizontal path following (only while hovering).
        if (isDead || rb == null) return;

        switch (flightState)
        {
            case FlightState.Hover:
                // EnemyBase only brakes pushes on the ground; a hovering golem needs it here.
                if (isInReaction && currentReaction == EnemyReactionType.Push)
                    BrakeHorizontalMovement(groundPushBraking);
                HoldHoverHeight();
                break;
            case FlightState.Rising:
                Rise();
                break;
                // Falling: gravity + hit reactions do the work.
        }
    }

    private float TargetHoverY()
    {
        float bob = Mathf.Sin(hoverClock * hoverBobSpeed) * hoverBobAmplitude;
        return (hasGroundHeight ? groundHeight : rb.position.y - hoverHeight) + hoverHeight + bob;
    }

    private void HoldHoverHeight()
    {
        hoverClock += Time.fixedDeltaTime;
        float vy = Mathf.Clamp((TargetHoverY() - rb.position.y) * hoverStiffness, -maxVerticalSpeed, maxVerticalSpeed);
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, vy, v.z);
    }

    private void Rise()
    {
        BrakeHorizontalMovement(standingBrake);
        float target = TargetHoverY();
        Vector3 v = rb.linearVelocity;

        if (rb.position.y >= target - 0.05f)
        {
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);
            EnterHover();
            return;
        }
        rb.linearVelocity = new Vector3(v.x, riseSpeed, v.z);
    }

    // ----------------------------------------------------------------
    // Flight states
    // ----------------------------------------------------------------

    private void EnterHover()
    {
        flightState = FlightState.Hover;
        isGroundEnemy = false;
        if (rb == null) return;
        rb.useGravity = false;
        InvalidatePath();
    }

    private void EnterFall()
    {
        if (flightState == FlightState.Falling) return;
        flightState = FlightState.Falling;
        isGroundEnemy = true;
        fallStartTime = Time.time;
        landedTimer = 0f;
        if (rb != null) rb.useGravity = true;
        if (!string.IsNullOrEmpty(fallState)) PlayLoop(fallState);
    }

    private void CheckRecovery()
    {
        bool landed = isGrounded;
        if (landed && landedTimer == 0f && landVfx != null)
            SpawnTimedVfx(landVfx, transform.position, Quaternion.identity, 3f);

        if (isStaggered || isStunned || isInReaction || isAirHung)
        {
            landedTimer = landed ? Mathf.Max(landedTimer, 0.0001f) : 0f;
            return;
        }

        landedTimer = landed ? landedTimer + Time.deltaTime : 0f;
        bool timedOut = Time.time - fallStartTime >= maxFallTime;
        if (landedTimer >= landedHoldTime || timedOut) StartRising();
    }

    private void StartRising()
    {
        flightState = FlightState.Rising;
        isGroundEnemy = false;
        rb.useGravity = false;
        CrossFadeTo(idleState);
    }

    // Stagger -> fall to the ground (before attack forces are applied).
    protected override void TriggerStagger()
    {
        if (isStaggered) return;
        base.TriggerStagger();
        EnterFall();
    }

    // ----------------------------------------------------------------
    // Combat
    // ----------------------------------------------------------------

    protected override void UpdateCombat()
    {
        if (FlatDistanceToPlayer() > attackRange || !CanSeePlayer())
        {
            MoveTo(player.transform.position);
            return;
        }

        StopMoving();
        FacePlayer();
        if (Time.time < nextAttackTime || !IsFacingPlayer(facingTolerance)) return;

        StartAction(Random.value < magicBulletsChance ? MagicBulletsRoutine() : MagicBombRoutine());
    }

    private IEnumerator MagicBulletsRoutine()
    {
        StopMoving();
        PlayActionAnim(magicBulletsState);

        Vector3 left = FirePoint(leftFirePoint, -1f);
        Vector3 right = FirePoint(rightFirePoint, 1f);
        SpawnTimedVfx(magicCircleVfx, left, transform.rotation, 3f);
        SpawnTimedVfx(magicCircleVfx, right, transform.rotation, 3f);

        int totalShots = alternatePoints ? shotsPerPoint * 2 : shotsPerPoint;
        float start = Time.time;
        bool started = false;
        int fired = 0;
        float nextShot = 0f;
        float hardLimit = animationSafetyTimeout + totalShots * shotInterval;

        while (Time.time - start < hardLimit)
        {
            FacePlayer();
            bool animDone = ActionAnimFinished();

            if (!started && (EventOrTime(EvtShoot, Time.time - start, bulletsStartTime) || animDone))
            {
                started = true;
                nextShot = Time.time;
            }

            if (started && fired < totalShots && Time.time >= nextShot)
            {
                if (alternatePoints)
                {
                    bool useLeft = fired % 2 == 0;
                    FireBullet(useLeft ? FirePoint(leftFirePoint, -1f) : FirePoint(rightFirePoint, 1f));
                }
                else
                {
                    FireBullet(FirePoint(leftFirePoint, -1f));
                    FireBullet(FirePoint(rightFirePoint, 1f));
                }
                fired++;
                nextShot = Time.time + shotInterval;
            }

            if (fired >= totalShots && animDone) break;
            yield return null;
        }

        yield return Recover();
    }

    private IEnumerator MagicBombRoutine()
    {
        StopMoving();
        bool thrown = false;

        yield return PlayAndWait(magicBombState, elapsed =>
        {
            if (thrown) return;
            FacePlayer();
            if (EventOrTime(EvtShoot, elapsed, bombThrowTime))
            {
                thrown = true;
                ThrowBomb();
            }
        });

        if (!thrown) ThrowBomb();
        yield return Recover();
    }

    private void FireBullet(Vector3 origin)
    {
        FireProjectile(bulletPrefab, origin, AimDirection(origin), bulletDamageMultiplier);
    }

    private void ThrowBomb()
    {
        if (!HasValidPlayer()) return;
        Vector3 origin = bombFirePoint != null ? bombFirePoint.position : transform.position + transform.forward;
        Vector3 target = aimBombAtFeet ? player.transform.position : PlayerAimPoint;
        Vector3 direction = target - origin;
        FireProjectile(bombPrefab, origin, direction, bombDamageMultiplier);
    }

    private Vector3 FirePoint(Transform point, float side)
    {
        if (point != null) return point.position;
        return transform.position + transform.right * (0.8f * side) + transform.forward * 0.5f;
    }

    private IEnumerator Recover()
    {
        CrossFadeTo(idleState);
        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);
        nextAttackTime = Time.time + attackCooldown;
    }

    protected override void OnActionCancelled()
    {
        nextAttackTime = Time.time + attackCooldown;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!debug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + Vector3.down * hoverHeight);
    }
}
