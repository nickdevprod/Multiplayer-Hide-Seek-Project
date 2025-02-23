using UnityEngine;

public class FallState : baseState
{
    public FallState(PlayerMovementStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }
    
    public override void Enter()
    {
       base.Enter();
    }
    public override void Update()
    {
        base.Update();
        
        if (ctx.isGrounded())
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.RunState);
            }
            else
            {
                playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.IdleState);
            }
        }
    }
    public override void Exit()
    {
        
    }
}
