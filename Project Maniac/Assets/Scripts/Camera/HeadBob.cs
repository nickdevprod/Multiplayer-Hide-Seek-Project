using UnityEngine;


public class HeadBob : MonoBehaviour
{
    
    [Header("GeneralSettings")]
    [SerializeField] bool headBob;
    [Header("Magnitude")]
    [SerializeField] float globalMagnitude = 0.9f;
    [SerializeField] float verticalMagnitude = 0.4f;
    [SerializeField] float horizontalMagnitude = 0.2f;
    [Header("Frequency")]
    [SerializeField] float verticalFrequency = 3.5f;
    [SerializeField] float horizontalFrequency = 2f;
    [SerializeField] float lerpSpeed = 4f;

    [HideInInspector] public CharacterController _characterController;
    float _playerSpeed;
    float _perlinSeed;
    Vector3 _headBobVector;
    
    void Update()
    {
        if(!headBob) return;
        
        _playerSpeed = _characterController.velocity.sqrMagnitude;
        if(_playerSpeed > 0)
        {
            HeadBobLogic();
        }else
        {
            _perlinSeed = 0;
            transform.localPosition = Vector3.Slerp(transform.localPosition, Vector3.zero, Time.deltaTime * lerpSpeed);
        }
    }
    void HeadBobLogic()
    {
        _perlinSeed += Time.deltaTime;
        float verticalBob = (Mathf.PerlinNoise(_perlinSeed * verticalFrequency , 0) - 0.5f) * 2 * globalMagnitude * verticalMagnitude;
        float horizontalBob = (Mathf.PerlinNoise(0, _perlinSeed * horizontalFrequency) - 0.5f) * 2 * globalMagnitude * horizontalMagnitude;

        _headBobVector = new Vector3(horizontalBob, verticalBob, 0);
        transform.localPosition = Vector3.Slerp(transform.localPosition, _headBobVector, Time.deltaTime * lerpSpeed);
    }
}
