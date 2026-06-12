using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Goblin_Melee : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float targetRayHeight = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float facingAngleThreshold = 15f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Hit")]
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 hitBoxPos = new Vector3(0f, 0f, 1f);
    [SerializeField] private float hitTime = 0.35f;
    [SerializeField] private float attackAnimDuration = 0.8f;
    [SerializeField] private float recoveryTime = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float detectionDelay = 0.25f;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private GameObject hitBoxPrefab;

    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;
    private bool isPerformingAction;
    private float detectionTimer;

    private int patrolIndex;
    private int patrolDir = 1;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (isStunned || IsStaggered) return;

        UpdateTarget();
        HandleDetection();
        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
    }

    private void UpdateTarget()
    {
        if (!HasValidPlayer())
        {
            player = FindFirstObjectByType<PlayerControl>();
        }
    }
    private bool HasLineOfSight()
    {
        if (!HasValidPlayer()) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;

        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != player.transform && hit.transform.root != player.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    private void HandleDetection()
    {
        if (safeZone == null) return;

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            detectionTimer = 0f;
            return;
        }

        float distHome = Vector3.Distance(transform.position, safeZone.position);
        float distPlayer = DistanceToPlayer();

        if (!hasDetectedPlayer)
        {
            if (distPlayer <= detectionRange && HasLineOfSight())
            {
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= detectionDelay)
                {
                    hasDetectedPlayer = true;
                    isFollowingPlayer = true;
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else
        {
            if (distHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                detectionTimer = 0f;
            }
        }
    }

    private void HandleMovement()
    {
        if (isFollowingPlayer && HasValidPlayer())
        {
            float distance = DistanceToPlayer();

            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
                RotateToVelocity();
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
                RotateToTarget();
            }
        }
        else
        {
            if (hasPatrol)
            {
                agent.isStopped = false;
                HandlePatrol();
                RotateToVelocity();
            }
            else
            {
                agent.ResetPath();
            }
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        Transform posDesired = patrolZones[patrolIndex];

        agent.SetDestination(posDesired.position);

        float dist = Vector3.Distance(transform.position, posDesired.position);

        if (dist <= stopDistance)
        {
            patrolIndex += patrolDir;

            if (patrolIndex >= patrolZones.Length)
            {
                patrolIndex = patrolZones.Length - 2;
                patrolDir = -1;
            }
            else if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolDir = 1;
            }
        }
    }

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer) return;
        if (!HasValidPlayer()) return;

        float distance = DistanceToPlayer();

        if (distance <= attackRange && IsFacingTarget())
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isPerformingAction = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTargetInstant();

        // anim.SetTrigger("Attack");

        yield return new WaitForSeconds(hitTime);

        FaceTargetInstant();
        DoHit();

        float remaining = attackAnimDuration - hitTime;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        yield return new WaitForSeconds(recoveryTime);

        if (!HasValidPlayer())
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
        }

        if (agent != null)
        {
            agent.isStopped = false;
        }

        isPerformingAction = false;
    }

    private void DoHit()
    {
        Vector3 attackForward = GetAttackForward();
        Vector3 center = transform.position + attackForward * hitBoxPos.z + Vector3.up * hitBoxPos.y;
        Quaternion rot = Quaternion.LookRotation(attackForward);

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, rot);

        ShowHitbox(center, hitBoxSize, rot);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {

                DamageInfo info = new DamageInfo
                {
                    damage = stats.damage,
                    hitDirection = transform.forward,
                };

                damageable.TakeDamage(in info);
            }
        }
    }

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateToTarget()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void FaceTargetInstant()
    {
        if (!HasValidPlayer()) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private bool IsFacingTarget()
    {
        if (!HasValidPlayer()) return false;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= facingAngleThreshold;
    }

    private Vector3 GetAttackForward()
    {
        if (HasValidPlayer())
        {
            Vector3 dir = player.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude < 0.01f) fallback = Vector3.forward;

        return fallback.normalized;
    }

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitBoxPrefab == null) return;

        GameObject box = Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }
}