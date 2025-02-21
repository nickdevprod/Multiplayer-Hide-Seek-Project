using UnityEngine;

public class baseState : State
{
    protected PlayerCharacterController ctx;
    protected StateMachine playerMovementStateMachine;
    
    public baseState(PlayerMovementStateMachine playerStateMachine)
    {
        playerMovementStateMachine = playerStateMachine;
        ctx = playerStateMachine.PlayerCharacterController;
    }
    public override void Enter()
    {
        Debug.Log("State is: " + this.GetType());
    }
    //since most of the classes use this functions
    public override void Update()
    {
        ctx.MovePlayer();
        ctx.HandleGravity();
    }
    public override void Exit()
    {
  
    }
}
