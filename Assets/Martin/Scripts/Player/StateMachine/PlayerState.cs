using UnityEngine;

public abstract class PlayerState 
{
    protected PlayerControl player;
    static protected bool falling;
    public PlayerState(PlayerControl player)
    {
        this.player = player;
    }

    public virtual void OnEnter() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnUpdate() {
        if (player.Rb.linearVelocity.y <= 0 && !player.IsGrounded)
        {
            falling = true;
            player.ChangeState(player.fall_State);
            return;
        }
        /*if (!player.IsGrounded)
        {
            player.Anim.SetBool("Falling", true);
            return;
        }*/
    }
    public virtual void OnExit()
    {
        if (player.IsGrounded) { player.Anim.SetBool("Idle", true); falling = false; }
        if (falling) player.Anim.SetBool("Idle", false);
    }
}

