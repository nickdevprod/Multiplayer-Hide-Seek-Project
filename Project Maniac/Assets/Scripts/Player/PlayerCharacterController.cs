using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 10f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _airControl = 0.5f;
    
    [Header("Visuals")]
    [SerializeField] private GameObject _ThirdPersonvisual;
    [SerializeField] private Camera _camera;
    private int _speedAnimHash = Animator.StringToHash("Speed"); 
    
    private NetworkVariable<float> SpeedAnimParam = new NetworkVariable<float>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    private CharacterController _characterController;
    private Animator _animator;
    private Vector3 _velocity;
    private Vector2 _input;
    private float _currentSpeed;
    private bool _isGrounded;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        
        if (!IsOwner) _camera.gameObject.SetActive(false);
        transform.name = $"Player: {OwnerClientId}";
    }
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _currentSpeed = _walkSpeed;
    }

    private void Update()
    {
        if (IsOwner)
        {
            GroundCheck();
            HandleInput();
            HandleMovementAndGravity();
        };
        HandleAnimations();
    }

    private void GroundCheck()
    {
        _isGrounded = _characterController.isGrounded;

        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f; 
    }
    private void HandleInput()
    {
        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (_input.sqrMagnitude > 1f)
            _input.Normalize();

        _currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _runSpeed : _walkSpeed;

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            _velocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravity);
    }
    private void HandleMovementAndGravity()
    {
        Vector3 move = transform.right * _input.x + transform.forward * _input.y;

        float control = _isGrounded ? 1f : _airControl;
        Vector3 horizontalMovement = move * _currentSpeed * control;


        _velocity.y += _gravity * Time.deltaTime;
        
        Vector3 finalMovement = horizontalMovement + _velocity;
        _characterController.Move(finalMovement * Time.deltaTime);
    }

    private void HandleAnimations()
    {
        if (IsOwner)
        {
            float moveMagnitude = _input.magnitude * _currentSpeed;
            float normalizedSpeed = moveMagnitude / _runSpeed;

            _animator.SetFloat(_speedAnimHash, normalizedSpeed);
        }
    }
}
