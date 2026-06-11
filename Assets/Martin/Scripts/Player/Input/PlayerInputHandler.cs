using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public float attackBufferTime = 0.2f;
    private float attackBufferCounter;

    public Vector2 moveInput { get; private set; }
    private bool hasJumped;
    public bool hasDashed { get; private set; }
    public bool isAiming { get; private set; }
    public Vector2 lookInput { get; private set; }
    public bool onLockTarget { get; private set; }
    public float scrollInput { get; private set; }
    public AttackInputType bufferedAttackType { get; private set; }
    public bool AttackBuffered => attackBufferCounter > 0;
    public bool onMelee { get; private set; }
    public bool skill1Pressed { get; private set; }
    public int skillPressedIndex { get; private set; } = -1;
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public bool ConsumeJump()
    {
        if (!hasJumped) return false;

        hasJumped = false;
        return true;
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            hasJumped = true;
        }
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
        scrollInput = context.ReadValue<float>();
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
            skillPressedIndex = 0;
        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 1;
        }
    }

    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 2;
        }
    }

    public void OnSkill4(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            skillPressedIndex = 3;
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

        scrollInput = 0f;
        skillPressedIndex = -1;
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

        onLockTarget = false;

        scrollInput = 0f;

        skill1Pressed = false;
        skillPressedIndex = -1;

        attackBufferCounter = 0f;
        bufferedAttackType = AttackInputType.None;

        onMelee = false;
    }
}
