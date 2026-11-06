using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;

    [Header("Normal Hits - Enemy Defined")]
    [Tooltip("Prevents all hit-related movement in both normal and staggered states. Damage and stagger still work.")]
    [SerializeField] protected bool immuneToMovement;
    [Tooltip("If disabled, normal hits will not push the enemy. Staggered attack reactions are unaffected.")]
    [SerializeField] protected bool enableNormalHitRecoil = true;
    [Min(0f)][SerializeField] protected float normalHitPushForce = 1.5f;
    [Min(0.01f)][SerializeField] protected float normalHitPushDuration = 0.18f;
    [Min(0f)][SerializeField] protected float groundPushBraking = 12f;

    [Header("Player Body Collision")]
    [Tooltip("Maximum extra horizontal speed from physically bumping this enemy. Attack reactions are exempt.")]
    [Min(0f)][SerializeField] protected float maxPlayerBodyPushSpeed = 0.18f;
    [Tooltip("Braking for an enemy that is standing still or staggered.")]
    [Min(0f)][SerializeField] protected float standingBrake = 16f;

    [Header("Rigidbody Navigation")]
    [Tooltip("Acceleration of the Rigidbody while following the NavMeshAgent's desired movement.")]
    [Min(0f)][SerializeField] protected float navigationAcceleration = 28f;
    [SerializeField] protected bool rotateToMovement = true;
    [Min(0.1f)][SerializeField] protected float navMeshReconnectRadius = 1.2f;

    [Header("Reaction Recovery")]
    [Min(0f)][SerializeField] protected float airRecoverDelay = 0.05f;
    [Min(0f)][SerializeField] protected float knockDownRecoverDelay = 0.15f;
    [Min(0.1f)][SerializeField] protected float maxAirReactionTime = 5f;
    [Min(0.01f)][SerializeField] protected float minimumAttackPushDuration = 0.2f;

    [Header("Air Juggling")]
    [Range(0f, 1f)][SerializeField] protected float airGravityMultiplier = 0.7f;
    [Min(0f)][SerializeField] protected float airHangDuration = 0.75f;
    protected float airHangRemaining;
    protected bool isAirHung;

    [Header("Ground")]
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected bool isGroundEnemy = true;
    [Min(0f)][SerializeField] protected float groundCheckDistance = 0.2f;

    [Header("Stagger")]
    [Min(1f)][SerializeField] protected float staggeredDamageMultiplier = 1.5f;

    [Header("Patrol")]
    [SerializeField] protected Transform safeZone;
    [SerializeField] protected Transform[] patrolZones;
    [SerializeField] protected bool hasPatrol;
    protected Transform spawnPos;

    [Header("Hit VFX")]
    [SerializeField] private GameObject vfxHit;
    [SerializeField] private Vector3 hitVfxOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float hitVfxDuration = 1.5f;

    [Header("Stagger VFX")]
    [SerializeField] private GameObject staggerVfx;
    [SerializeField] private Vector3 staggerVfxOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float staggerVfxDuration = 1.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource audio_Source;
    [SerializeField] private AudioClip onHit;
    [SerializeField] private AudioClip onStagger;

    [Header("Death Dissolve")]
    [Min(0.01f)][SerializeField] private float durationDissolve = 1.5f;
    [SerializeField] private Material dieMaterial;
    [ColorUsage(true, true)]
    [SerializeField]
    private Color EdgeColor = new Color(0.1875f * 2.33059f, 0.45937f * 2.33059f, 0.75f * 2.33059f, 1f);

    [Header("Broken Material")]
    [SerializeField] private Material brokenMat;
    [ColorUsage(true, true)]
    [SerializeField] private Color brokenColor = new Color(0.33f, 0.75f, 0.414f, 1f);
    [SerializeField] private Vector2 mainTexSpeed = new Vector2(1f, 0f);
    [SerializeField] private float distorsionIntensity = 0.01f;
    [SerializeField] private Vector2 distorsionSpeed = new Vector2(0f, -1f);

    [Header("Debug")]
    [SerializeField] protected bool debug = true;

    // ================================================================
    // REFERENCES AND RUNTIME STATE
    // ================================================================

    protected Rigidbody rb;
    protected Animator anim;
    protected NavMeshAgent agent;
    protected E_BarHolder_UI barUI;
    protected E_HealthBar healthUI;
    protected E_StaggerBar staggerUI;
    protected PlayerControl player;

    [Header("Runtime (Read Only)")]
    [SerializeField] protected float currentHp;
    [SerializeField] protected float currentStaggerBuild;

    protected bool isStunned;
    protected bool isStaggered;
    protected bool isGrounded;
    protected bool isDead;
    protected bool isInReaction;
    protected bool isInStaggerCooldown;
    protected EnemyReactionType currentReaction = EnemyReactionType.None;

    protected Coroutine stunCoroutine;
    protected Coroutine staggerCoroutine;
    protected Coroutine reactionCoroutine;
    protected Coroutine airHangCoroutine;

    private float staggerRemaining;
    private float playerContactUntil;
    private bool agentDisabledByReaction;
    private bool hadDestination;
    private bool agentWasStopped;
    private Vector3 previousDestination;
    private int currentAnimHash;
    private Renderer[] allRenderers;

    private readonly List<Material> clonMaterials = new List<Material>();
    private readonly Dictionary<Renderer, Material> brokenClonMats = new Dictionary<Renderer, Material>();
    private readonly Dictionary<Renderer, Material[]> originalBrokenMaterials = new Dictionary<Renderer, Material[]>();

    private static readonly int DissolveProgressHash = Shader.PropertyToID("_DisolveProgress");
    private static readonly int EdgeColorHash = Shader.PropertyToID("_EdgeColor");
    private static readonly int MainTexHash = Shader.PropertyToID("_MainTex");
    private static readonly int ColorHash = Shader.PropertyToID("_Color");
    private static readonly int MainTexSpeedHash = Shader.PropertyToID("_MainTexSpeed");
    private static readonly int DistorsionIntensityHash = Shader.PropertyToID("_DistorsionIntensity");
    private static readonly int SpeedHash = Shader.PropertyToID("_Speed");

    public float CurrentHp => currentHp;
    public bool IsStunned => isStunned;
    public bool IsStaggered => isStaggered;
    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;

    protected Transform HomePoint => safeZone != null ? safeZone : spawnPos;

    // Bosses can override this so their stagger period cannot be refreshed.
    protected virtual bool CanExtendStagger => true;

    // ================================================================
    // UNITY LIFECYCLE
    // ================================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponentInParent<NavMeshAgent>();

        barUI = GetComponentInChildren<E_BarHolder_UI>();
        healthUI = GetComponentInChildren<E_HealthBar>();
        staggerUI = GetComponentInChildren<E_StaggerBar>();
        allRenderers = GetComponentsInChildren<Renderer>(true);

        if (stats != null) currentHp = stats.maxHp;
        else Debug.LogError($"[EnemyBase] {name} has no EnemyStats.", this);

        if (rb == null)
        {
            Debug.LogError($"[EnemyBase] {name} needs a Rigidbody.", this);
        }
        else
        {
            // The body remains dynamic in navigation AND combat.
            rb.isKinematic = false;
            rb.useGravity = isGroundEnemy;
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (agent != null)
        {
            // Keep only one movement authority: the dynamic Rigidbody.
            agent.updatePosition = false;
            agent.updateRotation = false;

            if (rb != null && agent.transform != rb.transform)
                Debug.LogWarning($"[EnemyBase] {name}: Rigidbody and NavMeshAgent should be on the same root object for this movement setup.", this);
        }
    }

    protected virtual void OnEnable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
        TauntEnemies.OnTauntSpawned += OnPlayerSpawned;
    }

    protected virtual void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        TauntEnemies.OnTauntSpawned -= OnPlayerSpawned;
    }

    protected virtual void Start()
    {
        if (player != null) return;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) found.TryGetComponent(out player);
    }

    protected virtual void Update()
    {
        if (isDead) return;

        CheckGround();

        // Sync the NavMesh simulation to physics, never the reverse.
        if (agent != null && agent.enabled && agent.isOnNavMesh && rb != null) agent.nextPosition = rb.position;

        if (isGrounded || !isGroundEnemy) EnableAgentIfNeeded();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || rb == null) return;

        // Air juggling affects gravity only while the enemy is falling.
        if (isAirHung && rb.linearVelocity.y < 0f)
            rb.AddForce(-Physics.gravity * (1f - airGravityMultiplier), ForceMode.Acceleration);

        if (isInReaction)
        {
            // Only a horizontal push is damped; launch/fall gravity stays natural.
            if (currentReaction == EnemyReactionType.Push && isGrounded)
                BrakeHorizontalMovement(groundPushBraking);

            return;
        }

        if (isGroundEnemy && !isGrounded) return;

        if (isStunned || isStaggered)
        {
            BrakeHorizontalMovement(standingBrake);
            LimitPlayerBodyPush(Vector3.zero);
            return;
        }

        DriveRigidbodyFromNavMesh();
    }

    public void OnPlayerSpawned(PlayerControl spawnedPlayer)
    {
        if (spawnedPlayer != null) player = spawnedPlayer;
    }

    protected bool HasValidPlayer()
    {
        return player != null && player.gameObject.activeInHierarchy && !player.isDead;
    }

    protected float DistanceToPlayer()
    {
        return HasValidPlayer()? Vector3.Distance(transform.position, player.transform.position) : float.MaxValue;
    }

    // ================================================================
    // RIGIDBODY NAVIGATION + PLAYER COLLISION RESISTANCE
    // ================================================================

    private void DriveRigidbodyFromNavMesh()
    {
        Vector3 desired = Vector3.zero;

        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            desired = agent.desiredVelocity;
            desired.y = 0f;
            desired = Vector3.ClampMagnitude(desired, agent.speed);
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        // Accelerate towards navigation velocity instead of teleporting or
        // setting transform.position (which would fight collision physics).
        Vector3 delta = desired - horizontal;
        delta = Vector3.ClampMagnitude(delta, navigationAcceleration * Time.fixedDeltaTime);
        rb.AddForce(delta, ForceMode.VelocityChange);

        LimitPlayerBodyPush(desired);

        if (rotateToMovement && desired.sqrMagnitude > 0.01f && agent != null)
        {
            Quaternion target = Quaternion.LookRotation(desired.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation, target, agent.angularSpeed * Time.fixedDeltaTime));
        }
    }

    private void BrakeHorizontalMovement(float strength)
    {
        if (strength <= 0f) return;

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        rb.AddForce(-horizontal * strength, ForceMode.Acceleration);
    }

    // A grounded character can brush against the enemy, but not shove it
    // across the level. The limit applies ONLY to physical player contacts,
    // NOT to damage-based push/launch/knockdown reactions.
    private void LimitPlayerBodyPush(Vector3 expectedNavigationVelocity)
    {
        if (Time.time > playerContactUntil || isInReaction || (isGroundEnemy && !isGrounded)) return;

        Vector3 velocity = rb.linearVelocity;
        Vector3 actualHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 extraVelocity = actualHorizontal - expectedNavigationVelocity;
        float cap = immuneToMovement ? 0f : maxPlayerBodyPushSpeed;

        if (extraVelocity.sqrMagnitude <= cap * cap) return;

        Vector3 result = expectedNavigationVelocity + Vector3.ClampMagnitude(extraVelocity, cap);
        rb.linearVelocity = new Vector3(result.x, velocity.y, result.z);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        RegisterPlayerBodyContact(collision);
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        RegisterPlayerBodyContact(collision);
    }

    private void RegisterPlayerBodyContact(Collision collision)
    {
        if (collision.collider.GetComponentInParent<PlayerControl>() != null)
            playerContactUntil = Time.time + Time.fixedDeltaTime * 2f;
    }

    // ================================================================
    // ANIMATION HELPERS
    // ================================================================

    protected void PlayAnim(string state)
    {
        if (anim == null) return;
        currentAnimHash = Animator.StringToHash(state);
        anim.Play(currentAnimHash);
    }

    protected void PlayLoop(string state)
    {
        if (anim == null) return;
        int hash = Animator.StringToHash(state);
        if (hash == currentAnimHash) return;
        currentAnimHash = hash;
        anim.Play(hash);
    }

    // ================================================================
    // DAMAGE: NORMAL RECOIL OR STAGGERED ATTACK REACTION
    // ================================================================

    public void TakeDamage(in DamageInfo info)
    {
        if (isDead || stats == null) return;

        ShowHitVfx();
        if (audio_Source != null && onHit != null) audio_Source.PlayOneShot(onHit);

        float damage = isStaggered ? info.damage * staggeredDamageMultiplier : info.damage;
        currentHp -= damage;

        if (barUI != null) barUI.Show();
        if (healthUI != null) healthUI.UpdateHealthBar(currentHp, stats.maxHp);

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        // Returns true if already staggered OR this hit just triggered stagger.
        bool canUseAttackForces = HandleStagger(info);

        if (!immuneToMovement)
        {
            if (canUseAttackForces) HandleReaction(info);        // Staggered: attack-defined push, launch or knockdown.
            else if (enableNormalHitRecoil) ApplyNormalHitRecoil(info);  // Normal: optional enemy-defined small push.
        }

        // Attack stunDuration is deliberately NOT applied to the normal state.
        // During stagger, StaggerRoutine already controls the stunned state.
        if (staggerUI != null && stats.hasStagger) staggerUI.UpdateStaggerhBar(currentStaggerBuild, stats.staggerThreshold);
    }

    protected virtual void ApplyNormalHitRecoil(DamageInfo info)
    {
        if (immuneToMovement || !enableNormalHitRecoil) return;

        StartReaction(EnemyReactionType.Push, info.hitDirection, info);
    }

    // ================================================================
    // ATTACK REACTIONS
    // ================================================================

    protected virtual void HandleReaction(DamageInfo info)
    {
        if (immuneToMovement || rb == null) return;

        if (info.throwType == ThrowType.Knockdown) StopAirHang();
        else if (info.keepInAir && (!isGrounded || info.throwType == ThrowType.Airbone))
            SustainAir(info.airHangDuration > 0f ? info.airHangDuration : airHangDuration);

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

            default:
                if (!isGrounded && info.keepInAir)
                {
                    // A follow-up hit refreshes the juggle without a new throw.
                    if (reactionCoroutine == null) StartReaction(EnemyReactionType.Launch, info.hitDirection, info);
                    else AirJuggle(info.airLiftForce);
                }
                else
                {
                    // No special throw: a modest push is still visible.
                    StartReaction(EnemyReactionType.Push, info.hitDirection, info);
                }
                break;
        }
    }

    protected virtual void StartReaction(EnemyReactionType type, Vector3 hitDirection, DamageInfo info)
    {
        if (isDead || immuneToMovement || rb == null) return;

        // New hits can smoothly redirect the old reaction without waiting.
        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);

        reactionCoroutine = StartCoroutine(ReactionRoutine(type, hitDirection, info));
    }

    protected virtual IEnumerator ReactionRoutine(
        EnemyReactionType reactionType, Vector3 hitDirection, DamageInfo info)
    {
        isInReaction = true;
        currentReaction = reactionType;

        // In the normal state, pause navigation but keep its current path.
        // In the staggered state, the agent is disabled until recovery.
        if (isStaggered) DisableAgent();

        switch (reactionType)
        {
            case EnemyReactionType.Push:
                {
                    bool weakRecoil = !isStaggered;
                    float force = weakRecoil ? normalHitPushForce :
                        (info.pushForce > 0f ? info.pushForce : normalHitPushForce);

                    if (weakRecoil)
                    {
                        // Normal attacks give a consistent short recoil, regardless
                        // of the enemy's current walking/navigation velocity.
                        Vector3 v = rb.linearVelocity;
                        rb.linearVelocity = new Vector3(0f, v.y, 0f);
                    }

                    ApplyPush(hitDirection, force);

                    float duration = weakRecoil
                        ? normalHitPushDuration
                        : Mathf.Max(minimumAttackPushDuration, info.stunDuration);

                    yield return new WaitForSeconds(duration);
                    break;
                }

            case EnemyReactionType.Launch:
                {
                    if (!isGrounded && info.keepInAir)
                        AirJuggle(info.airLiftForce);
                    else
                        ApplyLaunch(hitDirection, info.airLiftForce);

                    // Allow physics to register the upward movement before
                    // checking grounded state.
                    yield return new WaitForFixedUpdate();
                    yield return WaitForLanding();
                    if (!isDead && airRecoverDelay > 0f)
                        yield return new WaitForSeconds(airRecoverDelay);
                    break;
                }

            case EnemyReactionType.KnockDown:
                {
                    ApplyKnockDown(hitDirection, info.knockDownForce, info.knockDownForwardScale);
                    yield return new WaitForFixedUpdate();
                    yield return WaitForLanding();
                    if (!isDead && knockDownRecoverDelay > 0f)
                        yield return new WaitForSeconds(knockDownRecoverDelay);
                    break;
                }
        }

        isInReaction = false;
        currentReaction = EnemyReactionType.None;
        reactionCoroutine = null;

        // This method refuses to resume the NavMesh while still staggered.
        EnableAgentIfNeeded();
    }

    private IEnumerator WaitForLanding()
    {
        float elapsed = 0f;
        bool leftGround = !isGrounded;

        while (!isDead && elapsed < maxAirReactionTime)
        {
            if (!isGrounded) leftGround = true;
            if (leftGround && isGrounded && !isAirHung) break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    protected virtual void ApplyPush(Vector3 hitDirection, float pushForce)
    {
        if (rb == null || pushForce <= 0f) return;
        rb.AddForce(GetHorizontalDirection(hitDirection) * pushForce,
            ForceMode.VelocityChange);
    }

    protected virtual void ApplyLaunch(Vector3 hitDirection, float liftForce)
    {
        if (rb == null || liftForce <= 0f) return;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f)
        {
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        rb.AddForce(Vector3.up * liftForce, ForceMode.VelocityChange);
    }

    protected virtual void ApplyKnockDown(
        Vector3 hitDirection, float knockDownForce, float forwardScale)
    {
        if (rb == null || knockDownForce <= 0f) return;

        Vector3 velocityChange = Vector3.down * knockDownForce;
        velocityChange += GetHorizontalDirection(hitDirection) * (knockDownForce * forwardScale);

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    protected virtual void SustainAir(float hangTime)
    {
        if (rb == null || hangTime <= 0f) return;

        airHangRemaining = Mathf.Max(airHangRemaining, hangTime);
        if (airHangCoroutine == null) airHangCoroutine = StartCoroutine(AirHangRoutine());
    }

    protected virtual IEnumerator AirHangRoutine()
    {
        isAirHung = true;
        while (!isDead && airHangRemaining > 0f)
        {
            airHangRemaining -= Time.deltaTime;
            yield return null;
        }

        isAirHung = false;
        airHangRemaining = 0f;
        airHangCoroutine = null;
    }

    private void StopAirHang()
    {
        if (airHangCoroutine != null) StopCoroutine(airHangCoroutine);
        airHangCoroutine = null;
        airHangRemaining = 0f;
        isAirHung = false;
    }

    protected virtual void AirJuggle(float lift)
    {
        if (rb == null || lift <= 0f) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, 0f);
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * lift, ForceMode.VelocityChange);
    }

    protected Vector3 GetHorizontalDirection(Vector3 hitDirection)
    {
        Vector3 direction = hitDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f && HasValidPlayer())
        {
            direction = transform.position - player.transform.position;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

        return direction.normalized;
    }

    // ================================================================
    // GROUND DETECTION AND NAVMESH RECONNECTION
    // ================================================================

    protected virtual void CheckGround()
    {
        if (groundCheck == null) return;

        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, whatIsGround, QueryTriggerInteraction.Ignore);
    }

    protected virtual void DisableAgent()
    {
        if (agent == null || !agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            hadDestination = agent.hasPath;
            if (hadDestination) previousDestination = agent.destination;
            agentWasStopped = agent.isStopped;
        }
        else
        {
            hadDestination = false;
            agentWasStopped = false;
        }

        agent.enabled = false;
        agentDisabledByReaction = true;
    }

    protected virtual void EnableAgentIfNeeded()
    {
        if (agent == null || !agentDisabledByReaction) return;
        if (isDead || isInReaction || isStunned || isStaggered || isAirHung ||
            (isGroundEnemy && !isGrounded))
            return;

        if (!NavMesh.SamplePosition(rb.position, out NavMeshHit hit,
                navMeshReconnectRadius, agent.areaMask))
            return;

        agent.enabled = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.Warp(hit.position);
        agentDisabledByReaction = false;

        if (hadDestination && agent.isOnNavMesh) agent.SetDestination(previousDestination);

        agent.isStopped = agentWasStopped;
        hadDestination = false;
    }

    // ================================================================
    // STUN + STAGGER
    // ================================================================

    protected virtual void ApplyStun(float duration)
    {
        if (isStaggered || duration <= 0f) return;

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    protected virtual IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        DisableAgent();
        yield return new WaitForSeconds(duration);

        stunCoroutine = null;
        if (!isStaggered) isStunned = false;
        EnableAgentIfNeeded();
    }

    protected virtual bool HandleStagger(DamageInfo info)
    {
        if (stats == null || !stats.hasStagger) return false;
        if (isInStaggerCooldown) return isStaggered;

        if (isStaggered)
        {
            if (CanExtendStagger) ExtendStagger();
            return true;
        }

        currentStaggerBuild += info.staggerBuild;
        if (currentStaggerBuild < stats.staggerThreshold) return false;

        if (audio_Source != null && onStagger != null) audio_Source.PlayOneShot(onStagger);

        ShowStaggerVfx();
        TriggerStagger();
        return true;
    }

    protected virtual void TriggerStagger()
    {
        if (isStaggered) return;

        isStaggered = true;
        isStunned = true;
        staggerRemaining = stats.staggerDuration;

        DisableAgent();
        AddBrokenMaterial();

        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }

    protected virtual void ExtendStagger()
    {
        staggerRemaining = Mathf.Max(staggerRemaining, stats.staggerDuration);
    }

    protected virtual IEnumerator StaggerRoutine()
    {
        while (!isDead && staggerRemaining > 0f)
        {
            staggerRemaining -= Time.deltaTime;
            yield return null;
        }

        isStaggered = false;
        RemoveBrokenMaterial();
        if (stunCoroutine == null) isStunned = false;

        isInStaggerCooldown = true;
        EnableAgentIfNeeded();

        yield return new WaitForSeconds(stats.timeResetStagger);

        currentStaggerBuild = 0f;
        isInStaggerCooldown = false;
        staggerCoroutine = null;

        if (staggerUI != null) staggerUI.UpdateStaggerhBar(currentStaggerBuild, stats.staggerThreshold);
    }

    // ================================================================
    // VFX AND MATERIALS
    // ================================================================

    protected virtual void ShowHitVfx()
    {
        SpawnTimedVfx(vfxHit, hitVfxOffset, hitVfxDuration);
    }

    protected virtual void ShowStaggerVfx()
    {
        SpawnTimedVfx(staggerVfx, staggerVfxOffset, staggerVfxDuration);
    }

    private void SpawnTimedVfx(GameObject prefab, Vector3 offset, float duration)
    {
        if (prefab == null) return;

        GameObject effect = Instantiate(prefab, transform.position + offset, transform.rotation);
        if (duration > 0f) Destroy(effect, duration);
    }

    private void AddBrokenMaterial()
    {
        if (brokenMat == null || originalBrokenMaterials.Count > 0) return;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            Material[] original = renderer.sharedMaterials;
            originalBrokenMaterials[renderer] = original;

            Material[] withBroken = new Material[original.Length + 1];
            Array.Copy(original, withBroken, original.Length);

            Material clone = new Material(brokenMat);
            if (clone.HasProperty(ColorHash)) clone.SetColor(ColorHash, brokenColor);
            if (clone.HasProperty(MainTexSpeedHash)) clone.SetVector(MainTexSpeedHash, mainTexSpeed);
            if (clone.HasProperty(DistorsionIntensityHash))
                clone.SetFloat(DistorsionIntensityHash, distorsionIntensity);
            if (clone.HasProperty(SpeedHash)) clone.SetVector(SpeedHash, distorsionSpeed);

            withBroken[withBroken.Length - 1] = clone;
            brokenClonMats[renderer] = clone;
            renderer.sharedMaterials = withBroken;
        }
    }

    private void RemoveBrokenMaterial()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in originalBrokenMaterials)
        {
            if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        }
        originalBrokenMaterials.Clear();

        foreach (Material material in brokenClonMats.Values)
        {
            if (material != null) Destroy(material);
        }
        brokenClonMats.Clear();
    }

    // ================================================================
    // DEATH
    // ================================================================

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        StopEnemyRoutines();
        DisableAgent();
        DisablePhysicsOnDeath();
        StartDeadEffect();
    }

    // Subclasses overriding Die() should use base.Die().
    protected void StopEnemyRoutines()
    {
        StopAllCoroutines();

        stunCoroutine = null;
        staggerCoroutine = null;
        reactionCoroutine = null;
        airHangCoroutine = null;

        isStunned = false;
        isInReaction = false;
        isAirHung = false;
        airHangRemaining = 0f;
        currentReaction = EnemyReactionType.None;
    }

    private void DisablePhysicsOnDeath()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }

    private void StartDeadEffect()
    {
        if (brokenClonMats.Count > 0) RemoveBrokenMaterial();
        ChangeMaterials();
        StartCoroutine(DieEffect());
    }

    private void ChangeMaterials()
    {
        if (dieMaterial == null) return;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            Material[] original = renderer.sharedMaterials;
            Material[] dissolve = new Material[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                Material clone = new Material(dieMaterial);
                if (clone.HasProperty(DissolveProgressHash)) clone.SetFloat(DissolveProgressHash, 0f);

                if (clone.HasProperty(EdgeColorHash)) clone.SetColor(EdgeColorHash, EdgeColor);

                if (original[i] != null && original[i].HasProperty(MainTexHash) &&
                    clone.HasProperty(MainTexHash))
                {
                    Texture texture = original[i].GetTexture(MainTexHash);
                    if (texture != null) clone.SetTexture(MainTexHash, texture);
                }

                dissolve[i] = clone;
                clonMaterials.Add(clone);
            }

            renderer.sharedMaterials = dissolve;
        }
    }

    private IEnumerator DieEffect()
    {
        float elapsed = 0f;

        while (elapsed < durationDissolve)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / durationDissolve);

            foreach (Material material in clonMaterials)
            {
                if (material != null && material.HasProperty(DissolveProgressHash))
                    material.SetFloat(DissolveProgressHash, progress);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (Material material in clonMaterials)
        {
            if (material != null) Destroy(material);
        }
        clonMaterials.Clear();

        foreach (Material material in brokenClonMats.Values)
        {
            if (material != null) Destroy(material);
        }
        brokenClonMats.Clear();
        originalBrokenMaterials.Clear();
    }

    // ================================================================
    // SPAWN CONFIGURATION
    // ================================================================

    public void OnSpawn(Transform[] PatrolPoints, Transform safePoint, bool hasPatrol, Transform spawnPos)
    {
        patrolZones = PatrolPoints;
        safeZone = safePoint;
        this.hasPatrol = hasPatrol;
        this.spawnPos = spawnPos;
    }
}

