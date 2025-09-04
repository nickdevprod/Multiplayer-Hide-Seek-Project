using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] float lerpSpeed;
    [SerializeField] float smoothTime;
    [SerializeField] float springForceMultiplier = 0.1f;
    [SerializeField] bool enableSpring = true; 
    
    [Header("Camera references")]
    [HideInInspector] public Transform cameraFollowTransform;
    [HideInInspector] public CharacterController characterController;

    float _startSpringForceMultiplier;
    Vector3 _playerVelocity;
    Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        _startSpringForceMultiplier = springForceMultiplier;
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, GetDesiredPosition(), Time.deltaTime * lerpSpeed);
    }

    Vector3 GetDesiredPosition()
    {
        if (!characterController || !cameraFollowTransform) return Vector3.zero;

        _playerVelocity = characterController.velocity;


        float currentSpringMultiplier = enableSpring ? (_playerVelocity.y > 0 ? 0 : _startSpringForceMultiplier) : 0f;

        Vector3 cameraSmoothDamp = Vector3.SmoothDamp(
            Vector3.zero,
            _playerVelocity * currentSpringMultiplier,
            ref _velocity,
            smoothTime * Time.deltaTime
        );

        Vector3 desiredPosition = cameraFollowTransform.position + new Vector3(0, cameraSmoothDamp.y, 0);

        return desiredPosition;
    }
}