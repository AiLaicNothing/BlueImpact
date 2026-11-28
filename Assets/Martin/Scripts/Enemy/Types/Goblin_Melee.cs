using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goblin_Melee : Enemy
{
    [Header("Goblin Melee - Attack")]
    [Min(0.1f)][SerializeField] private float attackRange = 1.8f;
    [Min(0f)][SerializeField] private float attackCooldown = 0.4f;
    [Range(1f, 180f)][SerializeField] private float facingTolerance = 20f;
    [SerializeField] private string attackState = "Attack";
    [Tooltip("Only used when 'Use Animation Events' is OFF.")]
    [Min(0f)][SerializeField] private float hitTime = 0.35f;
    [Min(0f)][SerializeField] private float damageMultiplier = 1f;

    [Header("Goblin Melee - Still After Attack")]
    [Tooltip("Seconds the goblin stays still after attacking before chasing again.")]
    [Min(0f)][SerializeField] private float stillTimeAfterAttack = 1.2f;

    [Header("Goblin Melee - Hitbox")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1.2f, 1f, 1.2f);
    [SerializeField] private Vector3 hitBoxOffset = new Vector3(0f, 1f, 1f);

    private float nextAttackTime;

    protected override void UpdateCombat()
    {
        bool inRange = FlatDistanceToPlayer() <= attackRange && CanSeePlayer();

        if (!inRange)
        {
            MoveTo(player.transform.position);
            return;
        }

        StopMoving();
        FacePlayer();

        if (Time.time >= nextAttackTime && IsFacingPlayer(facingTolerance))
            StartAction(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        Vector3 attackDirection = DirectionToPlayer();
        LockFacing(attackDirection); // Do not track the player during the swing.
        StopMoving();

        bool hitDone = false;
        yield return PlayAndWait(attackState, elapsed =>
        {
            if (!hitDone && EventOrTime(EvtHit, elapsed, hitTime))
            {
                hitDone = true;
                MeleeHit(hitBoxOffset, hitBoxSize, attackDirection, damageMultiplier);
            }
        });

        CrossFadeTo(idleState);

        // Stay still X seconds, then the brain resumes chasing.
        if (stillTimeAfterAttack > 0f) yield return new WaitForSeconds(stillTimeAfterAttack);

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
        DrawHitBoxGizmo(transform, hitBoxOffset, hitBoxSize, new Color(1f, 0.3f, 0.3f));
    }
}


