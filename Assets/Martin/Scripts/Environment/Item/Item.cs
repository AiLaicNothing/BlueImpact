using System.Linq;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private Item_Data data;
    private bool hasBeenPickUp;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickUpAudio;

    public void Interact()
    {
        if (!hasBeenPickUp)
        {
            if (Inventory_Manager.Instance == null) return;

            //If this item exist already on the inventory return
            if (Inventory_Manager.Instance.listOfItemInInventory.Contains(data)) return;

            Inventory_Manager.Instance.AddItem(data);

            hasBeenPickUp = true;

            GetInteractionText();
        }
        else
        {
            return;
        }
    }
    public string GetInteractionText()
    {
        if (hasBeenPickUp) return "";

        if (data == null) return "Missing Item_Data";

        return $"{data.itemName}";
    }

}
