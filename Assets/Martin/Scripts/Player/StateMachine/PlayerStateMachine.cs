using UnityEngine;

public class PlayerStateMachine 
{
    public PlayerState currentState { get; private set; }

    public void Initialize(PlayerState InitialState)
    {
        currentState = InitialState;
        currentState.OnEnter();
    }

    public void ChangeState(PlayerState nextState)
    {
        currentState.OnExit();
        currentState = nextState;
        currentState.OnEnter();
    }

    public void Update()
    {
        currentState?.OnUpdate();
    }

    public void FixedUpdate()
    {
        currentState?.OnFixedUpdate();
    }
}
