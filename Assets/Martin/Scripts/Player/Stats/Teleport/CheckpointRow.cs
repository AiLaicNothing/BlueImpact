using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointRow : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image _previewImage;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI locationName;

    private DiscoveredCheckpoint _checkpoint;
    private Travel_Panel travel_Panel;

    private bool isCurrent;
    private bool panelInteractable = true;

    public void Initialize(
        DiscoveredCheckpoint checkpoint,
        Travel_Panel panel,
        bool isCurrent)
    {
        _checkpoint = checkpoint;
        travel_Panel = panel;
        this.isCurrent = isCurrent;

        locationName.text = checkpoint._data.checkpointName;
        _previewImage.sprite = checkpoint._data.previewImage;

        stateText.text = isCurrent ? "Ya está aquí" : "";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);

        RefreshInteractable();
    }

    private void OnClick()
    {
        if (isCurrent || !panelInteractable || _checkpoint == null) return;

        travel_Panel.SelectCheckpoint(_checkpoint);
    }

    public void SetInteractable(bool interactable)
    {
        panelInteractable = interactable;
        RefreshInteractable();
    }

    private void RefreshInteractable()
    {
        if (selectButton != null) selectButton.interactable = panelInteractable && !isCurrent;
    }

    public void SimulateClick()
    {
        if (selectButton.interactable) OnClick();
    }

    public DiscoveredCheckpoint GetCheckpoint() => _checkpoint;

    public string GetCheckpointName() =>
    _checkpoint != null && _checkpoint._data != null ? _checkpoint._data.checkpointName : string.Empty;
}