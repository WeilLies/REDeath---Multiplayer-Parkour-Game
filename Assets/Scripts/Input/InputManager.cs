using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IInputManager, IAsyncInitializable
{
    public InputActionAsset playerControls;

    #region Player Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction crouchAction;
    private InputAction fireAction;
    private InputAction pauseAction;
    #endregion

    #region Player Input
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpInput { get; private set; }
    public float SprintInput { get; private set; }
    public bool InteractInput { get; private set; }
    public bool CrouchInput { get; private set; }
    public float FireInput { get; private set; }
    public bool PauseInput { get; private set; }
    #endregion

    private Dictionary<string, Func<object>> _inputLookup;

    private bool isLoaded;

    private void Awake()
    {
        InitActions();
        EnableActions();
        RegisterPlayerInputActions();
        BuildLookup();

        isLoaded = true;
    }

    public IEnumerator InitializeAsync()
    {
        yield return new WaitUntil(() => isLoaded);
    }

    private void OnDestroy()
    {
        DisableActions();
    }

    private void Update()
    {
        UpdatePlayerInput();
    }

    private void InitActions()
    {
        InputActionMap playerMap = playerControls.FindActionMap("Player");

        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");
        jumpAction = playerMap.FindAction("Jump");
        sprintAction = playerMap.FindAction("Sprint");
        interactAction = playerMap.FindAction("Interact");
        crouchAction = playerMap.FindAction("Crouch");
        fireAction = playerMap.FindAction("Fire");
        pauseAction = playerMap.FindAction("Open Menu");
    }

    private void EnableActions()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        interactAction.Enable();
        crouchAction.Enable();
        fireAction.Enable();
        pauseAction.Enable();
    }

    private void DisableActions()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        interactAction.Disable();
        crouchAction.Disable();
        fireAction.Disable();
        pauseAction.Disable();
    }

    private void UpdatePlayerInput()
    {
        JumpInput = jumpAction.triggered;
        InteractInput = interactAction.triggered;
        CrouchInput = crouchAction.triggered;
        PauseInput = pauseAction.triggered;
    }

    private void RegisterPlayerInputActions()
    {
        moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => MoveInput = Vector2.zero;

        lookAction.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => LookInput = Vector2.zero;

        sprintAction.performed += ctx => SprintInput = ctx.ReadValue<float>();
        sprintAction.canceled += ctx => SprintInput = 0f;

        fireAction.performed += ctx => FireInput = ctx.ReadValue<float>();
        fireAction.canceled += ctx => FireInput = 0f;
    }

    private void BuildLookup()
    {
        _inputLookup = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(MoveInput),      () => MoveInput      },
            { nameof(LookInput),      () => LookInput      },
            { nameof(JumpInput),      () => JumpInput      },
            { nameof(SprintInput),    () => SprintInput    },
            { nameof(InteractInput),  () => InteractInput  },
            { nameof(CrouchInput),    () => CrouchInput    },
            { nameof(FireInput),      () => FireInput      },
            { nameof(PauseInput),     () => PauseInput     },
        };
    }

    public T GetInput<T>(string inputName)
    {
        if (_inputLookup.TryGetValue(inputName, out Func<object> getter) && getter() is T value)
            return value;

        return default;
    }

    public void ChangeActionMap(string actionMap)
    {
        foreach (InputActionMap map in playerControls.actionMaps)
        {
            if (map.name == "UI") continue;

            if (map.name == actionMap)
                map.Enable();
            else
                map.Disable();
        }
    }
}
