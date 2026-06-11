using UnityEngine;

public abstract class PlayerState 
{
    protected PlayerController player;
    public PlayerState(PlayerController player)
    {
        this.player = player;
    }

    public virtual void OnEnter() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}

