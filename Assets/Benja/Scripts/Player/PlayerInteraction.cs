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

    private void Update()
    {
        FindClosestInteractable();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("Interact presionado");

        if (!context.performed)
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
}