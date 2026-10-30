using UnityEngine;

[RequireComponent(typeof(PickupPersistence))]
public class NarrativeObject : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private Log_Data _data;

    [Header("VFX")]
    [SerializeField] private GameObject _vfx;

    private PickupPersistence pickup;

    private void Awake()
    {
        pickup = GetComponent<PickupPersistence>();
    }

    public void Interact()
    {
        if (_data == null || Inspection_Manager.Instance == null ||
            Log_Manager.Instance == null) return;

        if (pickup.IsCollected) return;

        // Handle objects already recorded as inspected.
        if (Inspection_Manager.Instance.IsInspectedObject(_data))
        {
            if (pickup.TryCollect()) pickup.FinishCollect();

            return;
        }

        if (Inspection_UI.Instance == null) return;

        if (!pickup.TryCollect()) return;

        Inspection_Manager.Instance.AddInspectedObject(_data);

        Log_Manager.Instance.AddLogToDiscovered(_data);

        Inspection_UI.Instance.OpenInspection(_data);

        if (_vfx != null) _vfx.SetActive(false);

        pickup.FinishCollect();
    }

    public string GetInteractionText()
    {
        if (pickup.IsCollected) return "";

        if (_data == null) return "";

        return _data.logName;
    }
}