using System.Collections;
using UnityEngine;

public class CameraImpactTiltSystem : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform externalYawSource;

    [Header("Jump Tilt Settings")]
    [SerializeField] private float jumpLookUpAngle = 6f;
    [SerializeField] private float jumpRiseDuration = 0.12f;
    [SerializeField] private float jumpFallDuration = 0.25f;
    [SerializeField] private AnimationCurve jumpRiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve jumpFallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Landing Shake Settings")]
    [SerializeField] private float minLandingSpeedForShake = 2f;
    [SerializeField] private float maxLandingSpeedForShake = 10f;
    [SerializeField] private float maxLandingShakeAngle = 10f;
    [SerializeField] private float maxLandingShakePositionOffset = 0.08f;
    [SerializeField] private float landingShakeDuration = 0.4f;
    [SerializeField] private float landingShakeFrequency = 18f;
    [SerializeField] private float landingShakeBlendInTime = 0.06f;
    [SerializeField] private AnimationCurve landingShakeFalloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float jumpPitchOffset;
    private Vector3 shakePositionOffset;
    private float shakeRotationOffset;
    private Vector3 basePosition;

    private Coroutine jumpRoutine;
    private Coroutine landingRoutine;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (cameraTransform == null)
            cameraTransform = transform;

        basePosition = cameraTransform.localPosition;
    }

    private void OnEnable()
    {
        if (playerController == null)
            return;

        playerController.OnPlayerJump += HandleJump;
        playerController.OnPlayerFallDown += HandleLanding;
    }

    private void OnDisable()
    {
        if (playerController == null)
            return;

        playerController.OnPlayerJump -= HandleJump;
        playerController.OnPlayerFallDown -= HandleLanding;
    }

    private void HandleJump()
    {
        if (jumpRoutine != null)
            StopCoroutine(jumpRoutine);

        jumpRoutine = StartCoroutine(JumpTiltRoutine());
    }

    private IEnumerator JumpTiltRoutine()
    {
        yield return LerpValue(jumpPitchOffset, -jumpLookUpAngle, jumpRiseDuration, jumpRiseCurve,
            value => jumpPitchOffset = value);

        yield return LerpValue(jumpPitchOffset, 0f, jumpFallDuration, jumpFallCurve,
            value => jumpPitchOffset = value);

        jumpPitchOffset = 0f;
        jumpRoutine = null;
    }

    private IEnumerator LerpValue(float startValue, float targetValue, float duration, AnimationCurve curve, System.Action<float> setter)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float easedTime = curve.Evaluate(normalizedTime);
            setter(Mathf.Lerp(startValue, targetValue, easedTime));
            yield return null;
        }

        setter(targetValue);
    }

    private void HandleLanding(float landingSpeed)
    {
        if (landingSpeed < minLandingSpeedForShake)
            return;

        if (landingRoutine != null)
            StopCoroutine(landingRoutine);

        float speedFactor = Mathf.InverseLerp(minLandingSpeedForShake, maxLandingSpeedForShake, landingSpeed);
        landingRoutine = StartCoroutine(LandingShakeRoutine(speedFactor, shakePositionOffset, shakeRotationOffset));
    }

    private IEnumerator LandingShakeRoutine(float intensityFactor, Vector3 blendFromPosition, float blendFromRotation)
    {
        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, landingShakeDuration);
        float safeBlendTime = Mathf.Max(0.001f, landingShakeBlendInTime);
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);
        float seedZ = Random.Range(0f, 100f);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float falloff = landingShakeFalloffCurve.Evaluate(normalizedTime) * intensityFactor;

            float noiseX = Mathf.PerlinNoise(seedX, elapsedTime * landingShakeFrequency) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(seedY, elapsedTime * landingShakeFrequency) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(seedZ, elapsedTime * landingShakeFrequency) * 2f - 1f;

            Vector3 targetPosition = new Vector3(noiseX, noiseY, 0f) * maxLandingShakePositionOffset * falloff;
            float targetRotation = noiseZ * maxLandingShakeAngle * falloff;

            float blendFactor = Mathf.Clamp01(elapsedTime / safeBlendTime);

            shakePositionOffset = Vector3.Lerp(blendFromPosition, targetPosition, blendFactor);
            shakeRotationOffset = Mathf.Lerp(blendFromRotation, targetRotation, blendFactor);

            yield return null;
        }

        shakePositionOffset = Vector3.zero;
        shakeRotationOffset = 0f;
        landingRoutine = null;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        float externalYaw = externalYawSource != null ? externalYawSource.localEulerAngles.y : 0f;

        cameraTransform.localPosition = basePosition + shakePositionOffset;
        cameraTransform.localRotation = Quaternion.Euler(jumpPitchOffset, externalYaw, shakeRotationOffset);
    }
}