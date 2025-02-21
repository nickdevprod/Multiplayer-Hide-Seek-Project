using UnityEngine;

public class PlayerMovementStateMachine : StateMachine
{
    public PlayerCharacterController PlayerCharacterController { get; }

    public IdleState IdleState { get; private set; }
    public RunState RunState { get; private set; }
    public walkState WalkState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }

    public PlayerMovementStateMachine(PlayerCharacterController ctx)
    {
        PlayerCharacterController = ctx;
        
        IdleState = new IdleState(this);
        RunState = new RunState(this);
        WalkState = new walkState(this);
        JumpState = new JumpState(this);
        FallState = new FallState(this);
    }
}