using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField]
    private float interactionRadius = 2f;

    [SerializeField]
    private LayerMask interactableLayer;

    private IInteractable currentInteractable;

    private IInteractable previousInteractable;

    private bool interactionLocked;

    private void Update()
    {
        if (interactionLocked)
            return;

        FindClosestInteractable();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (interactionLocked)
            return;

        currentInteractable?.Interact();
    }

    private void FindClosestInteractable()
    {
        currentInteractable = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                interactionRadius,
                interactableLayer);

        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            MonoBehaviour behaviour =
                hit.GetComponent<MonoBehaviour>();

            if (behaviour is not IInteractable interactable)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
            }
        }

        if (previousInteractable != currentInteractable)
        {
            previousInteractable = currentInteractable;

            InteractionUI.Instance?.SetInteractable(
                currentInteractable);
        }
    }
    private void OnEnable()
    {
        GameModeManager.Instance.OnGameModeChanged += HandleGameModeChanged;
    }

    private void OnDisable()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged -= HandleGameModeChanged;
        }
    }

    private void HandleGameModeChanged(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Gameplay:
                UnlockInteraction();
                break;

            default:
                LockInteraction();
                break;
        }
    }
    public void LockInteraction()
    {
        interactionLocked = true;

        currentInteractable = null;
        previousInteractable = null;

        InteractionUI.Instance?.Clear();
    }

    public void UnlockInteraction()
    {
        interactionLocked = false;

        FindClosestInteractable();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius);
    }
#endif
}