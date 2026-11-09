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
        // Los pasos los controla PlayerControl.UpdateFootsteps (no desde el estado, para que no suenen en dash, ataque o muerte)

        if (!player.IsGrounded())
        {
            player.ChangeState(player.fall_State);
            return;
        }

        if (player.Input.moveInput.magnitude < 0.1f)
        {
            player.ChangeState(player.iddle_State);
            return;
        }

        if (player.Input.hasJumped)
        {
            player.ChangeState(player.jump_State);
            return;
        }
        if (player.lockOnTarget.isTargeting)
        {
            //Debug.Log("LockON Anim");
            if (player.Anim != null)
            {
                int lockOn = Animator.StringToHash("LockOn");

                if (player.Anim.HasState(0, lockOn))
                {
                    player.Anim.SetBool("LockOn", true);
                }
                else
                {
                    Debug.Log("[PlayerAnimator] is missing LockOn State");
                }
            }
        }
        else
        {
            player.Anim.SetBool("LockOn", false);
        }
    }

    public override void OnExit()
    {
        player.Anim.SetBool("Walking", false);
        player.PlayConstantAudio(player.walk, 1, true);
    }
}
