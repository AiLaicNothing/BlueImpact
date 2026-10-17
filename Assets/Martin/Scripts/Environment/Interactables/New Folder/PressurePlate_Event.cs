using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressurePlate_Event : MonoBehaviour
{
    [SerializeField] private List<UnityEvent> events = new();
    private bool isPressed;

    private void DoEvent()
    {
        foreach (UnityEvent eventToInvoke in events)
        {
            eventToInvoke.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPressed = true;

            DoEvent();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPressed = false;
        }
    }
}
