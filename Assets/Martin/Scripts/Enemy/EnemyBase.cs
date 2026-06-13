using System;
using System.Collections;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Analytics.IAnalytic;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;

    [Header("Reaction Tuning")]
    [SerializeField] protected float weight = 100f; // the lower the higher force
    [SerializeField] protected float pushResistance = 1f; // the higher the lower force
    [SerializeField] protected float launchResistance = 1f;
    [SerializeField] protected float knockDownResistance = 1f;

    [Header("Reaction")]
    [SerializeField] protected bool immuneToMovement;

    [Header("VFX")]
    [SerializeField] private GameObject vfxHit;
    [SerializeField] private Vector3 hitVfxOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float hitVfxDuration = 1.5f;

    [Header("Ground")]
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected bool isGroundEnemy = true;
    [SerializeField] protected float groundCheckDistance = 0.2f;

    [Header("Recovery")]
    [SerializeField] protected float airRecoverDelay = 0.05f;
    [SerializeField] protected float knockDownRecoverDelay = 0.15f;
    [SerializeField] protected float airHangDuration = 0.75f;
    protected float airHangRemaining;
    protected bool isAirHung;

    [Header("Revenge")]
    [SerializeField] protected int revengeThreshold = 3;
    [SerializeField] protected float revengeDecayDelay = 2f;

    [Header("Components")]
    protected Rigidbody rb;
    protected Animator anim;
    protected NavMeshAgent agent;

    [Header("Runtime State")]
    [SerializeField] protected float currentHp;
    [SerializeField] protected float currentStaggerBuild;
    [SerializeField] protected float revengeMeter;
    protected PlayerControl player;

    protected bool isStunned;
    protected bool isStaggered;
    protected bool isHitStunned;
    protected bool isRevengeMode;
    protected bool isGrounded;
    protected bool isDead;
    protected bool isInReaction;
    protected bool isInStaggerCooldown;

    protected Coroutine stunCoroutine;
    protected Coroutine hitStunCoroutine;
    protected Coroutine staggerCoroutine;
    protected Coroutine reactionCoroutine;
    protected Coroutine airHangCoroutine;
    protected Coroutine revengeDecayCoroutine;

    protected EnemyReactionType currentReaction = EnemyReactionType.None;

    public float CurrentHp => currentHp;
    public bool IsStunned => isStunned;
    public bool IsStaggered => isStaggered;
    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;

    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();

        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (stats != null)
        {
            currentHp = stats.maxHp;
        }

        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
        if (isDead) return;

        CheckGround();
    }
    protected bool HasValidPlayer()
    {
        return player != null && player.gameObject.activeInHierarchy && !player.isDead;
    }

    protected float DistanceToPlayer()
    {
        if (!HasValidPlayer()) return float.MaxValue;

        return Vector3.Distance(transform.position, player.transform.position);
    }
    public void TakeDamage(in DamageInfo info)
    {
        Debug.Log(
    $"TakeDamage | keepInAir:{info.keepInAir} " +
    $"hang:{info.airHangDuration} " +
    $"staggered:{isStaggered} " +
    $"inReaction:{isInReaction}"
);

        if (isDead) return;

        ShowHitVfx();

        currentHp -= info.damage;

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        HandleRevenge();

        bool canReact = HandleStagger(info);

        if (canReact)
        {
            HandleReaction(info);
        }

        if (info.stunDuration > 0f)
        {
            ApplyStun(info.stunDuration);
        }
    }

    protected virtual void HandleRevenge()
    {
        if (isStaggered) return;

        revengeMeter += 1f;

        if (revengeDecayCoroutine != null)
        {
            StopCoroutine(revengeDecayCoroutine);
        }

        revengeDecayCoroutine = StartCoroutine(RevengeDecayRoutine());

        if (!isRevengeMode && revengeMeter >= revengeThreshold)
        {
            EnterRevengeMode();
        }
    }

    protected virtual IEnumerator RevengeDecayRoutine()
    {
        yield return new WaitForSeconds(revengeDecayDelay);
        revengeMeter = 0f;
        revengeDecayCoroutine = null;
    }

    protected virtual void EnterRevengeMode()
    {
        isRevengeMode = true;

        if (hitStunCoroutine != null)
        {
            StopCoroutine(hitStunCoroutine);
            hitStunCoroutine = null;
        }

        isHitStunned = false;
    }

    protected virtual bool HandleStagger(DamageInfo info)
    {
        if (!stats.hasStagger)
            return false;

        if (isInStaggerCooldown)
            return isStaggered;

        if (isStaggered)
        {
            ExtendStagger();
            return true;
        }

        currentStaggerBuild += info.staggerBuild;

        if (currentStaggerBuild >= stats.staggerThreshold)
        {
            TriggerStagger();
            return true;
        }

        return false;
    }

    protected virtual void TriggerStagger()
    {
        if (isStaggered) return;

        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }

    protected virtual void ExtendStagger()
    {
        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }

    protected virtual IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        isStunned = true;

        yield return new WaitForSeconds(stats.staggerDuration);

        isStaggered = false;

        if (stunCoroutine == null)
        {
            isStunned = false;
        }

        isInStaggerCooldown = true;

        yield return new WaitForSeconds(stats.timeResetStagger);

        currentStaggerBuild = 0f;
        isInStaggerCooldown = false;
        staggerCoroutine = null;
    }

    protected virtual void ApplyStun(float duration)
    {
        if (isStaggered) return;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    protected virtual IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        yield return new WaitForSeconds(duration);

        stunCoroutine = null;

        if (!isStaggered)
        {
            isStunned = false;
        }
    }

    protected virtual void HandleReaction(DamageInfo info)
    {
        Debug.Log(
    $"HandleReaction | keepInAir:{info.keepInAir} " +
    $"hang:{info.airHangDuration}");

        if (immuneToMovement) return;

        // Always allow air extension
        if (info.keepInAir)
        {
            SustainAir(info.airHangDuration);
        }

        if (isInReaction) return;

        switch (info.throwType)
        {
            case ThrowType.Push:
                StartReaction(EnemyReactionType.Push, info.hitDirection, info);
                break;

            case ThrowType.Airbone:
                StartReaction(EnemyReactionType.Launch, info.hitDirection, info);
                break;

            case ThrowType.Knockdown:
                StartReaction(EnemyReactionType.KnockDown, info.hitDirection, info);
                break;
        }
    }

    protected virtual void StartReaction(EnemyReactionType type, Vector3 hitDirection, DamageInfo info)
    {

        Debug.Log("Get reaction");
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
        }

        reactionCoroutine = StartCoroutine(ReactionRoutine(type, hitDirection, info));
    }

    protected virtual IEnumerator ReactionRoutine(EnemyReactionType reactionType, Vector3 hitDirection, DamageInfo info)
    {
        isInReaction = true;

        DisableAgent();

        switch (reactionType)
        {
            case EnemyReactionType.Push:
                {
                    ApplyPush(hitDirection, info.pushForce);
                    break;
                }

            case EnemyReactionType.Launch:
                {
                    ApplyLaunch(hitDirection, info.airLiftForce);
                    break;
                }

            case EnemyReactionType.KnockDown:
                {
                    ApplyKnockDown(hitDirection, info.knockDownForce, info.knockDownForwardScale);
                    break;
                }
        }

        switch (reactionType)
        {
            case EnemyReactionType.Push:
                {
                    yield return new WaitForSeconds(info.stunDuration);
                    break;
                }

            case EnemyReactionType.Launch:
                {
                    while (rb.linearVelocity.y > 0.05f)
                    {
                        yield return null;
                    }

                    while (isAirHung)
                    {
                        yield return null;
                    }

                    while (!isGrounded)
                    {
                        yield return null;
                    }

                    yield return new WaitForSeconds(airRecoverDelay);
                    break;
                }

            case EnemyReactionType.KnockDown:
                {
                    while (!isGrounded)
                    {
                        yield return null;
                    }

                    yield return new WaitForSeconds(knockDownRecoverDelay);
                    break;
                }
        }

        rb.linearVelocity = Vector3.zero;

        isInReaction = false;
        reactionCoroutine = null;
    }

    protected virtual void ApplyPush(Vector3 hitDirection, float pushForce)
    {
        if (rb == null) return;

        Debug.Log("Get push");

        Vector3 dir = hitDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        float scaledForce = pushForce * GetPushScale();

        Vector3 force = dir * scaledForce;
        force.y = 0f;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    protected virtual void ApplyLaunch(Vector3 hitDirection, float liftForce)
    {
        if (rb == null) return;

        Debug.Log("Get Launch");

        float scaledLift = liftForce * GetLaunchScale();

        Vector3 dir = hitDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        rb.linearVelocity = new Vector3(0f, 0f, 0f);
        rb.AddForce(Vector3.up * scaledLift, ForceMode.VelocityChange);
    }

    protected virtual void ApplyKnockDown(Vector3 hitDirection, float knockDownForce, float forwardScale)
    {
        if (rb == null) return;

        Debug.Log("Get knockDown");

        float scaledForce = knockDownForce * GetKnockDownScale();

        Vector3 dir = hitDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0f, rb.linearVelocity.y), rb.linearVelocity.z);

        Vector3 downwardForce = Vector3.down * scaledForce;
        Vector3 forwardSlam = dir * (scaledForce * forwardScale);

        rb.AddForce(downwardForce, ForceMode.VelocityChange);
        rb.AddForce(forwardSlam, ForceMode.VelocityChange);
    }

    protected virtual void SustainAir(float hangTime)
    {

        if (rb == null || hangTime <= 0f)
            return;

        // Add time instead of replacing it
        airHangRemaining += hangTime;

        if (airHangCoroutine == null)
        {
            airHangCoroutine = StartCoroutine(AirHangRoutine());
        }
    }
    protected virtual IEnumerator AirHangRoutine()
    {
        isAirHung = true;

        rb.useGravity = false;

        while (airHangRemaining > 0f)
        {

            rb.linearVelocity = new Vector3( rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            airHangRemaining -= Time.deltaTime;

            yield return null;
        }

        rb.useGravity = true;

        isAirHung = false;
        airHangRemaining = 0f;
        airHangCoroutine = null;
    }

    protected virtual float GetPushScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return 1f / (pushResistance * weightFactor);
    }

    protected virtual float GetLaunchScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return 1f / (launchResistance * weightFactor);
    }

    protected virtual float GetKnockDownScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return 1f / (knockDownResistance * weightFactor);
    }

    protected virtual void CheckGround()
    {
        if (groundCheck == null) return;

        if (isAirHung)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, whatIsGround);

        if (!isGrounded) return;

        if (!IsStaggered && !agent.enabled) EnableAgentIfNeeded();
    }

    protected virtual void DisableAgent()
    {
        if (agent == null) return;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    protected virtual void EnableAgentIfNeeded()
    {
        if (agent == null) return;
        if (agent.enabled) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
            agent.Warp(hit.position);
        }
    }

    protected virtual void ShowHitVfx()
    {
        if (vfxHit == null) return;

        Vector3 spawnPos = transform.position + hitVfxOffset;
        GameObject vfx = Instantiate(vfxHit, spawnPos, transform.rotation);

        if (hitVfxDuration > 0f)
        {
            Destroy(vfx, hitVfxDuration);
        }
    }

    protected virtual void UpdateHealthUI()
    {
        // Hook your health bar UI here if needed.
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Destroy(gameObject);
    }
}
