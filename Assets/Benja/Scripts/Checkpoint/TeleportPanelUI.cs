using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeleportPanelUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform content;

    [SerializeField] private CheckpointEntryUI entryPrefab;

    [Header("Preview")]
    [SerializeField] private Image previewImage;

    [SerializeField] private TMP_Text checkpointName;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;

    [SerializeField] private Button backButton;

    private readonly List<CheckpointEntryUI> entries =
        new();

    private Checkpoint selectedCheckpoint;

    private void Awake()
    {
        travelButton.onClick.AddListener(Travel);

        backButton.onClick.AddListener(Back);
    }

    private void Back()
    {
        CheckpointMenuUI.Instance.ShowMainPanel();
    }

    public void Open()
    {
        RefreshList();

        selectedCheckpoint = null;

        previewImage.sprite = null;

        checkpointName.text =
            "Selecciona un destino";

        travelButton.interactable = false;
    }

    public void SelectCheckpoint(
        Checkpoint checkpoint)
    {
        selectedCheckpoint = checkpoint;

        previewImage.sprite =
            checkpoint.Data.previewImage;

        checkpointName.text =
            checkpoint.Data.checkpointName;

        travelButton.interactable = true;
    }

    private void RefreshList()
    {
        Clear();

        Checkpoint current =
            CheckpointManager.Instance.GetActiveCheckpoint();

        foreach (Checkpoint checkpoint in
                 CheckpointManager.Instance.GetDiscoveredCheckpoints())
        {
            CheckpointEntryUI entry =
                Instantiate(
                    entryPrefab,
                    content);

            bool isCurrent =
                checkpoint == current;

            entry.Initialize(
                checkpoint,
                this,
                isCurrent);

            entries.Add(entry);
        }
    }

    private void Travel()
    {
        if (selectedCheckpoint == null)
            return;

        TeleportConfirmPopup.Instance.Open(
            selectedCheckpoint.Data.checkpointName,
            ConfirmTravel);
    }

    private void ConfirmTravel()
    {
        TeleportManager.Instance.Teleport(
            selectedCheckpoint);
    }

    private void Clear()
    {
        foreach (CheckpointEntryUI entry in entries)
        {
            Destroy(entry.gameObject);
        }

        entries.Clear();
    }
}