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
                player.Anim.SetBool("Idle2", false);
                player.Anim.SetBool("Idle", true);
            }
            else
            {
                Debug.Log("[PlayerAnimator] is missing idle State");
            }
        }
        timerIdle2 = 0;
    }

    public override void OnUpdate()
    {

        if (!player.IsGrounded())
        {
            player.ChangeState(player.fall_State);
            return;
        }

        if (player.Input.moveInput.magnitude > 0.1f)
        {
            player.ChangeState(player.move_State);
            return;
        }

        if (player.Input.hasJumped)
        {
            player.ChangeState(player.jump_State);
            return;
        }
        if(timerIdle2 < 5f) timerIdle2 += Time.deltaTime;
        if(timerIdle2 >= 5f)
        {
            if (player.Anim != null)
            {
                int idle2 = Animator.StringToHash("Idle2");

                if (player.Anim.HasState(0, idle2))
                {
                    player.Anim.SetBool("Idle2", true);
                }
                else
                {
                    Debug.Log("[PlayerAnimator] is missing idle2 State");
                }
            }
        }

    }

    public override void OnExit()
    {
        timerIdle2 = 0;

        player.Anim.SetBool("Idle2", false);
        player.Anim.SetBool("Idle", false);
    }
}
