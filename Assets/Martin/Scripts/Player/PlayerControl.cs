using System.Collections;
using UnityEngine;

public class PlayerControl : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float moveMultiplier = 1f;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float groundCheckRange;
    private bool isGrounded;

    [Header("Gravity")]
    [SerializeField] private float baseGravity = -9.81f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    private float currentGravityMultiplier = 1f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCost = 20f;

    [Header("Rotation")]
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private Transform playerModel;

    [Header("Combat")]
    [SerializeField] private MeleeAttackCombo normalCombo;
    [SerializeField] private MeleeAttackCombo airCombo;
    private Coroutine attackMovementRoutine;

    [Header("Range")]
    [SerializeField] private bool isRange;
    [SerializeField] private Transform firePoint;

    [SerializeField] private LayerMask whatIsHitable;

    [Header("Skills")]
    [SerializeField] private int maxSkillSlot = 4;
    [SerializeField] private Skill[] skills = new Skill[4];

    private float[] skillsCD;

    [Header("Debug")]
    [SerializeField] private bool showDebug;
    [SerializeField] private GameObject hitboxPrefab;

    public bool hasUsedAirAttack = false;
    public bool hasUsedDash = false;

    public bool isPerformingAct = false;
    public bool blockVelocity = false;
    public bool isDead {  get; private set; }

    private Rigidbody rb;
    private Animator anim;
    private Camera mainCam;
    private PlayerInputHandler input;
    private LockOnTarget lockOnTarget;

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
    public Transform Model => playerModel;
    public PlayerInputHandler Input => input;
    public LockOnTarget LockOnTarget => lockOnTarget;
    public bool IsGrounded => isGrounded;
    public float DashCost => dashCost;
    public float DashDuration => dashDuration;
    public float DashDistance => dashDistance;
    public MeleeAttackCombo NormalCombo => normalCombo;
    public MeleeAttackCombo AirCombo => airCombo;
    public bool IsRange => isRange;
    public Transform FirePoint => firePoint;
    public float FallGravityMult => fallGravityMultiplier;

    #endregion

    private void Awake()
    {
        RegisterComponents();
        RegisterStates();
        mainCam = Camera.main;
    }

    private void Start()
    {
        moveSM.Initialize(iddle_State);
        actionSM.Initialize(iddle_AState);
    }

    private void Update()
    {
        if (isDead) return;

        CheckGround();

        moveSM.Update();
        actionSM.Update();
    }
    private void FixedUpdate()
    {
        if (isDead) return;

        ApplyGravity();

        if (!isPerformingAct) Movement();

        if (blockVelocity) rb.linearVelocity = Vector3.zero;

        moveSM.FixedUpdate();
        actionSM.FixedUpdate();
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
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInputHandler>();
        lockOnTarget = GetComponent<LockOnTarget>();
    }

    //===================================================================================
    //=====================            MOVEMENT              ============================
    //===================================================================================

    private void Movement()
    {
        Vector2 inputDir = input.moveInput.normalized;

        Vector3 moveDir = GetCameraRelativeDir(inputDir);

        Vector3 velocity = moveDir * movementSpeed * moveMultiplier;

        velocity.y = rb.linearVelocity.y;

        //if (platformRider != null && platformRider.IsOnPlatform)
        //{
        //    velocity += platformRider.CurrentPlatformVelocity;
        //}

        rb.linearVelocity = velocity;

        HandleRotation(moveDir);
    }

    public void Jump()
    {
        if (!isGrounded) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;
    }

    private void ApplyGravity()
    {
        if (rb == null) return;

        rb.AddForce(Vector3.up * baseGravity * currentGravityMultiplier, ForceMode.Acceleration);
    }
    public void SetGravityMultiplier(float value)
    {
        currentGravityMultiplier = value;
    }
    private void CheckGround()
    {
        bool previousGrounded = isGrounded;

        isGrounded = Physics.Raycast(GroundCheck.position, Vector3.down, groundCheckRange, whatIsGround);

        if (!previousGrounded && isGrounded)
        {
            hasUsedAirAttack = false;
            hasUsedDash = false;
        }
    }

    //===================================================================================
    //=====================         COMBAT RELATED           ============================
    //===================================================================================

    public void DoHit(int comboIndex, bool isGroundAttack)
    {
        AttackStep attack = isGroundAttack ? normalCombo.attackSteps[comboIndex] : airCombo.attackSteps[comboIndex];

        Vector3 center = playerModel.transform.position + playerModel.transform.forward * attack.hitBoxOffSet.z + Vector3.up * attack.hitBoxOffSet.y;

        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize * 0.5f, playerModel.transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector3 hitDir = playerModel.transform.forward;
                    damageable.TakeDamage(1f);
                }
            }
        }
        if (showDebug)
        {
            ShowHitbox(center, attack.hitBoxSize, playerModel.rotation);
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

        // Use the target's horizontal size as stopping distance.
        float radius = Mathf.Max(targetCol.bounds.extents.x, targetCol.bounds.extents.z);

        // Add a small buffer so you do not clip into them.
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

    //===================================================================================
    //=====================          SKILL RELATED           ============================
    //===================================================================================

    public Skill GetSkill(int index)
    {
        if (index < 0 || index >= skills.Length
        )
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

    private void HandleRotation(Vector3 moveDir)
    {
        Vector3 lookDir;

        if (input.isAiming || (lockOnTarget != null && lockOnTarget.isTargeting)
        )
        {
            var cam = mainCam;

            if (cam == null) return;

            lookDir = cam.transform.forward;
            lookDir.y = 0f;
            lookDir.Normalize();
        }
        else
        {
            if (moveDir.magnitude < 0.1f) return;

            lookDir = moveDir;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);

        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotSpeed * Time.deltaTime);
    }

    public void RotatePlayerModelToward(Vector3 dir, float rotateSpeed)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    //===================================================================================
    //=====================         HEALTH RELATED           ============================
    //===================================================================================

    public void TakeDamage(float damage)
    {

    }

    private void OnHeal(float ammount)
    {
        if (isDead) return;

    }

    private void OnDead()
    {
        isDead = true;
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
        if (hitboxPrefab == null)  return null;

        if (debugBox == null)
        {
            debugBox = Instantiate(hitboxPrefab);
        }

        debugBox.transform.SetPositionAndRotation(
            center,
            rot
        );

        debugBox.transform.localScale = size;

        return debugBox;
    }

}
