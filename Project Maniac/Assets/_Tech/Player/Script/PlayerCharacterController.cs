using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField] private GameObject playerVisual;
    [SerializeField] private Camera _camera;
    
    private CharacterController _characterController;
    private Animator _animator;
    private int _speedAnimHash = Animator.StringToHash("Speed"); 
    
    private Vector3 _velocity;
    private Vector2 _input;
    private float _currentSpeed;
    private bool _isGrounded;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        
        if (!IsOwner)
        {
            _camera.gameObject.SetActive(false);
            SetLayerRecursively(playerVisual, LayerMask.NameToLayer("ThirdPerson"));
        };
        transform.name = $"Player: {OwnerClientId}";
    }
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _currentSpeed = walkSpeed;
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

        _currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }
    private void HandleMovementAndGravity()
    {
        SendMovementServerRPC(_input, _isGrounded, _currentSpeed);
    }

    [ServerRpc]
    private void SendMovementServerRPC(Vector2 input, bool isGrounded, float speed)
    {
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        float control = isGrounded ? 1f : airControl;
        Vector3 horizontalMovement = move * speed * control;


        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 finalMovement = horizontalMovement + _velocity;

        _characterController.Move(finalMovement * Time.deltaTime);
    }

    private void HandleAnimations()
    {
        if (IsOwner)
        {
            float moveMagnitude = _input.magnitude * _currentSpeed;
            float normalizedSpeed = moveMagnitude / runSpeed;

            _animator.SetFloat(_speedAnimHash, normalizedSpeed);
        }
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
