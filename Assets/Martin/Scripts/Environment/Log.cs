using UnityEngine;

public class Log : MonoBehaviour, IInteractable
{
    [SerializeField] private Log_Data logData;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip onGetLog_Audio;

    public void Interact()
    {
        //If its already discovered nothing happen
        if (Log_Manager.Instance.IsDiscoveredLog(logData))
        {
            return;
        }
        else
        {
            if (audioSource != null && onGetLog_Audio != null)
            {
                audioSource.PlayOneShot(onGetLog_Audio);
            }

            Log_Manager.Instance.AddLogToDiscovered(logData);
        }
    }
    public string GetInteractionText()
    {
        if (logData == null) return "Missing log Data";

        return $"Log: {logData.logName}";
    }

}
