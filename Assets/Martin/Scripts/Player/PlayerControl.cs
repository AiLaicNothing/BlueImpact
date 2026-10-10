using CMF;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using static UnityEngine.Analytics.IAnalytic;

public class PlayerControl : Controller, IDamageable
{
    // Evento global: cualquier sistema puede suscribirse para reaccionar a la muerte
    // (ej. DeathScreenUI, ElevatorEvent) sin acoplarse directamente.
    public static event System.Action OnPlayerDied;

    public float moveSpeed = 7;
    public float jumpSpeed = 10;
    //Jump duration variables;
    public float jumpDuration = 0.2f;
    float currentJumpStartTime = 0f;
    public float gravity = 10;
    public float slideGravity = 5f;

    //Jump key variables;
    bool jumpInputIsLocked = false;
    bool jumpKeyWasPressed = false;
    bool jumpKeyWasLetGo = false;
    bool jumpKeyIsPressed = false;

    //How fast the controller can change direction while in the air;
    //Higher values result in more air control;
    public float airControlRate = 2f;

    //'AirFriction' determines how fast the controller loses its momentum while in the air;
    //'GroundFriction' is used instead, if the controller is grounded;
    public float airFriction = 0.5f;
    public float groundFriction = 100f;

    public Transform camTransform;

    public float slopeLimit = 80f;
    public bool useLocalMomentum;

    //Current momentum;
    protected Vector3 momentum = Vector3.zero;

    //-->Saved from last frame
    Vector3 savedVel = Vector3.zero;
    Vector3 savedMoveVel = Vector3.zero;

    private Mover mover;
    private PlayerInputHandler inputs;
    private Transform tr;

    ControllerState currentState = ControllerState.Falling;
    public enum ControllerState
    {
        Grounded,
        Sliding,
        Falling,
        Rising,
        Jumping,
    }

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCost = 20f;
    public bool canDash;

    [Header("Rotation")]
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private Transform playerModel;

    [Header("Combat")]
    [SerializeField] private MeleeAttackCombo normalCombo;
    [SerializeField] private MeleeAttackCombo airCombo;
    private StatsManager statsManager;
    private Coroutine attackMovementRoutine;

    [Header("Range")]
    [SerializeField] private bool isRange;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ShootData shootData;
    [SerializeField] private LayerMask whatIsHitable;

    [Header("Skills")]
    [SerializeField] private int maxSkillSlot = 4;
    [SerializeField] private Skill[] skills = new Skill[4];

    private List<Skill> unlockedSkills = new List<Skill>();

    [Header("Audios")]
    [SerializeField] public AudioSource audioLoopSource;
    [SerializeField] public AudioClip walk;
    [SerializeField] public AudioClip dash;
    [SerializeField] public AudioClip jump;
    [SerializeField] public AudioClip land;
    [SerializeField] public AudioClip onHit;
    [SerializeField] public AudioClip onDead;
    [SerializeField] public AudioClip meleeAttack;
    [SerializeField] public AudioClip shootSound;
    [SerializeField] private float sfxVolume = 0.8f;

    private void InitializeSkills()
    {
        // Si tienes skills en el array, considéralas desbloqueadas
        foreach (var skill in skills)
        {
            if (skill != null && !unlockedSkills.Contains(skill))
                unlockedSkills.Add(skill);
        }
    }

    public void UnlockSkill(Skill skill)
    {
        if (skill != null && !unlockedSkills.Contains(skill))
        {
            unlockedSkills.Add(skill);
            Debug.Log($"✅ Desbloqueada: {skill.skillName}");
        }
    }

    public List<Skill> GetUnlockedSkills()
    {
        return unlockedSkills;
    }

    public void EquipSkill(int slot, Skill skill)
    {
        if (slot >= 0 && slot < skills.Length)
        {
            skills[slot] = skill;
            Debug.Log($"⚡ Equipada en slot {slot}: {skill?.skillName}");
        }
    }

    public void UnequipSkill(int slot)
    {
        if (slot >= 0 && slot < skills.Length)
        {
            skills[slot] = null;
        }
    }

    public Skill GetEquippedSkill(int slot)
    {
        if (slot >= 0 && slot < skills.Length)
            return skills[slot];
        return null;
    }

    private float[] skillsCD;

    [Header("Debug")]
    [SerializeField] private bool showDebug;
    [SerializeField] private GameObject hitboxPrefab;

    public bool hasUsedAirAttack = false;
    public bool IsInputLocked { get; private set; }
    public bool isPerformingAct = false;
    public bool blockVelocity = false;
    public bool isDead { get; set; }  // ✅ CAMBIO: private set → public set
    public CharacterInfo CurrentCharacterInfo { get; set; }
    private PlayerStatsManager playerStatsManager;
    private bool isAudioMuted = false;
    private RigidbodyConstraints previousConstraints = RigidbodyConstraints.None;

    private Rigidbody rb;
    private Animator anim;
    private Camera mainCam;
    private PlayerInputHandler input;
    public LockOnTarget lockOnTarget;
    private P_Skill_UI asd;
    private CharacterStats stats;

    #region StateMachine References

    private PlayerStateMachine moveSM;

    public P_Iddle_State iddle_State;
    public P_Move_State move_State;
    public P_Jump_State jump_State;
    public P_Fall_State fall_State;

    private PlayerStateMachine actionSM;

    public P_Iddle_AState iddle_AState;
    public P_Attack_AState attack_AState;
    public P_AirAttack_AState airAttackAState;
    public P_Shoot_AState shoot_AState;
    public P_Skill_AState skill_AState;
    public P_Dash_AState dash_AState;

    #endregion

    #region Public References

    public Rigidbody Rb => rb;
    public Animator Anim => anim;

    public CharacterStats _Stats => stats;
    public Camera MainCam => mainCam;
    public Transform Cam => camTransform;
    public Transform Model => playerModel;
    public PlayerInputHandler Input => input;
    public LockOnTarget LockOnTarget => lockOnTarget;
    public StatsManager Stats => statsManager;
    public float DashCost => dashCost;
    public float DashDuration => dashDuration;
    public float DashDistance => dashDistance;
    public MeleeAttackCombo NormalCombo => normalCombo;
    public MeleeAttackCombo AirCombo => airCombo;
    public bool IsRange => isRange;
    public ShootData ShootData => shootData;
    public Transform FirePoint => firePoint;
    public int MaxSkillSlot => maxSkillSlot;
    public PlayerStatsManager PlayerStatsManager => playerStatsManager;

    #endregion

    private void Awake()
    {
        RegisterComponents();
        RegisterStates();
        mainCam = Camera.main;

        //  Obtener PlayerStatsManager
        playerStatsManager = GetComponent<PlayerStatsManager>();
        if (playerStatsManager != null)
        {
            playerStatsManager.EnsureInitialized();
        }

    }

    private void Start()
    {
        asd = FindAnyObjectByType<P_Skill_UI>();
        asd.RefreshIcons();
        InitializeSkills();

        // ✅ ASIGNAR UI INPUT MODULE
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            var uiInputModule = FindAnyObjectByType<InputSystemUIInputModule>();
            if (uiInputModule != null)
            {
                playerInput.uiInputModule = uiInputModule;
            }
            else
            {
                Debug.LogError("❌ InputSystemUIInputModule no encontrado");
            }
        }

        //  Inicializar cooldowns
        skillsCD = new float[maxSkillSlot];
        for (int i = 0; i < skillsCD.Length; i++)
        {
            skillsCD[i] = 0f;
        }

        moveSM.Initialize(iddle_State);
        actionSM.Initialize(iddle_AState);

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged += HandleGameModeChanged;
            Debug.Log("PlayerControl suscrito");
        }
        else
        {
            Debug.LogError("GameModeManager no encontrado");
        }

    }

    private void OnDisable()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged -= HandleGameModeChanged;
        }
    }

    private void HandleGameModeChanged(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Gameplay:
                UnlockPlayerControl();
                break;

            case GameMode.UI:
            case GameMode.Puzzle:
            case GameMode.Dialogue:
            case GameMode.Cutscene:
                LockPlayerControl();
                break;
        }
    }

    private void Update()
    {
        if (isDead) return;

        //CheckGround();

        if (IsInputLocked) return;

        moveSM.Update();
        HandleJumpKeyInput();
        actionSM.Update();
    }

    void HandleJumpKeyInput()
    {
        bool _newJumpKeyPressedState = IsJumpKeyPressed();

        if (jumpKeyIsPressed == false && _newJumpKeyPressedState == true)
            jumpKeyWasPressed = true;

        if (jumpKeyIsPressed == true && _newJumpKeyPressedState == false)
        {
            jumpKeyWasLetGo = true;
            jumpInputIsLocked = false;
        }

        jumpKeyIsPressed = _newJumpKeyPressedState;
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        //  Actualizar cooldowns
        for (int i = 0; i < skillsCD.Length; i++)
        {

            if (skillsCD[i] > 0)
            {
                skillsCD[i] -= Time.fixedDeltaTime;
            }
        }

        if (IsInputLocked)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            return;
        }

        if (!isPerformingAct) HandleMovement();

        if (blockVelocity) rb.linearVelocity = Vector3.zero;
    }


    ControllerState DecideControllerState()
    {
        bool _isRising = IsRisingOrFalling() && (VectorMath.GetDotProduct(GetMomentum(), tr.up) > 0f);

        bool _isSliding = mover.IsGrounded() && IsGroundTooSteep();

        if (currentState == ControllerState.Grounded)
        {
            if (_isRising)
            {
                OnGroundContactLost();
                return ControllerState.Rising;
            }
            if (!mover.IsGrounded())
            {
                OnGroundContactLost();
                return ControllerState.Falling;
            }
            if (_isSliding)
            {
                OnGroundContactLost();
                return ControllerState.Sliding;
            }
            return ControllerState.Grounded;
        }

        if (currentState == ControllerState.Falling)
        {
            if (_isRising)
            {
                return ControllerState.Rising;
            }
            if (mover.IsGrounded() && !_isSliding)
            {
                OnGroundContactRegained();
                hasUsedAirAttack = false;
                PlayAudio(land, sfxVolume);
                return ControllerState.Grounded;
            }
            if (_isSliding)
            {
                return ControllerState.Sliding;
            }
            return ControllerState.Falling;
        }

        if (currentState == ControllerState.Sliding)
        {
            if (_isRising)
            {
                OnGroundContactLost();
                return ControllerState.Rising;
            }
            if (!mover.IsGrounded())
            {
                OnGroundContactLost();
                return ControllerState.Falling;
            }
            if (mover.IsGrounded() && !_isSliding)
            {
                OnGroundContactRegained();
                hasUsedAirAttack = false;
                PlayAudio(land, sfxVolume);
                return ControllerState.Grounded;
            }
            return ControllerState.Sliding;
        }

        if (currentState == ControllerState.Rising)
        {
            if (!_isRising)
            {
                if (mover.IsGrounded() && !_isSliding)
                {
                    OnGroundContactRegained();
                    hasUsedAirAttack = false;
                    PlayAudio(land, sfxVolume);
                    return ControllerState.Grounded;
                }
                if (_isSliding)
                {
                    return ControllerState.Sliding;
                }
                if (!mover.IsGrounded())
                {
                    return ControllerState.Falling;
                }
            }
        }

        if (currentState == ControllerState.Jumping)
        {
            PlayAudio(jump, sfxVolume);

            //Check for jump timeout;
            if ((Time.time - currentJumpStartTime) > jumpDuration) return ControllerState.Rising;

            //Check if jump key was let go;
            if (jumpKeyWasLetGo) return ControllerState.Rising;

            return ControllerState.Jumping;
        }

        return ControllerState.Falling;
    }

    public void LockPlayerControl()
    {
        // 🔒 IMPORTANTE: Si ya está bloqueado, NO sobrescribir previousConstraints
        if (IsInputLocked)
        {
            if (showDebug)
            {
                Debug.Log("[PlayerControl] ⚠️ Ya estaba bloqueado, ignorando segunda llamada");
            }
            return;
        }

        IsInputLocked = true;

        // 🔒 Guardar constraints previos SOLO si no estaba bloqueado
        previousConstraints = rb.constraints;

        // Congelar completamente el Rigidbody
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        input.ClearInputs();

        if (showDebug)
        {
            Debug.Log("[PlayerControl] 🔒 CONTROL BLOQUEADO - Rigidbody congelado");
        }
    }

    public void UnlockPlayerControl()
    {
        IsInputLocked = false;

        // 🔓 Restaurar constraints previos
        rb.constraints = previousConstraints;

        input.ClearInputs();

        if (showDebug)
        {
            Debug.Log("[PlayerControl] 🔓 CONTROL DESBLOQUEADO");
        }
    }

    /// <summary>
    /// Silencia o reactiva todos los sonidos del jugador (movimiento, ataque, habilidades, etc.)
    /// Útil para cinemáticas y eventos de cámara
    /// </summary>
    public void MutePlayerAudio(bool mute)
    {
        isAudioMuted = mute;

        if (mute)
        {
            // Detener audio de loop (caminar)
            if (audioLoopSource != null && audioLoopSource.isPlaying)
            {
                audioLoopSource.Stop();
            }


        }
    }
    public void ChangeState(PlayerState nextState)
    {
        moveSM.ChangeState(nextState);
    }

    public void ChangeActionState(PlayerState nextState)
    {
        actionSM.ChangeState(nextState);
    }

    private void RegisterStates()
    {
        moveSM = new PlayerStateMachine();
        actionSM = new PlayerStateMachine();

        iddle_State = new P_Iddle_State(this);
        move_State = new P_Move_State(this);
        jump_State = new P_Jump_State(this);
        fall_State = new P_Fall_State(this);

        iddle_AState = new P_Iddle_AState(this);
        attack_AState = new P_Attack_AState(this);
        airAttackAState = new P_AirAttack_AState(this);
        shoot_AState = new P_Shoot_AState(this);
        skill_AState = new P_Skill_AState(this);
        dash_AState = new P_Dash_AState(this);
    }

    private void RegisterComponents()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<CharacterStats>();
        input = GetComponent<PlayerInputHandler>();
        lockOnTarget = GetComponent<LockOnTarget>();
        statsManager = GetComponent<StatsManager>();

        mover = GetComponent<Mover>();
        tr = transform;
        inputs = GetComponent<PlayerInputHandler>();

    }

    //===================================================================================
    //=====================            MOVEMENT              ============================
    //===================================================================================

    private void HandleMovement()
    {
        //--> Check if is grounded
        mover.CheckForGround();

        currentState = DecideControllerState();

        //Add gravity if its not grounded
        HandleMomentum();

        HandleJump();

        Vector3 _velocity = Vector3.zero;

        if (currentState == ControllerState.Grounded) _velocity = CalculateMoveVel();

        Vector3 _worldMomentum = momentum;

        if (useLocalMomentum)
        {
            _worldMomentum = tr.localToWorldMatrix * momentum;
        }

        _velocity += _worldMomentum;

        //If player is grounded or sliding on a slope, extend mover's sensor range;
        //This enables the player to walk up/down stairs and slopes without losing ground contact;
        mover.SetExtendSensorRange(IsGrounded());

        //Set mover velocity
        mover.SetVelocity(_velocity);

        savedVel = _velocity;
        savedMoveVel = CalculateMoveVel();

        jumpKeyWasPressed = false;
    }

    private Vector3 CalculateMoveDir()
    {
        Vector3 _velocity = Vector3.zero;

        if (camTransform != null)
        {
            _velocity += Vector3.ProjectOnPlane(camTransform.right, tr.up).normalized * inputs.GetHorizontalMovementInput();
            _velocity += Vector3.ProjectOnPlane(camTransform.forward, tr.up).normalized * inputs.GetVerticalMovementInput();
        }
        else
        {
            Debug.LogError("Player: missing camera transform in PlayerController");
        }

        if (_velocity.magnitude > 1f) _velocity.Normalize();

        return _velocity;
    }

    private Vector3 CalculateMoveVel()
    {
        Vector3 _velocity = CalculateMoveDir();

        _velocity *= moveSpeed;

        return _velocity;
    }

    private void HandleJump()
    {
        if (currentState == ControllerState.Grounded)
        {
            if ((jumpKeyIsPressed == true || jumpKeyWasPressed) && !jumpInputIsLocked)
            {
                //Call events;
                OnGroundContactLost();
                OnJumpStart();

                currentState = ControllerState.Jumping;
            }
        }
    }

    private void HandleMomentum()
    {
        if (useLocalMomentum) momentum = tr.localToWorldMatrix * momentum;

        Vector3 _vecticalMomentum = Vector3.zero;
        Vector3 _horizontalMomentum = Vector3.zero;

        //Split momentum into vertical and horizontal components;
        if (momentum != Vector3.zero)
        {
            _vecticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
            _horizontalMomentum = momentum - _vecticalMomentum;
        }

        //Add gravity to vertical momentum;
        _vecticalMomentum -= tr.up * gravity * Time.deltaTime;

        //Remove any downward force if the controller is grounded;
        if (currentState == ControllerState.Grounded && VectorMath.GetDotProduct(_vecticalMomentum, tr.up) < 0f)
        {
            _vecticalMomentum = Vector3.zero;
        }

        //Manipulate momentum to steer controller in the air (if controller is not grounded or sliding);
        if (!IsGrounded())
        {
            Vector3 _movementVel = CalculateMoveVel();

            if (_horizontalMomentum.magnitude > moveSpeed)
            {
                if (VectorMath.GetDotProduct(_movementVel, _horizontalMomentum.normalized) > 0f)
                {
                    _movementVel = VectorMath.RemoveDotVector(_movementVel, _horizontalMomentum.normalized);
                }

                float _airControlMult = 0.25f;
                _horizontalMomentum += _movementVel * Time.deltaTime * airControlRate * _airControlMult;
            }
            else
            {
                _horizontalMomentum += _movementVel * Time.deltaTime * airControlRate;
                _horizontalMomentum = Vector3.ClampMagnitude(_horizontalMomentum, moveSpeed);
            }
        }

        if (currentState == ControllerState.Sliding)
        {
            //Calculate vector pointing away from slope;
            Vector3 _pointDownVector = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;

            //Calculate movement velocity;
            Vector3 _slopeMovementVelocity = CalculateMoveVel();
            //Remove all velocity that is pointing up the slope;
            _slopeMovementVelocity = VectorMath.RemoveDotVector(_slopeMovementVelocity, _pointDownVector);

            //Add movement velocity to momentum;
            _horizontalMomentum += _slopeMovementVelocity * Time.fixedDeltaTime;
        }

        //Apply friction to horizontal momentum based on whether the controller is grounded;
        if (currentState == ControllerState.Grounded)
        {
            _horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, groundFriction, Time.deltaTime, Vector3.zero);
        }
        else
        {
            _horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, airFriction, Time.deltaTime, Vector3.zero);
        }

        momentum = _horizontalMomentum + _vecticalMomentum;

        if (currentState == ControllerState.Sliding)
        {
            //Project the current momentum onto the current ground normal if the controller is sliding down a slope;
            momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());

            //Remove any upwards momentum when sliding;
            if (VectorMath.GetDotProduct(momentum, tr.up) > 0f)
                momentum = VectorMath.RemoveDotVector(momentum, tr.up);

            //Apply additional slide gravity;
            Vector3 _slideDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
            momentum += _slideDirection * slideGravity * Time.deltaTime;
        }

        if (currentState == ControllerState.Jumping)
        {
            momentum = VectorMath.RemoveDotVector(momentum, tr.up);
            momentum += tr.up * jumpSpeed;
        }

        if (useLocalMomentum)
        {
            momentum = tr.worldToLocalMatrix * momentum;
        }

    }


    //===================================================================================
    //=====================        MELEE COMBAT RELATED      ============================
    //===================================================================================

    public void DoHit(int comboIndex, bool isGroundAttack)
    {
        AttackStep attack = isGroundAttack ? normalCombo.attackSteps[comboIndex] : airCombo.attackSteps[comboIndex];

        // 🔊 Sonido de ataque melee
        PlayAudio(meleeAttack, sfxVolume);

        Vector3 center = playerModel.transform.position + playerModel.transform.forward * attack.hitBoxOffSet.z + Vector3.up * attack.hitBoxOffSet.y;

        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize * 0.5f, playerModel.transform.rotation);

        if (showDebug)
        {
            ShowHitbox(center, attack.hitBoxSize, playerModel.rotation);
        }

        if (attack.attackVfx != null)
        {
            StartCoroutine(SpawnVfxAttack(attack));
        }

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();

                if (damageable != null)
                {

                    Vector3 hitDir = playerModel.transform.forward;

                    DamageInfo info = new DamageInfo
                    {
                        damage = (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Physical_Damage) * attack.hitData.physicalScale) + (CharacterStatsManager.Instance.GetCurrentStat(StatsType.Magical_Damage) * attack.hitData.magicalScale),
                        hitDirection = hitDir,
                        throwType = attack.hitData.throwType,
                        stunDuration = attack.hitData.stunDuration,
                        keepInAir = attack.hitData.keepInAir,
                        airHangDuration = attack.hitData.airHangDuration,
                        airLiftForce = attack.hitData.airLiftForce,
                        pushForce = attack.hitData.pushForce,
                        knockDownForce = attack.hitData.knockDownForce,
                        knockDownForwardScale = attack.hitData.knockDownForwardScale,
                        staggerBuild = attack.hitData.staggerCharge
                    };

                    damageable.TakeDamage(in info);

                    Debug.Log("Player Hit");
                }
            }
        }
    }

    public void StartAttackMove(AttackStep attack, Vector3? lockTargetPos = null)
    {
        StopAttackMove();

        if (attack == null) return;
        if (attack.moveDis <= 0f) return;
        if (attack.moveDuration <= 0f) return;

        attackMovementRoutine = StartCoroutine(AttackMoveRoutine(attack, lockTargetPos));
    }

    public void StopAttackMove()
    {
        if (attackMovementRoutine != null)
        {
            StopCoroutine(attackMovementRoutine);
            attackMovementRoutine = null;
        }
    }

    private Vector3 GetSafeAttackMove(Vector3 delta)
    {
        float distance = delta.magnitude;

        if (distance <= 0.0001f) return Vector3.zero;

        Vector3 dir = delta / distance;

        Collider coll = GetComponent<Collider>();

        if (coll == null) return delta;

        Vector3 center = rb.position + coll.bounds.center - transform.position;

        float radius = Mathf.Min(coll.bounds.extents.x, coll.bounds.extents.z) * 0.9f;
        float castDistance = distance;

        if (Physics.SphereCast(center, radius, dir, out RaycastHit hit, castDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - 0.02f, 0f);
            return dir * safeDistance;
        }

        return delta;
    }

    private float GetLockOnStopDistance(Transform target)
    {
        if (target == null) return 0f;

        Collider targetCol = target.GetComponentInChildren<Collider>();
        if (targetCol == null) return 0f;

        float radius = Mathf.Max(targetCol.bounds.extents.x, targetCol.bounds.extents.z);

        return radius + 0.25f;
    }

    private IEnumerator AttackMoveRoutine(AttackStep attack, Vector3? lockTargetPos)
    {
        if (attack.moveStartTime > 0f) yield return new WaitForSeconds(attack.moveStartTime);

        if (rb == null || playerModel == null) yield break;

        Vector3 dir = playerModel.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

        dir.Normalize();

        Vector3 startPos = rb.position;

        float finalMoveDistance = attack.moveDis;

        if (lockTargetPos.HasValue && lockOnTarget != null && lockOnTarget.isTargeting)
        {
            Vector3 targetPos = lockTargetPos.Value;
            Vector3 toTarget = targetPos - startPos;
            toTarget.y = 0f;

            float distToTarget = toTarget.magnitude;
            float stopDistance = GetLockOnStopDistance(lockOnTarget.CurrentTarget);

            float allowed = distToTarget - stopDistance;

            if (allowed < 0f) allowed = 0f;

            finalMoveDistance = Mathf.Min(finalMoveDistance, allowed);
        }

        if (finalMoveDistance <= 0f) yield break;

        Vector3 desiredEnd = startPos + dir * finalMoveDistance;

        float elapsed = 0f;

        while (elapsed < attack.moveDuration)
        {
            if (!isPerformingAct) yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / attack.moveDuration);
            float easeT = attack.moveCurve != null ? attack.moveCurve.Evaluate(t) : t;

            Vector3 desiredPos = Vector3.Lerp(startPos, desiredEnd, easeT);
            Vector3 delta = desiredPos - rb.position;

            if (delta.sqrMagnitude > 0.000001f)
            {
                Vector3 safeMove = GetSafeAttackMove(delta);
                rb.MovePosition(rb.position + safeMove);
            }

            yield return new WaitForFixedUpdate();
        }

        attackMovementRoutine = null;
    }

    private IEnumerator SpawnVfxAttack(AttackStep attack)
    {
        if (attack.vfxSpawnTime > 0f) yield return new WaitForSeconds(attack.vfxSpawnTime);

        Transform root = playerModel != null ? playerModel : transform;

        Vector3 spawnPos = root.TransformPoint(attack.vfxOffset);
        Quaternion spawnRot = root.rotation * Quaternion.Euler(attack.vfxRotOffset);

        GameObject vfx = Instantiate(attack.attackVfx, spawnPos, spawnRot);
        vfx.transform.localScale = attack.vfxScale;

        if (attack.vfxDuration > 0f) Destroy(vfx, attack.vfxDuration);
    }

    //===================================================================================
    //=====================        RANGE COMBAT RELATED      ============================
    //===================================================================================

    public void Shoot(Vector3 targetPoint)
    {
        if (shootData == null) return;
        if (shootData.proyectilePrefab == null) return;
        if (firePoint == null) return;

        Vector3 spawnPosition = firePoint.position;
        Vector3 direction = (targetPoint - spawnPosition).normalized;

        if (direction.sqrMagnitude < 0.0001f) direction = playerModel != null ? playerModel.forward : transform.forward;

        GameObject projectile = Instantiate(shootData.proyectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // 🔊 Sonido de disparo
        PlayAudio(shootSound, sfxVolume);

        P_Projectile proyectile = projectile.GetComponent<P_Projectile>();

        if (proyectile != null)
        {
            proyectile.Initialize(shootData.hitData, this, direction, shootData.proyectileSpeed, Vector3.zero);
        }
    }

    //===================================================================================
    //=====================          SKILL RELATED           ============================
    //===================================================================================

    public void UseSkill(int skillIndex, Vector3 targetPoint)
    {
        if (skillIndex < 0 || skillIndex >= skills.Length) return;

        Skill skill = skills[skillIndex];
        if (skill == null) return;

        if (!IsSkillReady(skillIndex)) return;

        // ✅ USAR PlayerStatsManager
        //if (!playerStatsManager.CanConsume(skill.resourceType, skill.cost))
        //{
        //    Debug.Log($"No hay suficiente {skill.resourceType}");
        //    return;
        //}

        //playerStatsManager.Consume(skill.resourceType, skill.cost);

        TriggerCooldown(skillIndex);

        Vector3 lockTargetPos = Vector3.zero;
        if (lockOnTarget != null && lockOnTarget.isTargeting && lockOnTarget.CurrentTarget != null)
        {
            lockTargetPos = lockOnTarget.CurrentTarget.position;
        }

        // Línea 659, antes de ExecuteSkill:
        PlayAudio(skill.castSound, sfxVolume);
        skill.ExecuteSkill(this, targetPoint, lockTargetPos);
    }

    public Skill GetSkill(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            return null;
        }

        return skills[index];
    }

    public bool IsSkillReady(int index)
    {
        if (index < 0 || index >= skillsCD.Length)
        {
            return false;
        }

        return skillsCD[index] <= 0f;
    }

    public void TriggerCooldown(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            return;
        }

        if (skills[index] == null)
            return;

        skillsCD[index] = skills[index].cooldown;
    }

    //===================================================================================
    //=====================         SKILL UI RELATED         ============================
    //===================================================================================

    public float GetSkillCooldownRemaining(int index)
    {
        if (index < 0 || index >= skillsCD.Length) return 0f;

        return Mathf.Max(0f, skillsCD[index]);
    }

    public float GetSkillCooldownDuration(int index)
    {
        Skill skill = GetSkill(index);

        if (skill == null) return 0f;

        return skill.cooldown;
    }

    //===================================================================================
    //=====================         CAMARA RELATED           ============================
    //===================================================================================

    public Vector3 GetCameraRelativeDir(Vector2 inputDir)
    {
        var cam = mainCam;
        if (cam == null) return Vector3.zero;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.y + camRight * inputDir.x;
        moveDir.y = 0f;

        return moveDir.normalized;
    }

    public Vector3 GetViewPoint()
    {
        var cam = mainCam;

        if (cam == null) return transform.position;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, whatIsHitable))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * 100f;
    }

    //===================================================================================
    //=====================       PLAYER MODEL VISUAL        ============================
    //===================================================================================
    public void RotatePlayerModelToward(Vector3 dir, float rotateSpeed)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    //===================================================================================
    //=====================         HEALTH RELATED           ============================
    //===================================================================================

    public void TakeDamage(in DamageInfo info)
    {
        if (isDead) return;

        stats.ConsumeStat(StatsType.Health, (int)info.damage);

        playerStatsManager.Consume(StatType.Vida, (int)info.damage);
        Anim.SetTrigger("Attacked");
        // 🔊 Sonido de recibir daño
        PlayAudio(onHit, sfxVolume);

        if (playerStatsManager.IsDead() || stats.IsDead())
        {
            OnDead();
        }

        //anim.SetTrigger("Hit");
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        playerStatsManager.Restore(StatType.Vida, amount);
        stats.RestoreCurrentStat(StatsType.Health, amount);
    }

    private void OnDead()
    {
        isDead = true;  // ✅ SETEAR PRIMERO

        Debug.Log("💀 Player muere");

        // 🔊 Sonido de muerte
        PlayAudio(onDead, sfxVolume);

        // 📢 Avisar a cualquier sistema suscrito (ej. DeathScreenUI, eventos de elevador)
        OnPlayerDied?.Invoke();

        // ✅ DeathScreenUI ahora controla el respawn después de la cuenta atrás
        // No llamar a Respawn() aquí - déjalo para el DeathScreenUI
    }

    public void PlayAudio(AudioClip audio, float volume = 1)
    {
        // No reproducir si el audio del jugador está silenciado
        if (isAudioMuted) return;

        if (audio == null || Audio_Manager.Instance == null) return;
        Audio_Manager.Instance.PlaySFX(audio, volume);
    }

    public void PlayConstantAudio(AudioClip audio, float volume, bool ended)
    {
        if (!ended)
        {
            // No reproducir si el audio está silenciado
            if (isAudioMuted) return;

            if (rb.linearVelocity.magnitude < 0.01f) return;

            if (audioLoopSource.isPlaying) return;

            audioLoopSource.clip = audio;
            audioLoopSource.loop = true;
            audioLoopSource.volume = volume;
            audioLoopSource.Play();
        }
        else
        {
            audioLoopSource.Stop();
        }
    }

    //===================================================================================
    //=====================             DEBUG                ============================
    //===================================================================================

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        if (hitboxPrefab == null) return;

        GameObject box = Instantiate(hitboxPrefab, center, rot);

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }

    public GameObject ShowHitboxPersistent(Vector3 center, Vector3 size, Quaternion rot, GameObject debugBox)
    {
        if (hitboxPrefab == null) return null;

        if (debugBox == null)
        {
            debugBox = Instantiate(hitboxPrefab);
        }

        debugBox.transform.SetPositionAndRotation(center, rot);

        debugBox.transform.localScale = size;

        return debugBox;
    }


    #region Events

    private void OnJumpStart()
    {
        if (useLocalMomentum) momentum = tr.localToWorldMatrix * momentum;

        momentum += tr.up * jumpSpeed;

        if (OnJump != null) OnJump(momentum);

        if (useLocalMomentum) momentum = tr.worldToLocalMatrix * momentum;
    }

    private void OnGroundContactLost()
    {
        //If local momentum is used, transform momentum into world coordinates first;
        if (useLocalMomentum) momentum = tr.localToWorldMatrix * momentum;

        Vector3 _velocity = GetMovementVelocity();

        //Check if the controller has both momentum and a current movement velocity;
        if (_velocity.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
        {
            //Project momentum onto movement direction;
            Vector3 _projectedMomentum = Vector3.Project(momentum, _velocity.normalized);

            //Calculate dot product to determine whether momentum and movement are aligned;
            float _dot = VectorMath.GetDotProduct(_projectedMomentum.normalized, _velocity.normalized);

            //If current momentum is already pointing in the same direction as movement velocity,
            //Don't add further momentum (or limit movement velocity) to prevent unwanted speed accumulation;
            if (_projectedMomentum.sqrMagnitude >= _velocity.sqrMagnitude && _dot > 0f)
            {
                _velocity = Vector3.zero;
            }
            else if (_dot > 0f)
            {
                _velocity -= _projectedMomentum;
            }
        }

        //Add movement velocity to momentum;
        momentum += _velocity;

        if (useLocalMomentum) momentum = tr.worldToLocalMatrix * momentum;
    }

    private void OnGroundContactRegained()
    {
        //Call 'OnLand' event;
        if (OnLand != null)
        {
            Vector3 _collisionVelocity = momentum;

            //If local momentum is used, transform momentum into world coordinates first;
            if (useLocalMomentum) _collisionVelocity = tr.localToWorldMatrix * _collisionVelocity;

            OnLand(_collisionVelocity);
        }
    }

    #endregion

    #region Helper functions

    private bool IsRisingOrFalling()
    {
        Vector3 _verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), tr.up);

        float _limit = 0.001f;

        return (_verticalMomentum.magnitude > _limit);
    }

    private bool IsGroundTooSteep()
    {
        if (!mover.IsGrounded()) return true;

        return (Vector3.Angle(mover.GetGroundNormal(), tr.up) > slopeLimit);
    }
    #endregion

    #region Getters
    public override Vector3 GetVelocity()
    {
        return savedVel;
    }

    public override Vector3 GetMovementVelocity()
    {
        return savedMoveVel;
    }

    public override bool IsGrounded()
    {
        return (currentState == ControllerState.Grounded || currentState == ControllerState.Sliding);
    }

    public Vector3 GetMomentum()
    {
        Vector3 _worldMomentum = momentum;

        if (useLocalMomentum) _worldMomentum = tr.localToWorldMatrix * momentum;

        return _worldMomentum;
    }

    public bool IsJumpKeyPressed()
    {
        if (inputs == null) return false;

        return inputs.IsJumpKeyPressed();
    }

    #endregion
}