using Unity.Netcode;
using UnityEngine;

public class PlayerBootstrap : NetworkBehaviour
{
    [SerializeField] GameObject cameraGameObject;
    [SerializeField] private Transform cameraFollowPos;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        CreateAndConfigCamera();
    }
    
    void CreateAndConfigCamera()
    {
        GameObject cameraObject = Instantiate(cameraGameObject);
        cameraObject.name = $"PlayerCamera: {OwnerClientId}";; 

        CameraFollow cameraFollowScript = cameraObject.GetComponent<CameraFollow>();
        cameraFollowScript.characterController = GetComponent<CharacterController>();
        cameraFollowScript.cameraFollowTransform = cameraFollowPos;
        
        CameraController cameraControllerScript = cameraObject.GetComponentInChildren<CameraController>();
        cameraControllerScript.playerTransform = transform;
        
        HeadBob headBobScript = cameraObject.GetComponentInChildren<HeadBob>();
        headBobScript._characterController = GetComponent<CharacterController>();
    }
}
