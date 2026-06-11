using UnityEngine;

public class P_Fall_State : PlayerState
{
    public P_Fall_State(PlayerController player) : base(player) { }
    public override void OnEnter()
    {
        Debug.Log("Enter Fall State");

        if (player.isPerformingAct)
        {
            return;
        }

        player.SetGravityMultiplier(player.FallGravityMult);
    }

    public override void OnUpdate()
    {
        if (player.IsGrounded)
        {
            if (player.Input.moveInput.magnitude > 0.1f)
            {
                player.ChangeState(player.move_State);
                return;
            }
            else
            {
                player.ChangeState(player.iddle_State);
            }
        }
    }

    public override void OnExit()
    {
        Debug.Log("Exit Fall State");

        player.SetGravityMultiplier(1f);
    }
}
