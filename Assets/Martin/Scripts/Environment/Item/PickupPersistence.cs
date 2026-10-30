using UnityEngine;

[DisallowMultipleComponent]
public class PickupPersistence : MonoBehaviour
{
    [Header("Persistence")]
    [SerializeField] private Pickup_Data pickupData;

    private bool consumed;

    public bool IsCollected => consumed || (Pickup_Manager.Instance != null && Pickup_Manager.Instance.IsCollected(pickupData));

    private void Start()
    {
        if (pickupData == null || string.IsNullOrWhiteSpace(pickupData.PickupID))
        {
            Debug.LogWarning("[PickupPersistence] Missing valid Pickup_Data.", this);
            return;
        }

        if (IsCollected) Destroy(gameObject);
    }

    public bool TryCollect()
    {
        if (consumed || Pickup_Manager.Instance == null || pickupData == null) return false;

        if (!Pickup_Manager.Instance.MarkCollected(pickupData)) return false;

        consumed = true;
        return true;
    }

    public void FinishCollect(float destroyDelay = 0f)
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        InteractionUI.Instance?.Clear();

        Destroy(gameObject, Mathf.Max(0f, destroyDelay));
    }
}
