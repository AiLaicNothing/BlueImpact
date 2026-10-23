using UnityEngine;
using UnityEngine.InputSystem;
using CMF;

public class PlayerInputHandler : CharacterInput
{
    public float attackBufferTime = 0.2f;
    private float attackBufferCounter;

    public Vector2 moveInput { get; private set; }
    public bool hasJumped {  get; private set; }
    public bool hasDashed { get; private set; }
    public bool isAiming { get; private set; }
    public Vector2 lookInput { get; private set; }
    public Vector2 gamepadLookInput { get; private set; }   // nuevo
    public bool onLockTarget { get; private set; }
    public Vector2 scrollInput { get; private set; }
    public AttackInputType bufferedAttackType { get; private set; }
    public bool AttackBuffered => attackBufferCounter > 0;
    public bool onMelee { get; private set; }
    public bool skill1Pressed { get; private set; }
    public bool skillCanceled { get; private set; }
    public int skillPressedIndex { get; private set; } = -1;

    // ✅ AGREGAR ESTO: Acción Pause
    public InputAction pauseAction { get; private set; }
    public InputAction journalAction {  get; private set; }
    private InputActionAsset inputActions;

    private void Awake()
    {
        inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

        if (inputActions == null)
        {
            Debug.LogError("❌ No se encontró InputSystem_Actions en Resources");
            return;
        }

        var playerActionMap = inputActions.FindActionMap("Player");

        if (playerActionMap == null)
        {
            Debug.LogError("❌ Action map 'Player' no encontrado");
            return;
        }

        pauseAction = playerActionMap.FindAction("Pause");

        if (pauseAction != null)
            pauseAction.Enable();
        else
            Debug.LogError("❌ Acción 'Pause' no encontrada");

        journalAction = playerActionMap.FindAction("Journal"); 

        if (journalAction != null)
            journalAction.Enable();
        else
            Debug.LogError("❌ Acción 'Journal' no encontrada");
    }

    private void OnDestroy()
    {
        pauseAction?.Disable();
        journalAction?.Disable();
    }

    public override float GetHorizontalMovementInput()
    {
        return moveInput.x;
    }

    public override float GetVerticalMovementInput()
    {
        return moveInput.y;
    }

    public override bool IsJumpKeyPressed()
    {
        return hasJumped;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        hasJumped = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            hasDashed = true;
            Debug.Log("Has dashed");
        }
    }

    public void OnAiming(InputAction.CallbackContext context)
    {
        isAiming = context.ReadValueAsButton();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();

        // Solo guardar el valor si viene de un gamepad
        gamepadLookInput = context.control.device is Gamepad ? lookInput : Vector2.zero;
    }

    public void OnLockTarget(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            onLockTarget = true;
        }
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        scrollInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            BufferAttack(AttackInputType.Primary);
            //attackPressed = true;
        }
    }

    public void OnMelee(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            BufferAttack(AttackInputType.Melee);
        }
    }

    public void OnSkill1Pressed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skill1Pressed = true;
        }
    }

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Skill 1");
            skillPressedIndex = 0;
            skillCanceled = false;
        }
        if (context.canceled)
        {
            Debug.Log("Skill Canceled");
            skillCanceled = true;
            skillPressedIndex = -1;
        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Skill 2");
            skillPressedIndex = 1;
            skillCanceled = false;
        }
        if (context.canceled)
        {
            Debug.Log("Skill Canceled");
            skillCanceled = true;
            skillPressedIndex = -1;
        }
    }

    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Skill 3");
            skillPressedIndex = 2;
            skillCanceled = false;
        }
        if (context.canceled)
        {
            Debug.Log("Skill Canceled");
            skillCanceled = true;
            skillPressedIndex = -1;
        }
    }

    public void OnSkill4(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Skill 4");
            skillPressedIndex = 3;
            skillCanceled = false;
        }
        if (context.canceled)
        {
            Debug.Log("Skill Canceled");
            skillCanceled = true;
            skillPressedIndex = -1;
        }
    }

    private void Update()
    {
        if (attackBufferCounter > 0)
        {
            attackBufferCounter -= Time.deltaTime;
        }
    }

    public void LateUpdate()
    {
        hasJumped = false;
        //attackPressed = false;
        hasDashed = false;
        skill1Pressed = false;
        onLockTarget = false;

        scrollInput = Vector2.zero;
    }

    private void BufferAttack(AttackInputType type)
    {
        attackBufferCounter = attackBufferTime;
        bufferedAttackType = type;
    }
    public void UseAttackBufer()
    {
        attackBufferCounter = 0f;
        bufferedAttackType = AttackInputType.None;
    }


    public void ClearInputs()
    {
        moveInput = Vector2.zero;

        hasJumped = false;
        hasDashed = false;

        isAiming = false;
        lookInput = Vector2.zero;
        gamepadLookInput = Vector2.zero;
        onLockTarget = false;

        scrollInput = Vector2.zero;

        skill1Pressed = false;
        skillPressedIndex = -1;

        attackBufferCounter = 0f;
        bufferedAttackType = AttackInputType.None;

        onMelee = false;
    }

}