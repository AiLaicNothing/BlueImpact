using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Goblin_Range : Enemy
{
    [Header("Goblin Range - Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [Min(0.5f)][SerializeField] private float attackRange = 9f;
    [Min(0f)][SerializeField] private float attackCooldown = 1.2f;
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 20f;
    [SerializeField] private string attackState = "Attack";
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float shootTime = 0.4f;
    [Min(0f)][SerializeField] private float damageMultiplier = 1f;
    [Min(0f)][SerializeField] private float attackRecovery = 0.2f;

    [Header("Goblin Range - Run Away")]
    [Tooltip("If the player is closer than this, the goblin runs away.")]
    [Min(0.1f)][SerializeField] private float tooCloseDistance = 3.5f;
    [Tooltip("Stops running once this far from the player.")]
    [Min(0.5f)][SerializeField] private float safeDistance = 6.5f;
    [Min(0.5f)][SerializeField] private float retreatTravelDistance = 5f;
    [Min(0.5f)][SerializeField] private float retreatSpeedMultiplier = 1.35f;
    [Min(0.1f)][SerializeField] private float maxRetreatDuration = 1.6f;
    [Min(0f)][SerializeField] private float retreatCooldown = 2f;
    [Tooltip("Time the player must stay too close before running (avoids jitter).")]
    [Min(0f)][SerializeField] private float retreatReactionTime = 0.2f;

    private static readonly float[] RetreatAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

    private float nextAttackTime;
    private float nextRetreatTime;
    private float tooCloseTimer;
    private NavMeshPath retreatPath;

    protected override void Awake()
    {
        base.Awake();
        retreatPath = new NavMeshPath();
    }

    protected override void UpdateCombat()
    {
        float distance = FlatDistanceToPlayer();
        bool canSee = CanSeePlayer();

        // 1) Too close -> run away (if a valid escape exists).
        tooCloseTimer = distance < tooCloseDistance ? tooCloseTimer + Time.deltaTime : 0f;
        if (tooCloseTimer >= retreatReactionTime && Time.time >= nextRetreatTime &&
            FindRetreatDestination(out Vector3 escape))
        {
            tooCloseTimer = 0f;
            StartAction(RetreatRoutine(escape));
            return;
        }

        // 2) Out of range / no sight -> get closer.
        if (distance > attackRange || !canSee)
        {
            MoveTo(player.transform.position);
            return;
        }

        // 3) In range -> stay still and shoot.
        StopMoving();
        FacePlayer();
        if (Time.time >= nextAttackTime && IsFacingPlayer(facingTolerance))
            StartAction(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        StopMoving();
        bool shot = false;

        yield return PlayAndWait(attackState, elapsed =>
        {
            if (shot) return;
            FacePlayer(); // Track the player until the shot leaves.
            if (EventOrTime(EvtShoot, elapsed, shootTime))
            {
                shot = true;
                FireAtPlayer();
            }
        });

        if (!shot) FireAtPlayer(); // Missing event safety.

        CrossFadeTo(idleState);
        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);
        nextAttackTime = Time.time + attackCooldown;
    }

    private void FireAtPlayer()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * eyeHeight;
        FireProjectile(projectilePrefab, origin, AimDirection(origin), damageMultiplier);
    }

    private IEnumerator RetreatRoutine(Vector3 escape)
    {
        speedMultiplier = retreatSpeedMultiplier;
        float endTime = Time.time + maxRetreatDuration;

        while (Time.time < endTime && HasValidPlayer())
        {
            MoveTo(escape);
            if (FlatDistanceToPlayer() >= safeDistance) break;
            if (FlatDistance(transform.position, escape) <= arrivalDistance) break;
            yield return null;
        }

        StopMoving();
        nextRetreatTime = Time.time + retreatCooldown;
    }

    // Picks the reachable point that gains the most distance from the player.
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
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * away;
            Vector3 wanted = start.position + direction * retreatTravelDistance;
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

    protected override void OnActionCancelled()
    {
        nextAttackTime = Time.time + attackCooldown;
        nextRetreatTime = Time.time + retreatCooldown * 0.5f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!debug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }
}