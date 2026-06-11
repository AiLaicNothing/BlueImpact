using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatEntryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image statIcon;

    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private TMP_Text changeValueText;
    [SerializeField] private TMP_Text finalValueText;

    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;

    [SerializeField] private TMP_Text blockedReasonText;

    private StatDefinition stat;
    private CheckpointStatsPanel panel;

    public void Initialize(
        StatDefinition stat,
        CheckpointStatsPanel panel)
    {
        this.stat = stat;
        this.panel = panel;

        statIcon.sprite = stat.icon;
        statNameText.text = stat.statName;

        increaseButton.onClick.RemoveAllListeners();
        increaseButton.onClick.AddListener(OnIncreaseClicked);

        decreaseButton.onClick.RemoveAllListeners();
        decreaseButton.onClick.AddListener(OnDecreaseClicked);

        Refresh();
    }

    public void Refresh()
    {
        StatsModificationSession session = panel.Session;

        int current = session.GetCurrentValue(stat);
        int change = session.GetChange(stat);
        int finalValue = session.GetFinalValue(stat);

        currentValueText.text = current.ToString();

        changeValueText.text =
            change > 0 ? $"+{change}" :
            change.ToString();

        finalValueText.text = finalValue.ToString();

        decreaseButton.interactable =
            session.CanUndo(stat);

        bool canIncrease =
            session.CanIncrease(stat, out string reason);

        increaseButton.interactable = canIncrease;

        blockedReasonText.text =
            canIncrease ? "" : reason;
    }

    private void OnIncreaseClicked()
    {
        panel.TryIncrease(stat);
    }

    private void OnDecreaseClicked()
    {
        panel.TryDecrease(stat);
    }
}