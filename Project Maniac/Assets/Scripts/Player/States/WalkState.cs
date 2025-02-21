using UnityEngine;

public class walkState : baseState
{
    public walkState(PlayerMovementStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }
    
    public override void Enter()
    {
        base.Enter();
        ctx.currentSpeed = ctx.walkSpeed;
    }
    public override void Update()
    {
        base.Update();
        
        if (ctx.playerInput == Vector2.zero)
        {
            playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.IdleState);
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.RunState);
        }
    }
    public override void Exit()
    {
        
    }
}
