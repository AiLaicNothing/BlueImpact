using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ItemRequirementEvent : MonoBehaviour, IInteractable
{
    [Header("Save System")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Required Items")]
    [SerializeField] private List<Item_Data> requiredItems = new();

    [Header("Interaction Text")]
    [SerializeField] private string eventName;
    [SerializeField] private string missingItem;

    [Header("VFX")]
    [SerializeField] private GameObject _vfx;

    [Header("Events to Invoke")]
    [SerializeField] private List<UnityEvent> events = new();

    [Header("Restore Completed State")]
    [SerializeField] private UnityEvent onRestoreCompleted;

    private bool hasActivated;

    private void Start()
    {
        RestoreEventState();
    }

    private void RestoreEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[ItemRequirementEvent] Missing Event_Manager or WorldEvent_Data.", this);
            return;
        }

        hasActivated = Event_Manager.Instance.IsCompleted(eventData);

        if (hasActivated)
        {
            if (_vfx != null) _vfx.SetActive(false);

            // Restore the state of affected world objects.
            onRestoreCompleted?.Invoke();
        }
    }

    private bool CheckIfHasItems()
    {
        if (Inventory_Manager.Instance == null) return false;

        var inventory = Inventory_Manager.Instance.listOfItemInInventory;

        foreach (Item_Data item in requiredItems)
        {
            if (item == null || !inventory.Contains(item))  return false;
        }

        return true;
    }

    public void Interact()
    {
        if (hasActivated) return;

        // Don't activate an event that cannot be saved.
        if (Event_Manager.Instance == null || eventData == null) return;

        if (!CheckIfHasItems()) return;

        // Consume the required items.
        foreach (Item_Data item in requiredItems)
        {
            Inventory_Manager.Instance.RemoveItem(item);
        }

        DoEvent();
    }

    private void DoEvent()
    {
        hasActivated = true;

        // Save before invoking other world events.
        Event_Manager.Instance.SaveState(eventData, true);

        if (_vfx != null) _vfx.SetActive(false);

        InteractionUI.Instance?.Clear();

        foreach (UnityEvent eventToInvoke in events)
        {
            eventToInvoke?.Invoke();
        }
    }

    public string GetInteractionText()
    {
        if (hasActivated) return "";

        return CheckIfHasItems() ? eventName : missingItem;
    }
}