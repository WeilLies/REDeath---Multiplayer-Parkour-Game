using UnityEngine;

public class CameraTilt : MonoBehaviour
{
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float tiltSpeed = 5f;
    private float currentTilt = 0f;
    private IInputManager inputManager;

    private void Awake()
    {
        inputManager = ServiceLocator.Get<IInputManager>();
    }

    private void Update()
    {
        float horizontalInput = inputManager.GetInput<Vector2>("LookInput").x;
    
        float targetTilt = Mathf.Clamp(-horizontalInput, -maxTiltAngle, maxTiltAngle);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        Vector3 currentRotation = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, currentTilt);
    }
}