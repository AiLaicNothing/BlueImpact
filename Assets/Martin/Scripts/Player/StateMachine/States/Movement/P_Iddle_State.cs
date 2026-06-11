using UnityEngine;

public class P_Iddle_State : PlayerState
{
    public P_Iddle_State(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Iddle State");
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
