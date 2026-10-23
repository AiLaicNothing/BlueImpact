using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ItemRequirementEvent : MonoBehaviour, IInteractable
{
    [Header("Required Items")]
    [SerializeField] private List<Item_Data> requiredItems;

    [Header("Interaction text")]
    [SerializeField] private string eventName;
    [SerializeField] private string missingItem;

    [Header("Events to invoke")]
    [SerializeField] private List<UnityEvent> events = new();

    private bool hasActivated;

    private bool CheckIfHasItems()
    {
        if (Inventory_Manager.Instance == null) return false;

        var inventory = Inventory_Manager.Instance.listOfItemInInventory;

        foreach (Item_Data item in requiredItems)
        {
            if (item == null || !inventory.Contains(item))
            {
                return false;
            }
        }

        return true;
    }

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
        if (hasActivated) return;

        //If has items
        if (CheckIfHasItems())
        {
            //Remove the items required
            foreach (Item_Data item in requiredItems)
            {
                Inventory_Manager.Instance.RemoveItem(item);
            }

            //Do the event
            DoEvent();

            GetInteractionText();
        }
        else
        {
            return;
        }

    }
    public string GetInteractionText()
    {
        if (hasActivated)
        {
            return "";
        }
        else
        {
            if (CheckIfHasItems())
            {
                return eventName;
            }
            else
            {
                return missingItem;
            }
        }
    }
}
