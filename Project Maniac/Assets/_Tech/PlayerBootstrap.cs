using Unity.Netcode;
using UnityEngine;

public class PlayerBootstrap : NetworkBehaviour
{
    [SerializeField] GameObject cameraGameObject;
    [SerializeField] private Transform cameraFollowPos;
    
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            CreateAndConfigCamera();
        }
    }
    
    void CreateAndConfigCamera()
    {
        GameObject cameraObject = Instantiate(cameraGameObject);

        CameraFollow cameraFollowScript = cameraObject.GetComponent<CameraFollow>();
        CameraController cameraControllerScript = cameraObject.GetComponentInChildren<CameraController>();
        HeadBob headBobScript = cameraObject.GetComponentInChildren<HeadBob>();

        cameraFollowScript.characterController = GetComponent<CharacterController>();
        cameraFollowScript.cameraFollowTransform = cameraFollowPos;
        
        cameraControllerScript.playerTransform = transform;
        headBobScript._characterController = GetComponent<CharacterController>();
    }
}
