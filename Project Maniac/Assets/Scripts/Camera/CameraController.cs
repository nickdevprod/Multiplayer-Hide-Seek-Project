using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{

    [SerializeField] float sensitivity;
    [SerializeField] public Transform playerTransform;

    Vector2 mouseVector;
    float _rotationY, _rotationX;

    float _mouseX;
    float _mouseY;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        PlayerInput();
        PlayerRotation();
    }
    void PlayerInput()
    {
        _mouseX = Input.GetAxis("Mouse X");
        _mouseY = Input.GetAxis("Mouse Y");

        mouseVector = new Vector2(_mouseX, _mouseY) * sensitivity;
    }
    void PlayerRotation()
    {
        if (playerTransform == null) return;
        
        _rotationY -= mouseVector.y;
        _rotationX += mouseVector.x;
        _rotationY = Mathf.Clamp(_rotationY, -90, 90);

        transform.localRotation = Quaternion.Euler(_rotationY,0, 0);
        playerTransform.Rotate(playerTransform.up * mouseVector.x);
    }
}
