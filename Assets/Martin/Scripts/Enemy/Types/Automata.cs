using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Automata : Enemy
{
    private enum AttackMode { Random, BurstOnly, SurpriseOnly }

    [Header("Automata - Common")]
    [Min(0.5f)][SerializeField] private float attackRange = 8f;
    [Min(0f)][SerializeField] private float attackCooldown = 1.2f;
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 20f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [Tooltip("Chance of BurstFire (the rest is AttackSurprise).")]
    [Range(0f, 1f)][SerializeField] private float burstChance = 0.6f;
    [Tooltip("For testing a single attack.")]
    [SerializeField] private AttackMode attackMode = AttackMode.Random;
    [Min(0f)][SerializeField] private float attackRecovery = 0.25f;

    [Header("Automata - BurstFire")]
    [SerializeField] private string burstState = "BurstFire";
    [Tooltip("Only used when 'Use Animation Events' is OFF. Burst starts on the 'Shoot' event otherwise.")]
    [Min(0f)][SerializeField] private float burstStartTime = 0.3f;
    [Min(1)][SerializeField] private int burstCount = 4;
    [Min(0.01f)][SerializeField] private float burstInterval = 0.14f;
    [SerializeField] private bool trackPlayerDuringBurst = true;
    [Min(0f)][SerializeField] private float burstDamageMultiplier = 0.6f;

    [Header("Automata - AttackSurprise (Dash)")]
    [Tooltip("AttackSurprise is only chosen when farther than this.")]
    [Min(0f)][SerializeField] private float surpriseMinDistance = 4f;
    [SerializeField] private string dashState = "Dash";
    [Min(0f)][SerializeField] private float dashWindup = 0.2f;
    [Min(0.1f)][SerializeField] private float dashSpeed = 12f;
    [Min(0.05f)][SerializeField] private float dashMaxDuration = 0.6f;
    [Tooltip("Stops dashing when this close to the player.")]
    [Min(0.1f)][SerializeField] private float dashStopDistance = 2.2f;
    [Tooltip("Re-aims the dash toward the player while moving.")]
    [SerializeField] private bool dashHoming = true;

    [Header("Automata - AttackSurprise (Spread)")]
    [SerializeField] private string spreadState = "Spread";
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float spreadFireTime = 0.2f;
    [Min(1)][SerializeField] private int spreadCount = 5;
    [Range(0f, 180f)][SerializeField] private float spreadAngle = 50f;
    [Min(0f)][SerializeField] private float spreadDamageMultiplier = 0.8f;

    private float nextAttackTime;
    private bool inAttackRange;

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
        if (Time.time < nextAttackTime || !IsFacingPlayer(facingTolerance)) return;

        StartAction(ChooseSurprise(distance) ? SurpriseRoutine() : BurstRoutine());
    }

    private bool ChooseSurprise(float distance)
    {
        bool surpriseAllowed = distance > surpriseMinDistance;
        switch (attackMode)
        {
            case AttackMode.BurstOnly: return false;
            case AttackMode.SurpriseOnly: return surpriseAllowed;
            default: return surpriseAllowed && Random.value >= burstChance;
        }
    }

    // ----------------------------------------------------------------
    // BurstFire
    // ----------------------------------------------------------------

    private IEnumerator BurstRoutine()
    {
        StopMoving();
        PlayActionAnim(burstState);

        float start = Time.time;
        bool started = false;
        int fired = 0;
        float nextShot = 0f;
        float hardLimit = animationSafetyTimeout + burstCount * burstInterval;

        while (Time.time - start < hardLimit)
        {
            float elapsed = Time.time - start;
            if (trackPlayerDuringBurst) FacePlayer();

            bool animDone = ActionAnimFinished();
            if (!started && (EventOrTime(EvtShoot, elapsed, burstStartTime) || animDone))
            {
                started = true;
                nextShot = Time.time;
            }

            if (started && fired < burstCount && Time.time >= nextShot)
            {
                FireFromGun(0f, burstDamageMultiplier);
                fired++;
                nextShot = Time.time + burstInterval;
            }

            if (fired >= burstCount && animDone) break;
            yield return null;
        }

        yield return Recover();
    }

    // ----------------------------------------------------------------
    // AttackSurprise: dash in, then spread shot
    // ----------------------------------------------------------------

    private IEnumerator SurpriseRoutine()
    {
        StopMoving();
        PlayActionAnim(dashState);

        // Wind-up: face the player.
        float t = 0f;
        while (t < dashWindup)
        {
            FacePlayer();
            t += Time.deltaTime;
            yield return null;
        }

        // Dash toward the player.
        Vector3 direction = DirectionToPlayer();
        LockFacing(direction);
        float dashTime = 0f;

        while (dashTime < dashMaxDuration && HasValidPlayer())
        {
            if (dashHoming)
            {
                direction = DirectionToPlayer();
                LockFacing(direction);
            }

            if (FlatDistanceToPlayer() <= dashStopDistance) break;
            if (!CanTravel(direction, dashSpeed * Time.deltaTime + 0.3f)) break;

            SetManualVelocity(direction * dashSpeed);
            dashTime += Time.deltaTime;
            yield return null;
        }

        ClearManualVelocity();
        UnlockFacing();

        // Spread shot.
        bool fired = false;
        float spreadStart = Time.time;
        PlayActionAnim(spreadState);
        do
        {
            if (!fired) FacePlayer();
            if (!fired && EventOrTime(EvtShoot, Time.time - spreadStart, spreadFireTime))
            {
                fired = true;
                FireSpread();
            }
            yield return null;
        }
        while (!ActionAnimFinished());

        if (!fired) FireSpread();
        yield return Recover();
    }

    private void FireSpread()
    {
        int count = Mathf.Max(1, spreadCount);
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, i / (float)(count - 1));
            FireFromGun(angle, spreadDamageMultiplier);
        }
    }

    private void FireFromGun(float yawOffset, float damageMultiplier)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * eyeHeight;
        Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * AimDirection(origin);
        FireProjectile(projectilePrefab, origin, direction, damageMultiplier);
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
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, surpriseMinDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dashStopDistance);
    }
}
