using UnityEngine;

public abstract class PlayerState 
{
    protected PlayerControl player;
    public PlayerState(PlayerControl player)
    {
        this.player = player;
    }

    public virtual void OnEnter() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}

