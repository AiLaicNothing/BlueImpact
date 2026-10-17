using UnityEngine;

public class NarrativeObject : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private Log_Data _data;
    private bool hasBeenInspected;

    public void Interact()
    {
        if (hasBeenInspected || Inspection_Manager.Instance.IsInspectedObject(_data))
        {
            return;
        }
        else
        {
            Inspection_Manager.Instance.AddInspectedObject(_data);
            Inspection_UI.Instance.OpenInspection(_data);
            Log_Manager.Instance.AddLogToDiscovered(_data);
            hasBeenInspected = true;
        }
    }

    public string GetInteractionText()
    {
        if (_data == null) return "";

        return $"{_data.logName}";
    }
}