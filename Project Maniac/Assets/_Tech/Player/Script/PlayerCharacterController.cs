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
    public Vector2 position;
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
    [SerializeField] private Camera _camera;
    
    private CharacterController _characterController;
    private Animator _animator;
    private int _speedAnimHash = Animator.StringToHash("Speed"); 
    
    private Vector3 velocity;
    private float _currentSpeed;
    
    private Vector2 input;
    private bool sprint, jump;
    //Netcode Client Specific
    [SerializeField] private float reconciliationThreshold = 10;
    const int BUFFER_SIZE = 1024;
    private CircularBuffer<InputPayLoad> clientInputBuffer;
    private CircularBuffer<StatePayload> clientStateBuffer;

    private StatePayload lastServerState;
    private StatePayload lastProcessedState;
    
    //Netcode Server Specific
    private CircularBuffer<StatePayload> serverStateBuffer;
    Queue<InputPayLoad> serverInputQueue;

    private int tick;
    
    
    #region Initialization
    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        transform.name = $"Player: {OwnerClientId}";
        
        
        if (!IsOwner)
        {
            _camera.gameObject.SetActive(false);
            playerSkinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        };
        
        
    }
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        clientInputBuffer = new CircularBuffer<InputPayLoad>(BUFFER_SIZE);
        clientStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        
        serverStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        serverInputQueue = new Queue<InputPayLoad>();
        NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
    }
    #endregion
    private void Update()
    {
        if (IsOwner)
        {
            SetInput();
        };
        HandleAnimations();
    }
    private void SetInput()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        sprint = Input.GetKey(KeyCode.LeftShift);
        jump = Input.GetKey(KeyCode.Space);
    }

    private void Tick()
    {
        if(IsOwner)
            HandleClientTick();
        
        if(IsServer)
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
        
        if(bufferIndex == -1) return;
        SendToClientRPC(serverStateBuffer.Get(bufferIndex));
    }
    private void HandleClientTick()
    {
        if(!IsClient) return;
        
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
        if(!ShouldReconcile()) return;

        float positionError;
        int bufferIndex;
        StatePayload rewindState = default;
        
        bufferIndex = lastServerState.tick % BUFFER_SIZE;
        if(bufferIndex - 1 < 0) return;
        
        rewindState  = IsHost ? serverStateBuffer.Get(bufferIndex - 1) :lastServerState;
        positionError = Vector3.Distance(rewindState.position, transform.position);

        if (positionError > reconciliationThreshold)
        {
            transform.position = rewindState.position;
            velocity = rewindState.velocity;
            
            if(!rewindState.Equals(lastServerState)) return;
            clientStateBuffer.Add(rewindState, rewindState.tick);
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < tick)
            {
                int _bufferIndex = tickToReplay % BUFFER_SIZE; 
                StatePayload statePayload = ProcessMovement(clientInputBuffer.Get(_bufferIndex));
                clientStateBuffer.Add(statePayload, _bufferIndex);
                tickToReplay++;
            }
        };
        
        lastProcessedState = lastServerState;
    }

    [ClientRpc]
    private void SendToClientRPC(StatePayload statePayload)
    {
        if(!IsOwner) return;
        lastServerState = statePayload;
    }

    [ServerRpc]
    private void SendToServerRPC(InputPayLoad inputPayLoad)
    {
        serverInputQueue.Enqueue(inputPayLoad);
    }
    StatePayload ProcessMovement(InputPayLoad input)
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
        velocity.y += gravity * Time.deltaTime;
        if (_jump && grounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        if (grounded && velocity.y < 0)
            velocity.y = -2f; 
        
        // Apply movement
        Vector3 finalMovement = horizontalMovement + velocity;
        _characterController.Move(finalMovement * Time.deltaTime);
    }
    

    private void HandleAnimations()
    {
        if (IsOwner)
        {
            float moveMagnitude = input.magnitude * _currentSpeed;
            float normalizedSpeed = moveMagnitude / runSpeed;

            _animator.SetFloat(_speedAnimHash, normalizedSpeed);
        }
    }
    private bool IsGrounded()
    {
        return _characterController.isGrounded;
    }

    private bool ShouldReconcile()
    {
        bool isNewServerState = !lastServerState.Equals(default);
        bool isLastStateUndefinedOrDefault = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);

        return isNewServerState && isLastStateUndefinedOrDefault;
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
