using UnityEngine;

[RequireComponent(typeof(PickupPersistence))]
public class Log : MonoBehaviour, IInteractable
{
    [SerializeField] private Log_Data logData;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip onGetLog_Audio;

    private PickupPersistence pickup;

    private void Awake()
    {
        pickup = GetComponent<PickupPersistence>();
    }

    public void Interact()
    {
        if (logData == null || Log_Manager.Instance == null) return;

        if (!pickup.TryCollect()) return;

        Log_Manager.Instance.AddLogToDiscovered(logData);

        float audioDuration = 0f;

        if (audioSource != null && onGetLog_Audio != null)
        {
            audioSource.PlayOneShot(onGetLog_Audio);
            audioDuration = onGetLog_Audio.length;
        }

        pickup.FinishCollect(audioDuration);
    }

    public string GetInteractionText()
    {
        if (pickup.IsCollected) return "";

        if (logData == null) return "Missing Log_Data";

        return $"Log: {logData.logName}";
    }
}
