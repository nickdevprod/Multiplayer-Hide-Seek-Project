using Unity.VisualScripting;
using UnityEngine;

public class RunState : baseState
{
    public RunState(PlayerMovementStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        ctx.currentSpeed = ctx.runSpeed;
    }
    public override void Update()
    {
        base.Update();
        
        if (ctx.playerInput == Vector2.zero)
        {
            playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.IdleState);
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.WalkState);
        }
    }
    public override void Exit()
    {
        
    }
}
