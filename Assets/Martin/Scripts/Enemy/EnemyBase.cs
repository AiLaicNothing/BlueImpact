using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("StaggerVfx")]
    [SerializeField] private GameObject staggerVfx;
    [SerializeField] private Vector3 staggerVfxOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float staggerVfxDuration = 1.5f;

    [Header("Sfx")]
    [SerializeField] private AudioSource audio_Source;
    [SerializeField] private AudioClip onHit;
    [SerializeField] private AudioClip onStagger;

    [Header("Ground")]
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected bool isGroundEnemy = true;
    [SerializeField] protected float groundCheckDistance = 0.2f;

    [Header("Recovery")]
    [SerializeField] protected float airRecoverDelay = 0.05f;
    [SerializeField] protected float knockDownRecoverDelay = 0.15f;

    [Header("Air Hang")]
    [SerializeField] protected float airGravityMultiplier = 0.7f;
    private float originalDrag;
    [SerializeField] protected float airHangDuration = 0.75f;
    protected float airHangRemaining;
    protected bool isAirHung;

    [Header("Stagger Tuning")]
    [SerializeField] protected float staggeredDamageMultiplier = 1.5f; // dano extra mientras esta staggered

    [Header("Revenge")]
    [SerializeField] protected int revengeThreshold = 3;
    [SerializeField] protected float revengeDecayDelay = 2f;

    [Header("Patrol")]
    [SerializeField] protected Transform safeZone;
    [SerializeField] protected Transform[] patrolZones;
    [SerializeField] protected bool hasPatrol;
    protected Transform spawnPos;

    [Header("Debug")]
    [SerializeField] protected bool debug = true;

    [Header("Components")]
    protected Rigidbody rb;
    protected Animator anim;
    protected NavMeshAgent agent;
    protected E_BarHolder_UI barUI;
    protected E_HealthBar healthUI;
    protected E_StaggerBar staggerUI;

    [Header("Runtime State")]
    [SerializeField] protected float currentHp;
    [SerializeField] protected float currentStaggerBuild;
    [SerializeField] protected float revengeMeter;
    protected PlayerControl player;

    protected bool isStunned;
    protected bool isStaggered;
    protected bool isRevengeMode;
    protected bool isGrounded;
    protected bool isDead;
    protected bool isInReaction;
    protected bool isInStaggerCooldown;

    protected Coroutine stunCoroutine;
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

    [Header("Dead Effect")]
    [SerializeField] float durationDissolve = 1.5f;
    static readonly int DissolveProgressHash = Shader.PropertyToID("_DisolveProgress");
    static readonly int EdgeColorHash = Shader.PropertyToID("_EdgeColor");
    static readonly int MainTexHash = Shader.PropertyToID("_MainTex");
    [SerializeField] Material dieMaterial;
    [ColorUsage(true, true)]
    [SerializeField] Color EdgeColor = new Color(0.1875f * 2.33059f, 0.45937f * 2.33059f, 0.75f * 2.33059f, 1f);
    private List<Material> clonMaterials = new List<Material>();
    private Renderer[] allRenderers;

    [Header("Broken Effect")]
    [SerializeField] Material brokenMat;
    [ColorUsage(true, true)]
    [SerializeField] Color brokenColor = new Color(0.33f, 0.75f, 0.414f, 1f);
    [SerializeField] Vector2 mainTexSpeed = new Vector2(1, 0);
    [SerializeField] float distorsionIntensity = 0.01f;
    [SerializeField] Vector2 distorsionSpeed = new Vector2(0, -1);
    static readonly int ColorHash = Shader.PropertyToID("_Color");
    static readonly int MainTexSpeedHash = Shader.PropertyToID("_MainTexSpeed");
    static readonly int DistorsionIntensityHash = Shader.PropertyToID("_DistorsionIntensity");
    static readonly int SpeedHash = Shader.PropertyToID("_Speed");
    private Dictionary<Renderer, Material> brokenClonMats = new Dictionary<Renderer, Material>();

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

    public void OnPlayerSpawned(PlayerControl spawnedPlayer)
    {
        if (spawnedPlayer == null) return;
        player = spawnedPlayer;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponentInParent<NavMeshAgent>();

        barUI = GetComponentInChildren<E_BarHolder_UI>();
        healthUI = GetComponentInChildren<E_HealthBar>();
        staggerUI = GetComponentInChildren<E_StaggerBar>();
        allRenderers = GetComponentsInChildren<Renderer>(true);

        if (stats != null)
        {
            currentHp = stats.maxHp;
        }
        else
        {
            Debug.LogError($"[EnemyBase] '{name}' no tiene EnemyStats asignado. No recibirá daño.", this);
        }

        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    protected virtual void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerObj.TryGetComponent(out player);
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

    // Punto de "casa" del enemigo: la zona segura si existe, si no el punto de spawn
    protected Transform HomePoint => safeZone != null ? safeZone : spawnPos;

    // Animaciones de un solo uso (ataques, reacciones): siempre reinician el estado
    private int currentAnimHash;

    protected void PlayAnim(string state)
    {
        if (anim == null) return;

        currentAnimHash = Animator.StringToHash(state);
        anim.Play(currentAnimHash);
    }

    // Locomocion (Idle/Walk) llamada cada frame: solo cambia si el estado es distinto.
    // Llamar anim.Play cada frame reinicia la animacion y se queda en el primer frame.
    protected void PlayLoop(string state)
    {
        if (anim == null) return;

        int hash = Animator.StringToHash(state);
        if (hash == currentAnimHash) return;

        currentAnimHash = hash;
        anim.Play(hash);
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (isDead || stats == null) return;

        ShowHitVfx();

        if (audio_Source != null) audio_Source.PlayOneShot(onHit);

        float totalDamage = IsStaggered ? info.damage * staggeredDamageMultiplier : info.damage;
        currentHp -= totalDamage;

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

        if (healthUI != null) healthUI.UpdateHealthBar(currentHp, stats.maxHp);
        if (staggerUI != null) staggerUI.UpdateStaggerhBar(currentStaggerBuild, stats.staggerThreshold);
        if (barUI != null) barUI.Show();
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
    }

    // Los enemigos normales dejan que el jugador alargue el stagger con un combo.
    // Un boss lo sobrescribe en false para que recupere su turno (ver Boss_MedusaMorgana).
    protected virtual bool CanExtendStagger => true;

    protected virtual bool HandleStagger(DamageInfo info)
    {
        if (stats == null || !stats.hasStagger) return false;

        if (isInStaggerCooldown) return isStaggered;

        if (isStaggered)
        {
            // Si no se puede extender, el dano entra igual pero la duracion no se reinicia
            if (CanExtendStagger) ExtendStagger();

            return true;
        }

        currentStaggerBuild += info.staggerBuild;

        if (currentStaggerBuild >= stats.staggerThreshold)
        {
            if (audio_Source != null) audio_Source.PlayOneShot(onStagger);
            ShowStaggerVfx();
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

        AddBrokenMaterial();

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
        RemoveBrokenMaterial();
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

    void AddBrokenMaterial()
    {
        if (brokenMat == null) return;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            Material[] originalMats = renderer.sharedMaterials;
            Material[] newMaterials = new Material[originalMats.Length + 1];
            for (int i = 0; i < originalMats.Length; i++)
            {
                newMaterials[i] = originalMats[i];
            }

            Material brokenClon = new Material(brokenMat);
            if (brokenClon.HasProperty(ColorHash)) brokenClon.SetColor(ColorHash, brokenColor);
            if (brokenClon.HasProperty(MainTexSpeedHash)) brokenClon.SetVector(MainTexSpeedHash, mainTexSpeed);
            if (brokenClon.HasProperty(DistorsionIntensityHash)) brokenClon.SetFloat(DistorsionIntensityHash, distorsionIntensity);
            if (brokenClon.HasProperty(SpeedHash)) brokenClon.SetVector(SpeedHash, distorsionSpeed);

            newMaterials[newMaterials.Length - 1] = brokenClon;

            // Si ya existía un clon de este renderer, se reemplaza para no dejarlo huérfano
            if (brokenClonMats.TryGetValue(renderer, out Material previous) && previous != null)
            {
                Destroy(previous);
            }
            brokenClonMats[renderer] = brokenClon;

            renderer.materials = newMaterials;
        }
    }

    void RemoveBrokenMaterial()
    {
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            Material[] originalMats = renderer.sharedMaterials;
            if (originalMats.Length <= 1) continue;

            Material[] newMaterials = new Material[originalMats.Length - 1];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = originalMats[i];
            }

            renderer.materials = newMaterials;
        }

        foreach (KeyValuePair<Renderer, Material> par in brokenClonMats)
        {
            if (par.Value != null) Destroy(par.Value);
        }
        brokenClonMats.Clear();
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
        if (immuneToMovement) return;

        // Always allow air extension
        if (info.keepInAir)
        {
            SustainAir(info.airHangDuration);

            if (!IsGrounded)
            {
                AirJuggle(info.airLiftForce);
            }
        }

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
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
        }

        reactionCoroutine = StartCoroutine(ReactionRoutine(type, hitDirection, info));
    }

    protected virtual IEnumerator ReactionRoutine(EnemyReactionType reactionType, Vector3 hitDirection, DamageInfo info)
    {
        if (rb == null)
        {
            reactionCoroutine = null;
            yield break;
        }

        isInReaction = true;

        DisableAgent();

        switch (reactionType)
        {
            case EnemyReactionType.Push:
                ApplyPush(hitDirection, info.pushForce);
                break;

            case EnemyReactionType.Launch:
                ApplyLaunch(hitDirection, info.airLiftForce);
                break;

            case EnemyReactionType.KnockDown:
                ApplyKnockDown(hitDirection, info.knockDownForce, info.knockDownForwardScale);
                break;
        }

        switch (reactionType)
        {
            case EnemyReactionType.Push:
                yield return new WaitForSeconds(info.stunDuration);
                break;

            case EnemyReactionType.Launch:
                // Los bucles salen también si el enemigo muere, para no quedar colgados
                while (!isDead && rb.linearVelocity.y > 0.05f)
                {
                    yield return null;
                }

                while (!isDead && isAirHung)
                {
                    yield return null;
                }

                while (!isDead && !isGrounded)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(airRecoverDelay);
                break;

            case EnemyReactionType.KnockDown:
                while (!isDead && !isGrounded)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(knockDownRecoverDelay);
                break;
        }

        if (rb != null && !isDead)
        {
            rb.linearVelocity = Vector3.zero;
        }

        isInReaction = false;
        reactionCoroutine = null;
    }

    protected virtual void ApplyPush(Vector3 hitDirection, float pushForce)
    {
        if (rb == null) return;

        Vector3 dir = GetHorizontalDirection(hitDirection);
        Vector3 force = dir * (pushForce * GetPushScale());
        force.y = 0f;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    protected virtual void ApplyLaunch(Vector3 hitDirection, float liftForce)
    {
        if (rb == null) return;

        float scaledLift = liftForce * GetLaunchScale();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * scaledLift, ForceMode.VelocityChange);
    }

    protected virtual void ApplyKnockDown(Vector3 hitDirection, float knockDownForce, float forwardScale)
    {
        if (rb == null) return;

        float scaledForce = knockDownForce * GetKnockDownScale();
        Vector3 dir = GetHorizontalDirection(hitDirection);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0f, rb.linearVelocity.y), rb.linearVelocity.z);

        Vector3 downwardForce = Vector3.down * scaledForce;
        Vector3 forwardSlam = dir * (scaledForce * forwardScale);

        rb.AddForce(downwardForce, ForceMode.VelocityChange);
        rb.AddForce(forwardSlam, ForceMode.VelocityChange);
    }

    protected virtual void SustainAir(float hangTime)
    {
        if (rb == null) return;

        airHangRemaining += hangTime;

        if (airHangCoroutine == null)
        {
            airHangCoroutine = StartCoroutine(AirHangRoutine());
        }
    }

    protected virtual IEnumerator AirHangRoutine()
    {
        isAirHung = true;

        while (airHangRemaining > 0f && !isDead)
        {
            airHangRemaining -= Time.deltaTime;

            if (rb.linearVelocity.y < 0f)
                rb.AddForce(Vector3.up * Physics.gravity.magnitude * (1f - airGravityMultiplier), ForceMode.Acceleration);

            yield return null;
        }

        isAirHung = false;
        airHangRemaining = 0f;
        airHangCoroutine = null;
    }

    protected virtual void AirJuggle(float lift)
    {
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;
        vel.y = Mathf.Max(vel.y, 0f);
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * lift, ForceMode.VelocityChange);
    }

    // Helpers de escala: el peso reduce el empuje, las resistencias lo reducen por tipo de reaccion
    protected float GetWeightFactor()
    {
        return Mathf.Max(0.1f, weight / 100f);
    }

    protected virtual float GetPushScale()
    {
        return 1f / (pushResistance * GetWeightFactor());
    }

    protected virtual float GetLaunchScale()
    {
        return 1f / (launchResistance * GetWeightFactor());
    }

    protected virtual float GetKnockDownScale()
    {
        return 1f / (knockDownResistance * GetWeightFactor());
    }

    // Direccion horizontal del golpe; si es casi cero, usa el frente del enemigo
    protected Vector3 GetHorizontalDirection(Vector3 hitDirection)
    {
        Vector3 dir = hitDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        return dir.normalized;
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

        if (agent != null && !IsStaggered && !agent.enabled) EnableAgentIfNeeded();
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
        SpawnTimedVfx(vfxHit, hitVfxOffset, hitVfxDuration);
    }

    protected virtual void ShowStaggerVfx()
    {
        SpawnTimedVfx(staggerVfx, staggerVfxOffset, staggerVfxDuration);
    }

    private void SpawnTimedVfx(GameObject prefab, Vector3 offset, float duration)
    {
        if (prefab == null) return;

        GameObject vfx = Instantiate(prefab, transform.position + offset, transform.rotation);

        if (duration > 0f)
        {
            Destroy(vfx, duration);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        // Antes de la disolucion: detener todo lo que siga corriendo (stagger, stun, reacciones)
        StopEnemyRoutines();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        DisablePhysicsOnDeath();
        StartDeadEffect();
    }

    // Detiene todas las corrutinas de combate y limpia su estado.
    // Las subclases que sobrescriben Die() deben llamarlo.
    protected void StopEnemyRoutines()
    {
        StopAllCoroutines();

        stunCoroutine = null;
        staggerCoroutine = null;
        reactionCoroutine = null;
        airHangCoroutine = null;
        revengeDecayCoroutine = null;

        isStunned = false;
        isInReaction = false;
        isAirHung = false;
        airHangRemaining = 0f;
    }

    // Un enemigo muerto no debe recibir colisiones ni ser empujado
    private void DisablePhysicsOnDeath()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    void StartDeadEffect()
    {
        if (isStaggered) RemoveBrokenMaterial();

        ChangeMaterials();
        StartCoroutine(DieEffect());
    }

    void ChangeMaterials()
    {
        if (dieMaterial == null) return;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            Material[] originalMats = renderer.sharedMaterials;
            Material[] newMaterials = new Material[originalMats.Length];

            for (int i = 0; i < originalMats.Length; i++)
            {
                Material originalMat = originalMats[i];

                Material clonMat = new Material(dieMaterial);
                clonMat.SetFloat(DissolveProgressHash, 0f);

                if (clonMat.HasProperty(EdgeColorHash))
                {
                    clonMat.SetColor(EdgeColorHash, EdgeColor);
                }

                if (originalMat != null && originalMat.HasProperty(MainTexHash))
                {
                    Texture originalTex = originalMat.GetTexture(MainTexHash);
                    if (originalTex != null && clonMat.HasProperty(MainTexHash))
                    {
                        clonMat.SetTexture(MainTexHash, originalTex);
                    }
                }

                newMaterials[i] = clonMat;
                clonMaterials.Add(clonMat);
            }

            renderer.materials = newMaterials;
        }
    }

    IEnumerator DieEffect()
    {
        float timePassed = 0f;
        while (timePassed < durationDissolve)
        {
            timePassed += Time.deltaTime;
            float progress = Mathf.Clamp01(timePassed / durationDissolve);

            foreach (Material mat in clonMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(DissolveProgressHash, progress);
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (clonMaterials != null)
        {
            foreach (Material mat in clonMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            clonMaterials.Clear();
        }

        foreach (KeyValuePair<Renderer, Material> par in brokenClonMats)
        {
            if (par.Value != null) Destroy(par.Value);
        }
        brokenClonMats.Clear();
    }

    public void OnSpawn(Transform[] PatrolPoints, Transform safePoint, bool hasPatrol, Transform spawnPos)
    {
        patrolZones = PatrolPoints;
        safeZone = safePoint;
        this.hasPatrol = hasPatrol;
        this.spawnPos = spawnPos;
    }
}
