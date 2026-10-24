using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TutorialPopUp : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Tutorial")]
    [SerializeField] private List<PopUpPage> pages = new();

    private bool hasActivated;
    private UIPopUp ui;

    private void Awake()
    {
        ui = FindAnyObjectByType<UIPopUp>();
    }

    private void Start()
    {
        RestoreEventState();
    }

    // =========================
    // SAVE SYSTEM
    // =========================

    private void RestoreEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[TutorialPopUp] Missing Event_Manager or WorldEvent_Data.", this);
            return;
        }

        hasActivated = Event_Manager.Instance.IsCompleted(eventData);
    }

    private void SaveEventState()
    {
        if (Event_Manager.Instance == null || eventData == null) return;

        Event_Manager.Instance.SaveState(eventData, true);
    }

    // =========================
    // INTERACTION
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;

        if (!other.CompareTag("Player")) return;

        // The popup UI may have been created after Awake.
        if (ui == null) ui = FindAnyObjectByType<UIPopUp>();

        if (ui == null)
        {
            Debug.LogWarning("[TutorialPopUp] UIPopUp not found.", this);
            return;
        }

        // Activate the tutorial.
        ui.ShowPopUp(pages);

        hasActivated = true;

        // Save so it doesn't activate again.
        SaveEventState();
    }
}

