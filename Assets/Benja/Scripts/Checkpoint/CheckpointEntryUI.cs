using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointEntryUI : MonoBehaviour
{
    [SerializeField] private Button button;

    [SerializeField] private Image previewImage;

    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text checkpointName;

    private Checkpoint checkpoint;

    private TeleportPanelUI panel;
    public void Initialize(
    Checkpoint checkpoint,
    TeleportPanelUI panel,
    bool isCurrent)
    {
        this.checkpoint = checkpoint;
        this.panel = panel;

        checkpointName.text =
            checkpoint.Data.checkpointName;

        previewImage.sprite =
            checkpoint.Data.previewImage;

        button.onClick.RemoveAllListeners();

        if (isCurrent)
        {
            button.interactable = false;

            stateText.text = "Ya estás aquí";
        }
        else
        {
            button.interactable = true;

            stateText.text = "";

            button.onClick.AddListener(OnClick);
        }
    }
    //public void Initialize(
    //    Checkpoint checkpoint,
    //    TeleportPanelUI panel)
    //{
    //    this.checkpoint = checkpoint;

    //    this.panel = panel;

    //    checkpointName.text =
    //        checkpoint.Data.checkpointName;

    //    previewImage.sprite =
    //        checkpoint.Data.previewImage;

    //    button.onClick.RemoveAllListeners();

    //    button.onClick.AddListener(OnClick);
    //}

    private void OnClick()
    {
        panel.SelectCheckpoint(checkpoint);
    }
}