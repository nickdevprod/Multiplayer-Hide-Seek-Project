using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCharacterController : NetworkBehaviour
{
    [Header("Movement")] 
    [SerializeField] public float walkSpeed = 10;
    [SerializeField] public float runSpeed;
    [SerializeField] public float jumpForce;
    [SerializeField] public float gravity;
    [SerializeField] public float airControl = 0.5f;
    [Header("Other")]
    [SerializeField] float groundedCheckRadius;
    [SerializeField] Transform footpos;
    [SerializeField] LayerMask groundLayer;


    [HideInInspector] public CharacterController characterController;
    
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public Vector2 playerInput;
    [HideInInspector] public Vector3 movementVector;
    [HideInInspector] public Vector3 velocity;

    public PlayerMovementStateMachine MovementStateMachine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        MovementStateMachine = new PlayerMovementStateMachine(this);
    }
    private void Start()
    {
        MovementStateMachine.currentState = MovementStateMachine.IdleState;
        currentSpeed = walkSpeed;
    }

    public override void OnNetworkSpawn()
    {
        gameObject.name = "player" + NetworkObjectId;
        if (IsOwner)
        {
            GetComponentInChildren<MeshRenderer>().enabled = false;   
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        
        if (IsServer)
        {
            MovePlayer(GetInput());
            
        }else if (IsClient)
        {
            MovePlayerRpc(GetInput());
        }
        
        MovementStateMachine.Update();
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded())
        {
            if (IsOwner)
            {
                JumpPlayerRpc();
            }
        }
        if (!isGrounded() && MovementStateMachine.currentState != MovementStateMachine.FallState)
        {
            MovementStateMachine.ChangeSate(MovementStateMachine.FallState);
        }
        
    }
    [Rpc(SendTo.Server)]
    public void MovePlayerRpc(Vector3 movementVector)
    {
        characterController.Move((movementVector * currentSpeed + velocity)  * Time.deltaTime);
    }
    void MovePlayer(Vector3 movementVector)
    {
        characterController.Move((movementVector * currentSpeed + velocity)  * Time.deltaTime);
    }

    [Rpc(SendTo.Server)]
    void JumpPlayerRpc()
    {
        MovementStateMachine.ChangeSate(MovementStateMachine.JumpState);
    }
    
    [Rpc(SendTo.Server)]
    public void HandleGravityRpc()
    {
        if (isGrounded() && velocity.y < 0){
            velocity.y = -2f;
        }
        else
        {
            velocity.y += Time.deltaTime * gravity;
        }
    }
    public Vector3 GetInput()
    {
        playerInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if(playerInput.sqrMagnitude > 1)
        {
            playerInput.Normalize();
        }

        movementVector = transform.forward * playerInput.y + transform.right * playerInput.x;
        return movementVector;
    }
    public bool isGrounded()
    {
        return Physics.CheckSphere(footpos.position, groundedCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(footpos.position, groundedCheckRadius);
    }
}
