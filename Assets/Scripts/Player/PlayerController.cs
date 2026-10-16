using System;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Walk,
    Run,
    Fall,
    Crouch,
    Slide
}

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
    [SerializeField] private float walkSpeedReachTolerance = 0.05f;

    [Header("Turning Settings")]
    [SerializeField] private float turnSensitivity = 60f;
    [SerializeField] private float turnSlowdownIntensity = 0.01f;
    [SerializeField] private float turnMultiplierChangeSpeed = 2f;

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

    [Header("Player State Settings")]
    [SerializeField] private float movementStateThreshold = 0.1f;

    [Header("Slide Settings")]
    [SerializeField] private float minSpeedStartSlide = 3f;
    [SerializeField] private float minSpeedEndSlide = 2f;
    [SerializeField] private float slideSpeedLoseIntensity = 1f;
    [SerializeField] private float slideHeight = 0.5f;

    [Header("Slope Settings")]
    [SerializeField] private float groundNormalCheckDistance = 0.4f;
    [SerializeField] private float slopeStickForce = 8f;
    [SerializeField] private LayerMask groundCheckLayers = ~0;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    private float startHeight;
    private float targetHeight;

    private CharacterController characterController;

    private Vector3 playerVelocity;
    private Vector2 smoothInputVelocity;
    private Vector3 moveDirection;
    private Vector3 landingInertiaVelocity;

    private bool wishCrouch;
    private bool wishSlide;
    private float currentHeightVelocity;
    private float sprintElapsedTime;

    private float previousYaw;
    private float turnSpeedMultiplier = 1f;

    private Vector3 lastIntendedVelocity;
    private Vector3 currentGroundNormal = Vector3.up;

    public Vector2 CurrentInputVector { get; private set; }
    public Vector2 CurrentInputVectorInt { get; private set; }
    public float CurrentMovementSpeed { get; private set; }
    public float CurrentFullSpeed { get; private set; }
    public Vector2 MoveVector { get; private set; }
    public float TurnSpeedMultiplier => turnSpeedMultiplier;
    public bool IsSprinting { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsSliding { get; private set; }

    private float ungroundedTime;

    public event Action OnPlayerJump;
    public event Action<float> OnPlayerFallDown;
    public event Action OnPlayerCrouchUp;
    public event Action OnPlayerCrouchDown;
    public event Action OnPlayerStateChanged;
    public event Action OnPlayerStartSlide;
    public event Action OnPlayerEndSlide;

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
        UpdatePlayerState();
    }

    private void HandleTurning()
    {
        float currentYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(previousYaw, currentYaw));
        previousYaw = currentYaw;

        float angularSpeed = Time.deltaTime > 0f ? yawDelta / Time.deltaTime : 0f;

        float excessSpeed = Mathf.Max(0f, angularSpeed - turnSensitivity);

        float targetMultiplier = Mathf.Clamp01(1f - excessSpeed * turnSlowdownIntensity);

        turnSpeedMultiplier = Mathf.MoveTowards(turnSpeedMultiplier, targetMultiplier, Time.deltaTime * turnMultiplierChangeSpeed);
    }

    private void GroundCheck()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = characterController.isGrounded;

        if (IsGrounded)
        {
            Vector3 origin = transform.position + Vector3.up * (characterController.radius + 0.05f);
            if (Physics.SphereCast(origin, characterController.radius * 0.9f, Vector3.down,
                out RaycastHit hit, groundNormalCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                currentGroundNormal = hit.normal;
            }
            else
            {
                currentGroundNormal = Vector3.up;
            }
        }
        else
        {
            currentGroundNormal = Vector3.up;
        }

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

        bool hasReachedWalkSpeed = CurrentMovementSpeed >= playerWalkSpeed - walkSpeedReachTolerance;

        if (IsSprinting && hasReachedWalkSpeed)
            sprintElapsedTime = Mathf.Min(sprintElapsedTime + Time.deltaTime, runAccelerationDuration);
        else if (!IsSprinting)
            sprintElapsedTime = 0f;

        CurrentInputVector = Vector2.SmoothDamp(CurrentInputVector, MoveVector, ref smoothInputVelocity, smoothInputSpeed);
        CurrentInputVectorInt = new Vector2(Mathf.Round(CurrentInputVector.x), Mathf.Round(CurrentInputVector.y));

        Vector3 rightMove = transform.right * CurrentInputVector.x;
        Vector3 forwardMove = transform.forward * CurrentInputVector.y;

        float targetSpeed;

        if (IsSliding)
        {
            targetSpeed = CurrentMovementSpeed;
        }
        else if (IsCrouching)
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

        if (IsSliding)
            CurrentMovementSpeed = targetSpeed;
        else
            CurrentMovementSpeed = Mathf.Lerp(CurrentMovementSpeed, targetSpeed, Time.deltaTime * smoothChangeSpeedValue);

        float effectiveMovementSpeed = CurrentMovementSpeed;

        if (IsGrounded)
            effectiveMovementSpeed = CurrentMovementSpeed * turnSpeedMultiplier;

        Vector3 moveDirectionInput = IsSliding
            ? transform.forward
            : Vector3.ClampMagnitude(rightMove + forwardMove, 1f);

        Vector3 movementVector = moveDirectionInput * effectiveMovementSpeed;

        if (IsGrounded)
        {
            Vector3 projected = Vector3.ProjectOnPlane(movementVector, currentGroundNormal);
            if (projected.sqrMagnitude > 0.0001f)
                movementVector = projected.normalized * movementVector.magnitude;
        }

        if (IsGrounded)
            moveDirection = movementVector;
        else
            moveDirection = Vector3.Lerp(moveDirection, movementVector, airControlFactor * Time.deltaTime);

        if (IsGrounded)
        {
            float slopeDot = Vector3.Dot(currentGroundNormal, Vector3.up);
            float slopeFactor = 1f - Mathf.Clamp01(slopeDot);
            float stickVelocity = -2f - slopeStickForce * slopeFactor;

            if (playerVelocity.y < 0f)
                playerVelocity.y = stickVelocity;

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

            if (achievedRatio < wallBlockRatioThreshold && CurrentMovementSpeed > playerWalkSpeed && !IsSliding)
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
        float crouchInput = inputManager.GetInput<float>("CrouchInput");

        bool isCrouchHeld = crouchInput > 0.1f;

        bool slideRequirements = MoveVector.y > 0f && CurrentFullSpeed >= minSpeedStartSlide;

        wishCrouch = !moveLocked && IsGrounded && isCrouchHeld && !slideRequirements && !IsSliding;
        wishSlide = !moveLocked && IsGrounded && isCrouchHeld && (slideRequirements || IsSliding);

        if (wishCrouch)
        {
            StartCrouch();
        }
        else if (wishSlide)
        {
            StartSlide();
        }

        if (!wishCrouch)
        {
            EndCrouch();
        }

        if (!wishSlide)
        {
            EndSlide();
        }

        characterController.height = Mathf.SmoothDamp(characterController.height, targetHeight,
        ref currentHeightVelocity, crouchTransitionSpeed * Time.deltaTime);
    }

    private void StartCrouch()
    {
        if (!IsCrouching && !IsSliding)
        {
            targetHeight = crouchHeight;
            IsCrouching = true;
            OnPlayerCrouchDown?.Invoke();
        }
    }
    private void EndCrouch()
    {
        if (IsCrouching)
        {
            float checkDistance = startHeight - crouchHeight;
            bool canStand = !Physics.SphereCast(transform.position, characterController.radius,
                Vector3.up, out _, checkDistance, crouchStandLayers, QueryTriggerInteraction.Ignore);

            if (canStand)
            {
                targetHeight = startHeight;
                IsCrouching = false;
                OnPlayerCrouchUp?.Invoke();
            }
        }
    }

    private void StartSlide()
    {
        if (!IsSliding && !IsCrouching)
        {
            IsSliding = true;
            targetHeight = slideHeight;
            CurrentMovementSpeed = Mathf.Max(CurrentFullSpeed, minSpeedStartSlide);
            OnPlayerStartSlide?.Invoke();
        }

        if (IsSliding)
        {
            CurrentMovementSpeed = Mathf.Lerp(CurrentMovementSpeed, minSpeedEndSlide - 0.1f, slideSpeedLoseIntensity * Time.deltaTime);

            if (CurrentMovementSpeed <= minSpeedEndSlide)
            {
                CurrentMovementSpeed = minSpeedEndSlide;
                EndSlide();
            }
        }
    }

    private void EndSlide()
    {
        if (IsSliding)
        {
            IsSliding = false;

            float checkDistance = startHeight - slideHeight;
            bool canStand = !Physics.SphereCast(transform.position, characterController.radius,
                Vector3.up, out _, checkDistance, crouchStandLayers, QueryTriggerInteraction.Ignore);

            targetHeight = canStand ? startHeight : crouchHeight;
            IsCrouching = !canStand;

            OnPlayerEndSlide?.Invoke();
        }
    }

    private void UpdatePlayerState()
    {
        PlayerState newState;
        float horizontalSpeed = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
        bool isMoving = horizontalSpeed > movementStateThreshold || CurrentInputVector.sqrMagnitude > movementStateThreshold * movementStateThreshold;
        if (!IsGrounded)
        {
            newState = PlayerState.Fall;
        }
        else if (IsSliding)
        {
            newState = PlayerState.Slide;
        }
        else if (IsCrouching)
        {
            newState = PlayerState.Crouch;
        }
        else if (IsSprinting && isMoving)
        {
            newState = PlayerState.Run;
        }
        else if (isMoving)
        {
            newState = PlayerState.Walk;
        }
        else
        {
            newState = PlayerState.Idle;
        }
        if (newState != CurrentState)
        {
            PlayerState previousState = CurrentState;
            CurrentState = newState;
            OnPlayerStateChanged?.Invoke();
        }
    }
}