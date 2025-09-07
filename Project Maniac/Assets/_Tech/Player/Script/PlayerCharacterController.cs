using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

#region Data
public struct InputPayLoad : INetworkSerializable
{
    public int tick;
    public Vector2 inputVector;
    public bool jump;
    public bool sprint;  
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref inputVector);
        serializer.SerializeValue(ref jump);
        serializer.SerializeValue(ref sprint);
    }
}
public struct StatePayload : INetworkSerializable
{
    public int tick;
    public Vector3 position;  
    public Vector3 velocity;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref velocity);
    }
}
#endregion

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float airControl = 0.5f;
    
    [Header("Visuals")]
    [SerializeField] private GameObject rotationObject;
    [SerializeField] private SkinnedMeshRenderer playerSkinnedMeshRenderer;
    
    private CharacterController _characterController;
    private Animator _animator;
    private int _speedAnimHash = Animator.StringToHash("Speed"); 
    
    private Vector3 velocity;
    private float _currentSpeed;
    
    private Vector2 input;
    private bool sprint, jump;
    
    // Netcode Client Specific
    [SerializeField] private float reconciliationThreshold = 0.1f;  
    const int BUFFER_SIZE = 1024;
    private CircularBuffer<InputPayLoad> clientInputBuffer;
    private CircularBuffer<StatePayload> clientStateBuffer;

    private StatePayload lastServerState;
    private StatePayload lastProcessedState;
    
    // Netcode Server Specific
    private CircularBuffer<StatePayload> serverStateBuffer;
    private Queue<InputPayLoad> serverInputQueue;

    private int tick;
    private float TimeBetweenTicks => 1f / NetworkManager.Singleton.NetworkTickSystem.TickRate;
    
    #region Initialization
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();  // Fixed: was calling OnNetworkDespawn
        transform.name = $"Player: {OwnerClientId}";
        
        if (!IsOwner)
        {
            if (playerSkinnedMeshRenderer != null)
                playerSkinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        clientInputBuffer = new CircularBuffer<InputPayLoad>(BUFFER_SIZE);
        clientStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        
        serverStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        serverInputQueue = new Queue<InputPayLoad>();
    }
    
    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkTickSystem != null)
        {
            NetworkManager.Singleton.NetworkTickSystem.Tick -= Tick;
        }
        base.OnNetworkDespawn();
    }
    
    private void Start()
    {
        // Subscribe to tick system after NetworkManager is ready
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
        }
    }
    #endregion
    
    private void Update()
    {
        if (IsOwner)
        {
            SetInput();
        }
        HandleAnimations();
    }
    
    private void SetInput()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        sprint = Input.GetKey(KeyCode.LeftShift);
        jump = Input.GetKeyDown(KeyCode.Space);  // Changed to GetKeyDown for proper jump detection
    }

    private void Tick()
    {
        if (IsOwner)
            HandleClientTick();
        
        if (IsServer && !IsOwner)
            HandleServerTick();
        
        tick++;
    }

    private void HandleServerTick()
    {
        var bufferIndex = -1;
        while (serverInputQueue.Count > 0)
        {
            InputPayLoad inputPayload = serverInputQueue.Dequeue();
            bufferIndex = inputPayload.tick % BUFFER_SIZE;
            
            StatePayload statePayload = ProcessMovement(inputPayload);
            serverStateBuffer.Add(statePayload, bufferIndex);
        }
        
        if (bufferIndex == -1) return;
        SendToClientRPC(serverStateBuffer.Get(bufferIndex));
    }
    
    private void HandleClientTick()
    {
        if (!IsClient) return;
        
        var bufferIndex = tick % BUFFER_SIZE;
        InputPayLoad inputPayLoad = new InputPayLoad()
        {
            tick = tick,
            inputVector = input,
            jump = jump,
            sprint = sprint,
        };
        
        clientInputBuffer.Add(inputPayLoad, bufferIndex);
        SendToServerRPC(inputPayLoad);

        StatePayload statePayload = ProcessMovement(inputPayLoad);
        clientStateBuffer.Add(statePayload, bufferIndex);

        HandleReconciliation();
    }

    private void HandleReconciliation()
    {
        if (!ShouldReconcile()) return;

        int serverBufferIndex = lastServerState.tick % BUFFER_SIZE;
        
        // Get the client state at the same tick as server
        StatePayload clientStateAtServerTick = clientStateBuffer.Get(serverBufferIndex);
        float positionError = Vector3.Distance(lastServerState.position, clientStateAtServerTick.position);

        if (positionError > reconciliationThreshold)
        {
            // Reconcile: set position and velocity to server values
            transform.position = lastServerState.position;
            velocity = lastServerState.velocity;
            
            // Update client state buffer with corrected server state
            clientStateBuffer.Add(lastServerState, serverBufferIndex);
            
            // Replay all inputs after the server state
            int tickToReplay = lastServerState.tick + 1;
            int currentClientTick = tick - 1; // Current client tick before increment
            
            while (tickToReplay <= currentClientTick)
            {
                int replayBufferIndex = tickToReplay % BUFFER_SIZE;
                InputPayLoad inputToReplay = clientInputBuffer.Get(replayBufferIndex);
                
                // Only replay if we have valid input for this tick
                if (inputToReplay.tick == tickToReplay)
                {
                    StatePayload replayedState = ProcessMovement(inputToReplay);
                    clientStateBuffer.Add(replayedState, replayBufferIndex);
                }
                tickToReplay++;
            }
        }
        
        lastProcessedState = lastServerState;
    }

    [ClientRpc]
    private void SendToClientRPC(StatePayload statePayload)
    {
        if (!IsOwner) return;
        lastServerState = statePayload;
    }

    [ServerRpc]
    private void SendToServerRPC(InputPayLoad inputPayLoad)
    {
        serverInputQueue.Enqueue(inputPayLoad);
    }
    
    private StatePayload ProcessMovement(InputPayLoad input)
    {
        Move(input.inputVector, input.sprint, input.jump);

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
            velocity = velocity
        };
    }
    private void Move(Vector2 _input, bool _sprint, bool _jump)
    {
        bool grounded = IsGrounded();
        
        // Horizontal movement
        Vector3 move = transform.right * _input.x + transform.forward * _input.y;
        float control = grounded ? 1f : airControl;
        _currentSpeed = _sprint ? runSpeed : walkSpeed;
        Vector3 horizontalMovement = move * _currentSpeed * control;
        
        // Gravity & jump
        if (grounded && velocity.y < 0)
            velocity.y = -2f; 
            
        velocity.y += gravity * TimeBetweenTicks;
        
        if (_jump && grounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        
        // Apply movement
        Vector3 finalMovement = horizontalMovement + Vector3.up * velocity.y;
        _characterController.Move(finalMovement * TimeBetweenTicks);
    }
    
    private void HandleAnimations()
    {
        if (_animator == null) return;
        
        // Use immediate input for responsive animations
        float moveMagnitude = input.magnitude * _currentSpeed;
        float normalizedSpeed = moveMagnitude / runSpeed;
        
        // Smooth animation transitions
        float currentAnimSpeed = _animator.GetFloat(_speedAnimHash);
        float targetAnimSpeed = normalizedSpeed;
        float smoothedSpeed = Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 10f);
        
        _animator.SetFloat(_speedAnimHash, smoothedSpeed);
    }
    private bool IsGrounded()
    {
        return _characterController.isGrounded;
    }
    private bool ShouldReconcile()
    {
        bool isNewServerState = !lastServerState.Equals(default(StatePayload));
        bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default(StatePayload)) || 
                                             lastProcessedState.tick != lastServerState.tick;

        return isNewServerState && isLastStateUndefinedOrDifferent;
    }
    public static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}