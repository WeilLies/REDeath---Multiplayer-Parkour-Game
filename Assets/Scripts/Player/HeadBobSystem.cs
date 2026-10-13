using UnityEngine;

public class HeadBobSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraTransform;

    [Header("Bob Frequency Settings")]
    [SerializeField] private float stepsPerMeter = 1.8f;
    [SerializeField] private float minSpeedToBob = 0.1f;
    [SerializeField] private float minInputToBob = 0.1f;

    [Header("Bob Amplitude Settings")]
    [SerializeField] private float verticalBobAmplitude = 0.05f;
    [SerializeField] private float horizontalBobAmplitude = 0.03f;
    [SerializeField] private AnimationCurve amplitudeBySpeedCurve = AnimationCurve.Linear(0f, 0f, 6f, 1f);
    [SerializeField] private float maxReferenceSpeedForAmplitude = 6f;
    [SerializeField] private float crouchAmplitudeMultiplier = 0.6f;

    [Header("Tilt Settings")]
    [SerializeField] private float rollTiltAmplitude = 1.5f;

    [Header("Speed Tilt Settings")]
    [SerializeField] private float tiltSpeedThreshold = 6f;
    [SerializeField] private float tiltMaxReferenceSpeed = 12f;
    [SerializeField] private float maxSpeedTiltAngle = 8f;
    [SerializeField] private AnimationCurve tiltAngleBySpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float speedTiltSmoothSpeed = 8f;

    [Header("Smoothing Settings")]
    [SerializeField] private float amplitudeBlendSpeed = 6f;
    [SerializeField] private float positionSmoothSpeed = 12f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    private Vector3 initialLocalPosition;
    private float bobCycleTimer;
    private float currentAmplitudeMultiplier;
    private float currentRoll;
    private float currentSpeedTiltAngle;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (cameraTransform == null)
            cameraTransform = transform;

        initialLocalPosition = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (playerController == null || cameraTransform == null)
            return;

        float speedForBob = playerController.CurrentMovementSpeed;
        bool hasInput = playerController.CurrentInputVector.sqrMagnitude > minInputToBob * minInputToBob;

        bool isBobbingActive = playerController.IsGrounded &&
                                hasInput &&
                                speedForBob > minSpeedToBob;

        float stepFrequency = speedForBob * stepsPerMeter;
        bobCycleTimer += stepFrequency * Time.deltaTime;
        bobCycleTimer %= 1f;

        float targetAmplitudeMultiplier = isBobbingActive ? 1f : 0f;
        currentAmplitudeMultiplier = Mathf.MoveTowards(currentAmplitudeMultiplier, targetAmplitudeMultiplier,
            amplitudeBlendSpeed * Time.deltaTime);

        float clampedSpeed = Mathf.Clamp(speedForBob, 0f, maxReferenceSpeedForAmplitude);
        float speedAmplitudeFactor = amplitudeBySpeedCurve.Evaluate(clampedSpeed);
        float crouchFactor = playerController.IsCrouching ? crouchAmplitudeMultiplier : 1f;

        float totalAmplitudeFactor = currentAmplitudeMultiplier * speedAmplitudeFactor * crouchFactor;

        float verticalPhase = (bobCycleTimer * 2f) % 1f;
        float verticalParabola = 4f * verticalPhase * (1f - verticalPhase);
        float verticalOffset = verticalParabola * verticalBobAmplitude * totalAmplitudeFactor;

        float bobOscillation = Mathf.Sin(bobCycleTimer * Mathf.PI * 2f);
        float horizontalOffset = bobOscillation * horizontalBobAmplitude * totalAmplitudeFactor;

        Vector3 targetLocalPosition = initialLocalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetLocalPosition,
            positionSmoothSpeed * Time.deltaTime);

        float targetRoll = -bobOscillation * rollTiltAmplitude * totalAmplitudeFactor;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, rotationSmoothSpeed * Time.deltaTime);

        float tiltRange = Mathf.Max(0.01f, tiltMaxReferenceSpeed - tiltSpeedThreshold);
        float tiltSpeedT = Mathf.Clamp01((speedForBob - tiltSpeedThreshold) / tiltRange);
        float tiltFactor = tiltAngleBySpeedCurve.Evaluate(tiltSpeedT);
        float targetSpeedTiltAngle = -bobOscillation * maxSpeedTiltAngle * tiltFactor * currentAmplitudeMultiplier;
        currentSpeedTiltAngle = Mathf.Lerp(currentSpeedTiltAngle, targetSpeedTiltAngle,
            speedTiltSmoothSpeed * Time.deltaTime);

        float finalRoll = currentRoll + currentSpeedTiltAngle;
        cameraTransform.localRotation = Quaternion.Euler(0f, 0f, finalRoll);
    }
}