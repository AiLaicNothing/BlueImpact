using UnityEngine;
using UnityEngine.UI;

public class P_Stats_UI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider manaSlider;

    private PlayerControl player;
    private PlayerStatsManager statsManager;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
        statsManager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatsManager>();
    }

    private void Update()
    {
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        UpdateHp();
        UpdateStamina();
        UpdateMana();
    }

    private void UpdateHp()
    {
        hpSlider.value = statsManager.GetActualValue(StatType.Health) / statsManager.GetCapValue(StatType.Health);
    }

    private void UpdateStamina()
    {
        hpSlider.value = statsManager.GetActualValue(StatType.Stamina) / statsManager.GetCapValue(StatType.Stamina);
    }

    private void UpdateMana()
    {
        hpSlider.value = statsManager.GetActualValue(StatType.Stamina) / statsManager.GetCapValue(StatType.Stamina);
    }

    public void ShowUI()
    {
        gameObject.SetActive(true);
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }
}
