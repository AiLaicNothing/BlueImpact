using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatRow : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image statIcon;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI currentValueText;
    [SerializeField] private TextMeshProUGUI changeValueText;
    [SerializeField] private TextMeshProUGUI finalValueText;

    [Header("Buttons")]
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;

    private StatsType _statType;
    private EventSystem _eventSystem;

    private bool wasIncreaseButtonInteractable = true;

    public void Initialize(StatsType statType)
    {
        _statType = statType;
        _eventSystem = EventSystem.current;

        statsText.text = statType.ToString();

        CharacterStatsManager.Instance.OnStatsChanged += Refresh;

        increaseButton.onClick.RemoveAllListeners();
        increaseButton.onClick.AddListener(() => OnIncrease());

        decreaseButton.onClick.RemoveAllListeners();
        decreaseButton.onClick.AddListener(() => OnDecrease());

        Refresh();
    }

    private void OnDestroy()
    {
        CharacterStatsManager.Instance.OnStatsChanged -= Refresh;
    }

    public void Refresh()
    {
        var statsManager = CharacterStatsManager.Instance;

        if (statsManager == null) return;

        int currentValue = statsManager.GetCurrentStat(_statType);

        int pendingValue = statsManager.GetPendingPoints(_statType);
        int allocatedValue = statsManager.GetAllocatedPoints(_statType);
        int changeValue = pendingValue - allocatedValue;

        int resultValue = statsManager.GetDisplayedStat(_statType);

        currentValueText.text = currentValue.ToString();

        changeValueText.text = changeValue > 0? $"+{changeValue}" : changeValue.ToString();
        finalValueText.text = resultValue.ToString();

        bool canIncrease = statsManager.AvailablePoints > 0;
        bool wasInteractable = increaseButton.interactable;

        increaseButton.interactable = canIncrease;

        //This is call when the button can be interacted but it cant increase more
        if (wasInteractable && !canIncrease && _eventSystem != null) CheckIfSelectedAndSwitchFocus();
    }

    private void CheckIfSelectedAndSwitchFocus()
    {
        GameObject selectedObj = _eventSystem.currentSelectedGameObject;

        //If button is selected
        if (selectedObj == increaseButton.gameObject)
        {
            //Chabge to deacrease button, else disable both of them
            if (decreaseButton.interactable)
            {
                _eventSystem.SetSelectedGameObject(null);
                _eventSystem.SetSelectedGameObject(decreaseButton.gameObject);
            }
            else
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }
    }

    private void OnIncrease()
    {
        CharacterStatsManager.Instance.AddPoint(_statType);
    }

    private void OnDecrease()
    {
        CharacterStatsManager.Instance.RemovePoint(_statType);
    }

    #region Gamepad
    public StatsType GetStatDefinition() => _statType;
    public Button GetDecreaseButton() => decreaseButton;
    public Button GetIncreaseButton() => increaseButton;

    #endregion
}