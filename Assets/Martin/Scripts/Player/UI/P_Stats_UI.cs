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
        UpdateSliders();
    }
    private void OnEnable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned += SetPlayer;
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= SetPlayer;
    }

    private void SetPlayer(PlayerControl playerControl)
    {
        player = playerControl;
        statsManager = playerControl.GetComponent<PlayerStatsManager>();
    }


    private void UpdateSliders()
    {
        if (player == null || statsManager == null) return;  

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
