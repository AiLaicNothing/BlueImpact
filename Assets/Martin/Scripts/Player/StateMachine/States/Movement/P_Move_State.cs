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

        if (player.Anim != null)
        {
            int walk = Animator.StringToHash("Walk");

            if (player.Anim.HasState(0, walk))
            {
                player.Anim.SetBool("Walking", true);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing walk State");
            }
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        player.PlayConstantAudio(player.walk, 1, false);

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

    public override void OnExit()
    {
        player.Anim.SetBool("Walking", false);
        player.PlayConstantAudio(player.walk, 1, true);
    }
}
