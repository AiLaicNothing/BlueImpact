using UnityEngine;

public class P_Move_State : PlayerState
{
    public P_Move_State(PlayerControl player) : base(player) { }

    public override void OnEnter()
    {
        if (player.isPerformingAct)
        {
            return;
        }
        //--> Play the animation
    }

    public override void OnUpdate()
    {
        if (!player.IsGrounded)
        {
            player.ChangeState(player.fall_State);
            return;
        }

        if (player.Input.moveInput.magnitude < 0.1f)
        {
            player.ChangeState(player.iddle_State);
            return;
        }

        if (player.Input.ConsumeJump())
        {
            player.ChangeState(player.jump_State);
            return;
        }
    }
}
