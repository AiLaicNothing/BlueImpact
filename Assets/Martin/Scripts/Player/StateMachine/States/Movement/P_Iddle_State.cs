using UnityEngine;

public class P_Iddle_State : PlayerState
{
    public P_Iddle_State(PlayerControl player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Iddle State");
        if (player.isPerformingAct)
        {
            return;
        }
        //--> Play the animation

        if (player.Anim != null)
        {
            int idle = Animator.StringToHash("Idle");

            if (player.Anim.HasState(0, idle))
            {
                player.Anim.Play(idle);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing idle State");
            }
        }
    }

    public override void OnUpdate()
    {

        if (!player.IsGrounded)
        {
            player.ChangeState(player.fall_State);
            return;
        }

        if (player.Input.moveInput.magnitude > 0.1f)
        {
            player.ChangeState(player.move_State);
            return;
        }

        if (player.Input.ConsumeJump())
        {
            player.ChangeState(player.jump_State);
            return;
        }
    }
}
