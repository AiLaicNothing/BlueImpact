using UnityEngine;
using UnityEngine.UI;

public class P_Stats_UI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider manaSlider;

    private PlayerControl player;
    private PlayerStatsManager statsManager;
    private CharacterStats stats;
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
        stats = playerControl.GetComponent<CharacterStats>();
    }


    private void UpdateSliders()
    {
        if (player == null || stats == null) return;  

        UpdateHp();
        UpdateStamina();
        UpdateMana();
    }

    private void UpdateHp()
    {
        //hpSlider.value = (float)statsManager.GetActualValue(StatType.Vida) / statsManager.GetMaxValue(StatType.Vida);

        if (hpSlider == null || stats.MaxHp <= 0) return;

        hpSlider.value = (float)stats.CurrentHp / stats.MaxHp;
    }

    private void UpdateStamina()
    {
        //staminaSlider.value = (float)statsManager.GetActualValue(StatType.Estamina) / statsManager.GetMaxValue(StatType.Estamina);

        if (staminaSlider == null || stats.MaxStamina <= 0) return;

        staminaSlider.value = (float)stats.CurrentStamina / stats.MaxStamina;
    }

    private void UpdateMana()
    {
        //manaSlider.value = (float)statsManager.GetActualValue(StatType.Maná) / statsManager.GetMaxValue(StatType.Maná);

        if (manaSlider == null || stats.MaxMp <= 0) return;

        manaSlider.value = (float)stats.CurrentMp / stats.MaxMp;
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
