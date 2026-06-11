using System;
using System.Collections;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;

    [Header("Reaction Tuning")]
    [SerializeField] protected float weight = 100f;
    [SerializeField] protected float pushResistance = 1f;
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
    protected Coroutine revengeDecayCoroutine;

    protected EnemyReactionType currentReaction = EnemyReactionType.None;

    public float CurrentHp => currentHp;
    public bool IsStunned => isStunned;
    public bool IsStaggered => isStaggered;
    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;

    protected virtual void Awake()
    {
        player = FindFirstObjectByType<PlayerControl>();

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
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    protected virtual void Start()
    {
        if (agent != null && !isGroundEnemy)
        {
            agent.enabled = false;
        }
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

    public virtual void TakeDamage(HitData hitData, Vector3 hitDirection, Transform attacker = null)
    {
        if (isDead) return;
        if (hitData == null) return;

        ShowHitVfx();

        //float damage = CalculateDamage(hitData);
        //currentHp -= damage;

        UpdateHealthUI();

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        HandleRevenge(hitData);
        HandleStagger(hitData);
        HandleReaction(hitData, hitDirection);

        if (hitData.stunDuration > 0f)
        {
            ApplyStun(hitData.stunDuration);
        }
    }

    //protected virtual float CalculateDamage(HitData hitData)
    //{
    //    float baseDamage = ((stats.physicalDamage * hitData.physicalScale) +
    //                        (stats.magicalDamage * hitData.magicalScale)) *
    //                        hitData.damageMultiplier;

    //    if (isStaggered)
    //    {
    //        baseDamage *= 1.5f;
    //    }

    //    return baseDamage;
    //}

    protected virtual void HandleRevenge(HitData hitData)
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

    protected virtual void HandleStagger(HitData hitData)
    {
        if (!stats.hasStagger) return;
        if (isInStaggerCooldown) return;

        if (isStaggered)
        {
            ExtendStagger();
            return;
        }

        currentStaggerBuild += hitData.staggerCharge;

        if (currentStaggerBuild >= stats.staggerThreshold)
        {
            TriggerStagger();
        }
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

    protected virtual void HandleReaction(HitData hitData, Vector3 hitDirection)
    {
        if (immuneToMovement)
            return;

        if (isInReaction)
            return;

        switch (hitData.throwType)
        {
            case ThrowType.Push:
                StartReaction(EnemyReactionType.Push, hitDirection, hitData);
                break;

            case ThrowType.Airbone:
                StartReaction(EnemyReactionType.Launch, hitDirection, hitData);
                break;

            case ThrowType.Knockdown:
                StartReaction(EnemyReactionType.KnockDown, hitDirection, hitData);
                break;
        }

        if (hitData.keepInAir)
        {
            SustainAir(hitData.airLiftForce);
        }
    }

    protected virtual void StartReaction(EnemyReactionType type, Vector3 hitDirection, HitData hitData)
    {
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
        }

        reactionCoroutine = StartCoroutine(ReactionRoutine(type, hitDirection, hitData));
    }

    protected virtual IEnumerator ReactionRoutine(EnemyReactionType type, Vector3 hitDirection, HitData hitData)
    {
        isInReaction = true;
        DisableAgent();

        switch (type)
        {
            case EnemyReactionType.Push:
                ApplyPush(hitDirection, hitData.pushForce);
                break;

            case EnemyReactionType.Launch:
                ApplyLaunch(hitDirection, hitData.airLiftForce);
                break;

            case EnemyReactionType.KnockDown:
                ApplyKnockDown(hitDirection, hitData.knockDownForce, hitData.knockDownForwardScale);
                break;
        }

        if (type == EnemyReactionType.Launch)
        {
            while (rb.linearVelocity.y > 0.1f)
            {
                yield return null;
            }

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            yield return new WaitForSeconds(airRecoverDelay);
        }
        else if (type == EnemyReactionType.KnockDown)
        {
            yield return new WaitForSeconds(knockDownRecoverDelay);
        }
        else
        {
            yield return new WaitForSeconds(hitData.stunDuration);
        }

        EnableAgentIfNeeded();

        isInReaction = false;
        reactionCoroutine = null;
    }

    protected virtual void ApplyPush(Vector3 hitDirection, float pushForce)
    {
        if (rb == null) return;

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
        rb.AddForce(force, ForceMode.Impulse);
    }

    protected virtual void ApplyLaunch(Vector3 hitDirection, float liftForce)
    {
        if (rb == null) return;

        float scaledLift = liftForce * GetLaunchScale();

        Vector3 dir = hitDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        rb.linearVelocity = new Vector3(0f, 0f, 0f);
        rb.AddForce(Vector3.up * scaledLift, ForceMode.Impulse);
        rb.AddForce(dir * (scaledLift * 0.25f), ForceMode.Impulse);
    }

    protected virtual void ApplyKnockDown(Vector3 hitDirection, float knockDownForce, float forwardScale)
    {
        if (rb == null) return;

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

        rb.AddForce(downwardForce, ForceMode.Impulse);
        rb.AddForce(forwardSlam, ForceMode.Impulse);
    }

    protected virtual void SustainAir(float lift)
    {
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;

        if (vel.y < 0f)
        {
            vel.y = 0f;
        }

        vel.y += lift;
        rb.linearVelocity = vel;
    }

    protected virtual float GetPushScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return Mathf.Max(0.1f, pushResistance * weightFactor);
    }

    protected virtual float GetLaunchScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return Mathf.Max(0.1f, launchResistance * weightFactor);
    }

    protected virtual float GetKnockDownScale()
    {
        float weightFactor = Mathf.Max(0.1f, weight / 100f);
        return Mathf.Max(0.1f, knockDownResistance * weightFactor);
    }

    protected virtual void CheckGround()
    {
        if (groundCheck == null) return;

        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, whatIsGround);
    }

    protected virtual void DisableAgent()
    {
        if (agent == null) return;

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    protected virtual void EnableAgentIfNeeded()
    {
        if (!isGroundEnemy) return;
        if (agent == null) return;

        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
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

    public void ForceKnockDown(Vector3 hitDirection, float force)
    {
        if (isDead) return;
        StartReaction(EnemyReactionType.KnockDown, hitDirection, new HitData
        {
            knockDownForce = force,
            stunDuration = 0.2f
        });
    }
}
