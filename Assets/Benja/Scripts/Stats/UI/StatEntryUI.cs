using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    private EventSystem eventSystem;

    // ✅ RASTREAR ESTADO ANTERIOR
    private bool wasIncreaseButtonInteractable = true;

    public void Initialize(
        StatDefinition stat,
        CheckpointStatsPanel panel)
    {
        this.stat = stat;
        this.panel = panel;
        this.eventSystem = EventSystem.current;

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

        // ✅ GUARDAR ESTADO ANTERIOR
        bool wasInteractable = increaseButton.interactable;
        increaseButton.interactable = canIncrease;

        // ✅ SI SE DESHABILITA Y ESTÁ SELECCIONADO, CAMBIAR SELECCIÓN
        if (wasInteractable && !canIncrease && eventSystem != null)
        {
            CheckIfSelectedAndSwitchFocus();
        }

        blockedReasonText.text =
            canIncrease ? "" : reason;
    }

    // ✅ CAMBIAR FOCO SI EL BOTÓN DESHABILITADO ESTABA SELECCIONADO
    private void CheckIfSelectedAndSwitchFocus()
    {
        GameObject selectedObj = eventSystem.currentSelectedGameObject;

        // Si increaseButton estaba seleccionado
        if (selectedObj == increaseButton.gameObject)
        {
            // Intentar cambiar a decreaseButton si está habilitado
            if (decreaseButton.interactable)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.SetSelectedGameObject(decreaseButton.gameObject);
                Debug.Log($"[StatEntryUI] {stat.statName}: Selección cambiada de Increase a Decrease");
            }
            else
            {
                // Si ambos están deshabilitados, deseleccionar
                eventSystem.SetSelectedGameObject(null);
                Debug.Log($"[StatEntryUI] {stat.statName}: Ambos botones deshabilitados, deseleccionando");
            }
        }
    }

    private void OnIncreaseClicked()
    {
        panel.TryIncrease(stat);
    }

    private void OnDecreaseClicked()
    {
        panel.TryDecrease(stat);
    }

    // ✅ GETTERS PARA GAMEPAD NAVIGATION
    public StatDefinition GetStatDefinition() => stat;
    public Button GetDecreaseButton() => decreaseButton;
    public Button GetIncreaseButton() => increaseButton;
}