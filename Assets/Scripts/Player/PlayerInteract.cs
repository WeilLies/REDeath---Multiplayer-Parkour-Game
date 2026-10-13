using System;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform castTransform;
    [SerializeField] private float maxDistance;
    [SerializeField] private Vector3 direction = Vector3.forward;
    [SerializeField] private LayerMask layer;
    public bool OnInteractableCollider { get; private set; }
    public IInteractable LastTarget { get; private set; }

    public event Action<bool> OnInteracted;

    private IInputState inputState;

    private void Awake()
    {
        inputState = ServiceLocator.Get<IInputState>();
    }

    private void Update()
    {
        CheckForInteractable();

        if (!inputState.GetLockState(InputState.LockType.Camera) || !inputState.GetLockState(InputState.LockType.Cursor)) return;

        LastTarget?.Interact(playerObject);
    }

    private void CheckForInteractable()
    {
        bool hitDetected = Physics.Raycast(
            castTransform.position,
            castTransform.TransformDirection(direction),
            out RaycastHit hit,
            maxDistance,
            layer,
            QueryTriggerInteraction.Ignore
        );

        IInteractable newTarget = null;
        if (hitDetected && hit.collider != null)
            hit.collider.TryGetComponent(out newTarget);

        UpdateInteractionState(newTarget);
    }

    private void UpdateInteractionState(IInteractable newTarget)
    {
        if (LastTarget != newTarget)
        {
            LastTarget?.OnInteractedEnd(playerObject);
            LastTarget = newTarget;
            LastTarget?.OnInteractedStart(playerObject);
        }

        bool isInteracting = newTarget != null;
        if (isInteracting != OnInteractableCollider)
        {
            OnInteractableCollider = isInteracting;
            OnInteracted?.Invoke(OnInteractableCollider);
        }
    }
}

public interface IInteractable : IInteract, IInteractedStart, IInteractedEnd { }

public interface IInteract
{
    public void Interact(GameObject player = null);
}

public interface IInteractedStart
{
    public void OnInteractedStart(GameObject player = null);
}

public interface IInteractedEnd
{
    public void OnInteractedEnd(GameObject player = null);
}
