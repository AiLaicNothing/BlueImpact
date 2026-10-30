using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PickupPersistence))]
public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private Item_Data data;

    [Header("VFX")]
    [SerializeField] private GameObject _vfx;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickUpAudio;

    private PickupPersistence pickup;

    private void Awake()
    {
        pickup = GetComponent<PickupPersistence>();
    }

    public void Interact()
    {
        if (data == null || Inventory_Manager.Instance == null) return;

        if (pickup.IsCollected) return;

        // only one instance of this Item_Data in inventory.
        if (Inventory_Manager.Instance.listOfItemInInventory.Contains(data)) return;

        // Record this particular world pickup.
        if (!pickup.TryCollect()) return;

        // Give the player the item.
        Inventory_Manager.Instance.AddItem(data);

        float audioDuration = 0f;

        if (audioSource != null && pickUpAudio != null)
        {
            audioSource.PlayOneShot(pickUpAudio);
            audioDuration = pickUpAudio.length;
        }

        if (_vfx != null) _vfx.SetActive(false);

        pickup.FinishCollect(audioDuration);
    }

    public string GetInteractionText()
    {
        if (pickup.IsCollected) return "";

        if (data == null) return "Missing Item_Data";

        return data.itemName;
    }
}
