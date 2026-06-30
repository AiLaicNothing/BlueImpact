using UnityEngine;

/// <summary>
/// Lever dedicado a controlar un ElevetorEvent. No usa PuzzleReceiver/IActivator
/// porque no es parte de un puzzle de varios levers: es un interactuable simple
/// que delega toda la lógica de estado (ambush vs movimiento libre) al ElevetorEvent.
/// </summary>
public class ElevatorLever : MonoBehaviour, IInteractable
{
    [Header("Elevator Reference")]
    [SerializeField] private ElevetorEvent elevatorEvent;

    [Header("Visual")]
    [SerializeField] private Animator animator;

    public void Interact()
    {
        if (elevatorEvent == null) return;
        if (elevatorEvent.IsRunning) return;

        animator?.SetTrigger("Pull");

        elevatorEvent.ToggleElevator();
    }

    public string GetInteractionText()
    {
        if (elevatorEvent == null) return null;

        return elevatorEvent.GetNextActionText();
    }

    public bool CanInteract()
    {
        return elevatorEvent != null && !elevatorEvent.IsRunning;
    }
}