using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float playerWalkSpeed = 2.5f;
    [SerializeField] private float playerRunSpeed = 5f;
    [SerializeField] private float playerCrouchSpeed = 1.25f;
    [SerializeField] private float smoothInputSpeed = 0.15f;
    [SerializeField] private float smoothChangeSpeedValue = 5f;

    [Header("Run Acceleration Settings")]
    [SerializeField] private AnimationCurve runSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float runAccelerationDuration = 3f;

    [Header("Turning Settings")]
    [SerializeField] private float maxTurnAngleForFullStop = 180f;
    [SerializeField] private float turnAngleRecoverySpeed = 540f;
    [SerializeField] private float turnSensitivity = 1f;
    [SerializeField] private float minAngularSpeedToAccumulate = 1f;
    [SerializeField] private AnimationCurve turnSpeedPenaltyCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private float minTurnSpeed = 0.5f;

    [Header("Crouching Settings")]
    [SerializeField] private LayerMask crouchStandLayers;
    [SerializeField] private float crouchTransitionSpeed = 4f;
    [SerializeField] private float crouchHeight = 1f;

    [Header("Jump Settings")]
    [SerializeField] private float airControlFactor = 0.1f;
    [SerializeField] private float playerGravity = -19.8f;
    [SerializeField] private float jumpHeight = 0.4f;

    [Header("Landing Inertia Settings")]
    [SerializeField] private float landingInertiaStrength = 0.25f;
    [SerializeField] private float landingInertiaDamping = 6f;
    [SerializeField] private float minLandingSpeedForInertia = 3f;
    [SerializeField] private float maxLandingInertiaSpeed = 6f;

    [Header("Wall Collision Settings")]
    [SerializeField] private float minIntendedSpeedToCheckWall = 0.05f;
    [SerializeField][Range(0f, 1f)] private float wallBlockRatioThreshold = 0.3f;

    [Header("Landing Event Settings")]
    [SerializeField] private float minAirborneTimeForLanding = 0.1f;

    private float startHeight;
    private float targetHeight;

    private CharacterController characterController;

    private Vector3 playerVelocity;
    private Vector2 smoothInputVelocity;
    private Vector3 moveDirection;
    private Vector3 landingInertiaVelocity;

    private bool wishCrouch;
    private float currentHeightVelocity;
    private float sprintElapsedTime;

    private float previousYaw;
    private float turnAngleAccumulator;
    private float turnSpeedMultiplier = 1f;

    private Vector3 lastIntendedVelocity;

    public Vector2 CurrentInputVector { get; private set; }
    public Vector2 CurrentInputVectorInt { get; private set; }
    public float CurrentMovementSpeed { get; private set; }
    public float CurrentFullSpeed { get; private set; }
    public Vector2 MoveVector { get; private set; }
    public float TurnSpeedMultiplier => turnSpeedMultiplier;
    public bool IsSprinting { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }

    private float ungroundedTime;

    public event Action OnPlayerJump;
    public event Action<float> OnPlayerFallDown;
    public event Action OnPlayerCrouchUp;
    public event Action OnPlayerCrouchDown;

    private IInputManager inputManager;
    private IInputState inputState;

    private void Awake()
    {
        inputManager = ServiceLocator.Get<IInputManager>();
        inputState = ServiceLocator.Get<IInputState>();

        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        startHeight = characterController.height;
        targetHeight = startHeight;
        previousYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        GroundCheck();
        HandleCrouch();
        HandleTurning();
        HandleMovementAndJump();
    }

    private void HandleTurning()
    {
        float currentYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(previousYaw, currentYaw));
        previousYaw = currentYaw;

        float angularSpeed = Time.deltaTime > 0f ? yawDelta / Time.deltaTime : 0f;

        if (angularSpeed > minAngularSpeedToAccumulate)
            turnAngleAccumulator += yawDelta * turnSensitivity;
        else
            turnAngleAccumulator = Mathf.MoveTowards(turnAngleAccumulator, 0f, turnAngleRecoverySpeed * Time.deltaTime);

        turnAngleAccumulator = Mathf.Clamp(turnAngleAccumulator, 0f, maxTurnAngleForFullStop);

        float normalizedTurn = maxTurnAngleForFullStop > 0f
            ? turnAngleAccumulator / maxTurnAngleForFullStop
            : 0f;

        turnSpeedMultiplier = turnSpeedPenaltyCurve.Evaluate(normalizedTurn);
    }

    private void GroundCheck()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = characterController.isGrounded;

        if (!IsGrounded)
            ungroundedTime += Time.deltaTime;

        if (IsGrounded && !wasGrounded && ungroundedTime >= minAirborneTimeForLanding)
        {
            Vector3 preLandingVelocity = lastIntendedVelocity;
            Vector3 horizontalVelocity = new Vector3(preLandingVelocity.x, 0f, preLandingVelocity.z);

            if (horizontalVelocity.magnitude >= minLandingSpeedForInertia)
            {
                Vector3 clampedVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxLandingInertiaSpeed);
                landingInertiaVelocity += clampedVelocity * landingInertiaStrength;
            }

            OnPlayerFallDown?.Invoke(preLandingVelocity.magnitude);
        }

        if (IsGrounded)
            ungroundedTime = 0f;
    }

    private void HandleMovementAndJump()
    {
        bool moveLocked = inputState.GetLockState(InputState.LockType.Move);

        Vector2 moveInput = inputManager.GetInput<Vector2>("MoveInput");
        float sprintInput = inputManager.GetInput<Vector2>("MoveInput").y;
        bool jumpInput = inputManager.GetInput<bool>("JumpInput");

        MoveVector = moveLocked ? Vector2.zero : moveInput;

        IsSprinting = !moveLocked &&
                    sprintInput > 0f &&
                    MoveVector.y > 0f &&
                    !IsCrouching;

        if (IsSprinting)
            sprintElapsedTime = Mathf.Min(sprintElapsedTime + Time.deltaTime, runAccelerationDuration);
        else
            sprintElapsedTime = 0f;

        CurrentInputVector = Vector2.SmoothDamp(CurrentInputVector, MoveVector, ref smoothInputVelocity, smoothInputSpeed);
        CurrentInputVectorInt = new Vector2(Mathf.Round(CurrentInputVector.x), Mathf.Round(CurrentInputVector.y));

        Vector3 rightMove = transform.right * CurrentInputVector.x;
        Vector3 forwardMove = transform.forward * CurrentInputVector.y;

        float targetSpeed;

        if (IsCrouching)
        {
            targetSpeed = playerCrouchSpeed;
        }
        else if (IsSprinting)
        {
            float normalizedTime = runAccelerationDuration > 0f ? sprintElapsedTime / runAccelerationDuration : 1f;
            float curveValue = runSpeedCurve.Evaluate(normalizedTime);
            targetSpeed = Mathf.Lerp(playerWalkSpeed, playerRunSpeed, curveValue);
        }
        else
        {
            targetSpeed = playerWalkSpeed;
        }

        CurrentMovementSpeed = Mathf.Lerp(CurrentMovementSpeed, targetSpeed, Time.deltaTime * smoothChangeSpeedValue);

        float effectiveMovementSpeed = CurrentMovementSpeed;

        if (IsGrounded)
        {
            float penalizedSpeed = CurrentMovementSpeed * turnSpeedMultiplier;
            float clampedMinTurnSpeed = Mathf.Min(minTurnSpeed, CurrentMovementSpeed);
            effectiveMovementSpeed = Mathf.Max(penalizedSpeed, clampedMinTurnSpeed);
        }

        Vector3 movementVector = Vector3.ClampMagnitude(rightMove + forwardMove, 1f) * effectiveMovementSpeed;

        if (IsGrounded)
            moveDirection = movementVector;
        else
            moveDirection = Vector3.Lerp(moveDirection, movementVector, airControlFactor * Time.deltaTime);

        if (IsGrounded)
        {
            if (playerVelocity.y < -2f) playerVelocity.y = -2f;

            if (!moveLocked &&
                jumpInput &&
                !IsCrouching)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -3f * playerGravity);
                OnPlayerJump?.Invoke();
                landingInertiaVelocity = Vector3.zero;
            }
        }

        playerVelocity.y += playerGravity * Time.deltaTime;

        if (characterController.collisionFlags == CollisionFlags.Above && playerVelocity.y > 0)
            playerVelocity.y = 0f;

        Vector3 intendedHorizontalMove = new Vector3(moveDirection.x, 0f, moveDirection.z);
        float intendedHorizontalSpeed = intendedHorizontalMove.magnitude;

        Vector3 combinedVelocity = moveDirection + playerVelocity + landingInertiaVelocity;

        if (characterController.enabled)
            characterController.Move(combinedVelocity * Time.deltaTime);

        lastIntendedVelocity = combinedVelocity;

        if (IsGrounded && intendedHorizontalSpeed > minIntendedSpeedToCheckWall)
        {
            Vector3 actualHorizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float achievedSpeedAlongIntent = Vector3.Dot(actualHorizontalVelocity, intendedHorizontalMove / intendedHorizontalSpeed);
            achievedSpeedAlongIntent = Mathf.Clamp(achievedSpeedAlongIntent, 0f, intendedHorizontalSpeed);

            float achievedRatio = achievedSpeedAlongIntent / intendedHorizontalSpeed;

            if (achievedRatio < wallBlockRatioThreshold && CurrentMovementSpeed > playerWalkSpeed)
            {
                CurrentMovementSpeed = playerWalkSpeed;
                sprintElapsedTime = 0f;
            }
        }

        landingInertiaVelocity = Vector3.Lerp(landingInertiaVelocity, Vector3.zero, landingInertiaDamping * Time.deltaTime);

        CurrentFullSpeed = characterController.velocity.magnitude;
    }

    private void HandleCrouch()
    {
        bool moveLocked = inputState.GetLockState(InputState.LockType.Move);
        bool crouchInput = inputManager.GetInput<bool>("CrouchInput");

        if (!moveLocked && crouchInput)
            wishCrouch = !wishCrouch;

        float checkDistance = startHeight - crouchHeight;
        bool canStand = !Physics.SphereCast(transform.position, characterController.radius,
            Vector3.up, out _, checkDistance, crouchStandLayers, QueryTriggerInteraction.Ignore);

        bool wasCrouching = IsCrouching;

        if (wishCrouch)
        {
            targetHeight = crouchHeight;
            IsCrouching = true;
        }
        else if (canStand)
        {
            targetHeight = startHeight;
            IsCrouching = false;
        }

        if (IsCrouching && !wasCrouching)
            OnPlayerCrouchDown?.Invoke();
        else if (!IsCrouching && wasCrouching)
            OnPlayerCrouchUp?.Invoke();

        characterController.height = Mathf.SmoothDamp(characterController.height, targetHeight,
            ref currentHeightVelocity, crouchTransitionSpeed * Time.deltaTime);
    }
}