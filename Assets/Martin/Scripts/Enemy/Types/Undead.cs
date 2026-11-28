using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Undead : Enemy
{
    private enum HitMode { Timer, AnimationEvents }

    [Header("Undead - Common")]
    [Min(0.1f)][SerializeField] private float attackRange = 2.2f;
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 15f;

    [Header("Undead - Attack Slash")]
    [SerializeField] private string slashState = "Attack";
    [Min(0f)][SerializeField] private float slashCooldown = 0.8f;
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float slashHitTime = 0.4f;
    [Min(0f)][SerializeField] private float slashDamageMultiplier = 1f;
    [Min(0f)][SerializeField] private float slashRecovery = 0.2f;
    [SerializeField] private Vector3 slashHitBoxSize = new Vector3(1.4f, 1f, 1.4f);
    [SerializeField] private Vector3 slashHitBoxOffset = new Vector3(0f, 1f, 1f);

    [Header("Undead - Attack Consecutive Slash")]
    [Tooltip("Available only when HP% is LOWER than this (0.75 = 75%).")]
    [Range(0f, 1f)][SerializeField] private float consecutiveHpThreshold = 0.75f;
    [Tooltip("Chance checked once per second while available and in range.")]
    [Range(0f, 1f)][SerializeField] private float consecutiveChance = 0.4f;
    [Min(0f)][SerializeField] private float consecutiveCooldown = 7f;
    [Tooltip("Can start from this distance (it lunges forward, so it can be > attackRange).")]
    [Min(0.1f)][SerializeField] private float consecutiveStartRange = 4f;
    [Tooltip("Time spent turning toward the player before moving.")]
    [Min(0f)][SerializeField] private float consecutiveAimTime = 0.35f;
    [Tooltip("Optional state played while aiming (empty = keep Idle).")]
    [SerializeField] private string consecutiveAimState = "";
    [Tooltip("Looping state played while moving and slashing.")]
    [SerializeField] private string consecutiveState = "ConsecutiveSlash";
    [Tooltip("X seconds: how long it moves forward slashing.")]
    [Min(0.1f)][SerializeField] private float consecutiveDuration = 2f;
    [Min(0f)][SerializeField] private float consecutiveMoveSpeed = 5f;
    [Tooltip("Timer: a hit every 'Hit Interval'. AnimationEvents: a hit per 'Hit' event.")]
    [SerializeField] private HitMode consecutiveHitMode = HitMode.Timer;
    [Min(0.05f)][SerializeField] private float consecutiveHitInterval = 0.3f;
    [Min(0f)][SerializeField] private float consecutiveFirstHitDelay = 0.1f;
    [Min(0f)][SerializeField] private float consecutiveDamageMultiplier = 0.6f;
    [Min(0f)][SerializeField] private float consecutiveRecovery = 0.6f;
    [SerializeField] private Vector3 consecutiveHitBoxSize = new Vector3(1.8f, 1.2f, 1.6f);
    [SerializeField] private Vector3 consecutiveHitBoxOffset = new Vector3(0f, 1f, 1f);
    [SerializeField] private GameObject consecutiveStartVfx;

    private float nextSlashTime;
    private bool inAttackRange;
    private float nextConsecutiveTime;
    private float nextConsecutiveRoll;

    private bool ConsecutiveAvailable =>
        HpPercent < consecutiveHpThreshold && Time.time >= nextConsecutiveTime;

    protected override void UpdateCombat()
    {
        float distance = FlatDistanceToPlayer();
        bool canSee = CanSeePlayer();

        // Consecutive Slash: rolled once per second while available.
        if (ConsecutiveAvailable && canSee && distance <= consecutiveStartRange && Time.time >= nextConsecutiveRoll)
        {
            nextConsecutiveRoll = Time.time + 1f;
            if (Random.value < consecutiveChance)
            {
                StartAction(ConsecutiveSlashRoutine());
                return;
            }
        }

        if (!InRange(distance, attackRange, ref inAttackRange) || !canSee)
        {
            MoveTo(player.transform.position);
            return;
        }

        StopMoving();
        FacePlayer();
        if (Time.time >= nextSlashTime && IsFacingPlayer(facingTolerance))
            StartAction(SlashRoutine());
    }

    // ----------------------------------------------------------------
    // Attack Slash
    // ----------------------------------------------------------------

    private IEnumerator SlashRoutine()
    {
        Vector3 direction = DirectionToPlayer();
        LockFacing(direction);
        StopMoving();

        bool hitDone = false;
        yield return PlayAndWait(slashState, elapsed =>
        {
            if (!hitDone && EventOrTime(EvtHit, elapsed, slashHitTime))
            {
                hitDone = true;
                MeleeHit(slashHitBoxOffset, slashHitBoxSize, direction, slashDamageMultiplier);
            }
        });

        CrossFadeTo(idleState);
        if (slashRecovery > 0f) yield return new WaitForSeconds(slashRecovery);
        nextSlashTime = Time.time + slashCooldown;
    }

    // ----------------------------------------------------------------
    // Attack Consecutive Slash
    // ----------------------------------------------------------------

    private IEnumerator ConsecutiveSlashRoutine()
    {
        nextConsecutiveTime = Time.time + consecutiveCooldown;
        StopMoving();

        if (consecutiveStartVfx != null)
            SpawnTimedVfx(consecutiveStartVfx, transform.position, transform.rotation, 3f);

        // 1) Look toward the player.
        if (!string.IsNullOrEmpty(consecutiveAimState)) PlayAnim(consecutiveAimState);
        else CrossFadeTo(idleState);

        float aim = 0f;
        while (aim < consecutiveAimTime)
        {
            FacePlayer();
            aim += Time.deltaTime;
            yield return null;
        }

        // 2) Lock that direction and move forward while hitting.
        Vector3 direction = DirectionToPlayer();
        LockFacing(direction);
        PlayActionAnim(consecutiveState);

        float elapsed = 0f;
        float nextHit = consecutiveFirstHitDelay;
        ConsumeEvent(EvtHit); // Ignore stale events.

        while (elapsed < consecutiveDuration)
        {
            float step = consecutiveMoveSpeed * Time.deltaTime + 0.25f;
            SetManualVelocity(CanTravel(direction, step) ? direction * consecutiveMoveSpeed : Vector3.zero);

            bool doHit = consecutiveHitMode == HitMode.Timer
                ? elapsed >= nextHit
                : ConsumeEvent(EvtHit);

            if (doHit)
            {
                nextHit += consecutiveHitInterval;
                MeleeHit(consecutiveHitBoxOffset, consecutiveHitBoxSize, direction, consecutiveDamageMultiplier);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ClearManualVelocity();
        CrossFadeTo(idleState);
        if (consecutiveRecovery > 0f) yield return new WaitForSeconds(consecutiveRecovery);

        nextSlashTime = Time.time + slashCooldown;
    }

    protected override void OnActionCancelled()
    {
        nextSlashTime = Time.time + slashCooldown;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!debug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(0.7f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, consecutiveStartRange);
        DrawHitBoxGizmo(transform, slashHitBoxOffset, slashHitBoxSize, new Color(1f, 0.3f, 0.3f));
        DrawHitBoxGizmo(transform, consecutiveHitBoxOffset, consecutiveHitBoxSize, new Color(0.7f, 0f, 1f));
    }
}
