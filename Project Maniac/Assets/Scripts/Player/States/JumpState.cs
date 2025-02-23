using UnityEngine;

public class JumpState : baseState
{
    public JumpState(PlayerMovementStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }
    
    public override void Enter()
    {
        base.Enter();
        ctx.currentSpeed *= ctx.airControl;
        ctx.velocity.y = Mathf.Sqrt(ctx.jumpForce * -3.0f * ctx.gravity);
        Debug.Log(ctx.velocity.y);
    }
    public override void Update()
    { 
       base.Update(); 
        
       if (ctx.velocity.y < 0)
       {
           playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.FallState);
       }
    }
    public override void Exit()
    {
        
    }
}
