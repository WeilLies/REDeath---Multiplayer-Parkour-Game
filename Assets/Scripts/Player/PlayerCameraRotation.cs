using UnityEngine;

public class PlayerCameraRotation : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxAngleY = 90f;
    [SerializeField] private float smoothTime = 0.05f;
    public Camera PlayerCamera => playerCamera;

    private Vector2 cameraRotation;
    private Vector2 currentInput;
    private Vector2 smoothedInput;
    private Vector2 inputVelocity;

    private IInputManager inputManager;
    private ISettingsManager settingsManager;
    private IInputState inputState;

    private float cameraSensitivity = 1f;

    private void Awake()
    {
        inputManager = ServiceLocator.Get<IInputManager>();
        settingsManager = ServiceLocator.Get<ISettingsManager>();
        inputState = ServiceLocator.Get<IInputState>();

        if (inputState != null)
            inputState.AddLock(InputState.LockType.Cursor, "InitPlayer");

        if (settingsManager != null)
            settingsManager.OnParametersChanged += ChangeSensitivityValue;

        ChangeSensitivityValue();
    }

    private void OnDisable()
    {
        if (settingsManager != null)
            settingsManager.OnParametersChanged -= ChangeSensitivityValue;
    }

    private void Update() 
        => CameraRotation();

    private void ChangeSensitivityValue() 
        => cameraSensitivity = settingsManager != null ? settingsManager.GetParametersValue<float>("Sensitivity") : 1f;

    private void CameraRotation()
    {
        if (inputManager == null || inputState == null) { return; }

        bool cursorLocked = inputState.GetLockState(InputState.LockType.Cursor);
        bool cameraLocked = inputState.GetLockState(InputState.LockType.Camera);

        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !cursorLocked;

        if (cameraLocked) { return; }

        currentInput = inputManager.GetInput<Vector2>("LookInput") * cameraSensitivity;

        smoothedInput.x = Mathf.SmoothDamp(smoothedInput.x, currentInput.x, ref inputVelocity.x, smoothTime);
        smoothedInput.y = Mathf.SmoothDamp(smoothedInput.y, currentInput.y, ref inputVelocity.y, smoothTime);

        cameraRotation.x += smoothedInput.x;
        cameraRotation.y += smoothedInput.y;

        cameraRotation.y = Mathf.Clamp(cameraRotation.y, -maxAngleY, maxAngleY);

        cameraTransform.localRotation = Quaternion.Euler(-cameraRotation.y, 0f, 0f);
        bodyTransform.rotation = Quaternion.Euler(0f, cameraRotation.x, 0f);
    }
}
