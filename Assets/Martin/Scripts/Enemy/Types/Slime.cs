using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Slime : Enemy
{
    [Header("Slime - Attack")]
    [Min(0.1f)][SerializeField] private float attackRange = 2.5f;
    [FormerlySerializedAs("facingAngleThreshold")]
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 15f;
    [Min(0f)][SerializeField] private float attackCooldown = 0.2f;
    [Tooltip("Animator state for the attack. If it does not exist, 'Missing Animation Duration' is used.")]
    [SerializeField] private string attackState = "Attack";
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float hitTime = 0.35f;
    [Min(0f)][SerializeField] private float damageMultiplier = 1f;
    [Tooltip("Seconds standing still after the attack, before retreating.")]
    [Min(0f)][SerializeField] private float postAttackStunDuration = 0.3f;

    [Header("Slime - Hitbox")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [FormerlySerializedAs("hitBoxPos")]
    [SerializeField] private Vector3 hitBoxOffset = new Vector3(0f, 0f, 1f);
    [Tooltip("Debug only: spawned for 0.2s where the hitbox was (needs 'debug' ON).")]
    [SerializeField] private GameObject hitBoxPrefab;

    [Header("Slime - Hit and Run")]
    [Min(0.5f)][SerializeField] private float retreatDistance = 5f;
    [Tooltip("Absolute speed while retreating (Move Speed is the chase speed).")]
    [Min(0.1f)][SerializeField] private float retreatSpeed = 7f;
    [Min(0.1f)][SerializeField] private float maxRetreatDuration = 3f;
    [Tooltip("Seconds standing still facing the player after retreating.")]
    [Min(0f)][SerializeField] private float recoveryDuration = 1.5f;

    private static readonly float[] RetreatAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

    private float nextAttackTime;
    private bool inAttackRange;
    private NavMeshPath retreatPath;

    protected override void Awake()
    {
        base.Awake();
        retreatPath = new NavMeshPath();
    }

    protected override void UpdateCombat()
    {
        float distance = FlatDistanceToPlayer();
        if (!InRange(distance, attackRange, ref inAttackRange) || !CanSeePlayer())
        {
            MoveTo(player.transform.position);
            return;
        }

        StopMoving();
        FacePlayer();
        if (Time.time >= nextAttackTime && IsFacingPlayer(facingTolerance))
            StartAction(HitAndRunRoutine());
    }

    // ----------------------------------------------------------------
    // Attack -> Stun -> Retreat -> Recover
    // ----------------------------------------------------------------

    private IEnumerator HitAndRunRoutine()
    {
        // 1) Attack (direction locked at the start of the swing).
        Vector3 direction = DirectionToPlayer();
        LockFacing(direction);
        StopMoving();

        bool hitDone = false;
        yield return PlayAndWait(attackState, elapsed =>
        {
            if (!hitDone && EventOrTime(EvtHit, elapsed, hitTime))
            {
                hitDone = true;
                DoHit(direction);
            }
        });

        CrossFadeTo(idleState);

        // 2) Post-attack stun.
        if (postAttackStunDuration > 0f) yield return new WaitForSeconds(postAttackStunDuration);

        // 3) Retreat away from the player (faces the movement direction).
        UnlockFacing();
        if (HasValidPlayer() && FindRetreatDestination(out Vector3 escape))
        {
            speedMultiplier = retreatSpeed / Mathf.Max(0.1f, moveSpeed);
            float endTime = Time.time + maxRetreatDuration;

            while (Time.time < endTime && HasValidPlayer())
            {
                MoveTo(escape);
                if (FlatDistance(transform.position, escape) <= arrivalDistance) break;
                yield return null;
            }

            StopMoving();
            speedMultiplier = 1f;
        }

        // 4) Recovery: stand still, keep looking at the player.
        CrossFadeTo(idleState);
        float recoverEnd = Time.time + recoveryDuration;
        while (Time.time < recoverEnd)
        {
            FacePlayer();
            yield return null;
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private void DoHit(Vector3 direction)
    {
        MeleeHit(hitBoxOffset, hitBoxSize, direction, damageMultiplier);
        ShowHitbox(direction);
    }

    // Picks the reachable NavMesh point that gains the most distance from the player.
    private bool FindRetreatDestination(out Vector3 best)
    {
        best = transform.position;
        if (!HasValidPlayer()) return false;

        NavMeshQueryFilter filter = NavFilter;
        if (!NavMesh.SamplePosition(GetNavQueryPosition(), out NavMeshHit start, navSampleRadius, filter))
            return false;

        Vector3 away = Flat(transform.position - player.transform.position);
        if (away.sqrMagnitude < 0.01f) away = -Flat(transform.forward);
        away.Normalize();

        float currentDistance = FlatDistanceToPlayer();
        float bestGain = 0.5f;
        bool found = false;

        foreach (float angle in RetreatAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * away;
            Vector3 wanted = start.position + dir * retreatDistance;
            if (!NavMesh.SamplePosition(wanted, out NavMeshHit target, 1.5f, filter)) continue;

            float gain = FlatDistance(target.position, player.transform.position) - currentDistance;
            if (gain <= bestGain) continue;
            if (!NavMesh.CalculatePath(start.position, target.position, filter, retreatPath) ||
                retreatPath.status != NavMeshPathStatus.PathComplete) continue;

            bestGain = gain;
            best = target.position;
            found = true;
        }
        return found;
    }

    private void ShowHitbox(Vector3 direction)
    {
        if (hitBoxPrefab == null || !debug) return;

        Quaternion rotation = Quaternion.LookRotation(Flat(direction).sqrMagnitude > 0.001f ? Flat(direction) : Vector3.forward);
        GameObject box = Instantiate(hitBoxPrefab, transform.position + rotation * hitBoxOffset, rotation);
        box.transform.localScale = hitBoxSize;
        Destroy(box, 0.2f);
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
        Gizmos.color = new Color(1f, 0.9f, 0f);
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
        DrawHitBoxGizmo(transform, hitBoxOffset, hitBoxSize, new Color(1f, 0.3f, 0.3f));
    }
}