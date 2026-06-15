using UnityEngine;

public class P_Jump_State : PlayerState
{
    public P_Jump_State(PlayerControl player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Jump State");
        if (player.isPerformingAct)
        {
            return;
        }

        player.Jump();


        if (player.Anim != null)
        {
            int jump = Animator.StringToHash("Jump");

            if (player.Anim.HasState(0, jump))
            {
                player.Anim.Play(jump);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing jump State");
            }
        }
    }

    public override void OnUpdate()
    {
        if (player.Rb.linearVelocity.y <= 0)
        {
            player.ChangeState(player.fall_State);
            return;
        }
    }
    public override void OnExit()
    {
        Debug.Log("Exit Fall State");
    }
}
