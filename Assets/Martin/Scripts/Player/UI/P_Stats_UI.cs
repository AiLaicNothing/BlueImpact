using UnityEngine;
using UnityEngine.UI;

public class P_Stats_UI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider manaSlider;

    private PlayerControl player;
    private PlayerStatsManager statsManager;
    private bool hasFoundPlayer;

    private void Update()
    {
        FindPlayer();
        UpdateSliders();
    }

    private void FindPlayer()
    {
        if (!hasFoundPlayer)
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
            statsManager = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatsManager>();

            if (player != null && statsManager != null )
            {
                hasFoundPlayer = true;
            }
        }
    }

    private void UpdateSliders()
    {
        UpdateHp();
        UpdateStamina();
        UpdateMana();
    }

    private void UpdateHp()
    {
        hpSlider.value = (float)statsManager.GetActualValue(StatType.Health) / statsManager.GetMaxValue(StatType.Health);
    }

    private void UpdateStamina()
    {
        staminaSlider.value = (float)statsManager.GetActualValue(StatType.Stamina) / statsManager.GetMaxValue(StatType.Stamina);
    }

    private void UpdateMana()
    {
        manaSlider.value = (float)statsManager.GetActualValue(StatType.Mana) / statsManager.GetMaxValue(StatType.Mana);
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
