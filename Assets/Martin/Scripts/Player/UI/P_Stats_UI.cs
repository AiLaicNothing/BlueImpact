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
        hpSlider.value = (float)statsManager.GetActualValue(StatType.Vida) / statsManager.GetMaxValue(StatType.Vida);
    }

    private void UpdateStamina()
    {
        staminaSlider.value = (float)statsManager.GetActualValue(StatType.Estamina) / statsManager.GetMaxValue(StatType.Estamina);
    }

    private void UpdateMana()
    {
        manaSlider.value = (float)statsManager.GetActualValue(StatType.Maná) / statsManager.GetMaxValue(StatType.Maná);
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
