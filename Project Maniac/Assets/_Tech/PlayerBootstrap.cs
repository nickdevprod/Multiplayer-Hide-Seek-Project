using Unity.Netcode;
using UnityEngine;

public class PlayerBootstrap : NetworkBehaviour
{
    [SerializeField] GameObject cameraGameObject;
    [SerializeField] private Transform cameraFollowPos;
    private GameObject _container;
    
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerContainer();
            CreateAndConfigCamera();
        }
    }

    void PlayerContainer()
    {
        _container = new GameObject($"PlayerContainer_{OwnerClientId}");
        transform.SetParent(_container.transform, true);
    }
    void CreateAndConfigCamera()
    {
        GameObject cameraObject = Instantiate(cameraGameObject, _container.transform, true);
        var netObj = _container.AddComponent<NetworkObject>();
        netObj.Spawn();

        CameraFollow cameraFollowScript = cameraObject.GetComponent<CameraFollow>();
        CameraController cameraControllerScript = cameraObject.GetComponentInChildren<CameraController>();
        HeadBob headBobScript = cameraObject.GetComponentInChildren<HeadBob>();

        cameraFollowScript.characterController = GetComponent<CharacterController>();
        cameraFollowScript.cameraFollowTransform = cameraFollowPos;
        
        cameraControllerScript.playerTransform = transform;
        headBobScript._characterController = GetComponent<CharacterController>();
    }
}
