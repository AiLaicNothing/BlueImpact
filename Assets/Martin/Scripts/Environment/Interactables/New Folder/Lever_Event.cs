using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Lever_Event : MonoBehaviour, IInteractable
{
    [Header("Events")]
    [SerializeField] private List<UnityEvent> events = new();
    [SerializeField] private string interactionText;
    [SerializeField] private bool oneTimeEvent;
    private bool hasActivated;

    private void DoEvent()
    {
        hasActivated = true;

        foreach (UnityEvent eventToInvoke in events)
        {
            eventToInvoke.Invoke();
        }
    }

    public void Interact()
    {
        if (oneTimeEvent)
        {
            if (hasActivated) return;
        }

        //Do all events
        DoEvent();

        GetInteractionText();
    }
    public string GetInteractionText()
    {
        if (oneTimeEvent)
        {
            if (hasActivated) return "";
        }

        return $"{interactionText}";
    }
}
