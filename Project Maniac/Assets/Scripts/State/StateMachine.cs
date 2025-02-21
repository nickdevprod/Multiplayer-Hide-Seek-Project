using Unity.VisualScripting;
using UnityEngine;

public abstract class StateMachine
{
    public State currentState;
    
    public void ChangeSate(State newState)
    {
        currentState?.Exit();
        currentState = newState; 
        
        newState.Enter();
    }
    
    public void Enter()
    {
        currentState?.Enter();
    }
    public void Exit()
    {
        currentState?.Exit();
    }
    public void Update()
    {
        currentState?.Update();
    }
}
