using UnityEngine;

public class IdleState : baseState
{
    public IdleState(PlayerMovementStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }
    public override void Update()
    {
        base.Update();
        
        if (ctx.playerInput !=  Vector2.zero)
        {
            playerMovementStateMachine.ChangeSate(ctx.MovementStateMachine.WalkState);
        }
    }
    public override void Exit()
    {
    }
}
