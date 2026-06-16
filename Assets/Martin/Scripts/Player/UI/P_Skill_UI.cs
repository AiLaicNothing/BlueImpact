using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class P_Skill_UI : MonoBehaviour
{
    [SerializeField] private P_SkillSlot_UI[] slots;

    private PlayerControl player;
    private bool hasFoundPlayer;

    private void Start()
    {
        RefreshIcons();
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
        RefreshIcons();
    }
    private void Update()
    {

        if (player == null) return;

        UpdateCooldowns();
    }



    public void RefreshIcons()
    {
        if (player == null) return;  

        for (int i = 0; i < slots.Length; i++)
        {
            Skill skill = player.GetSkill(i);

            if (skill == null)
            {
                slots[i].SetEmpty();
                continue;
            }

            slots[i].SetIcon(skill.skillSprite);
        }
    }

    private void UpdateCooldowns()
    {
        if (player == null) return; 
        for (int i = 0; i < slots.Length; i++)
        {
            Skill skill = player.GetSkill(i);

            if (skill == null) continue;

            slots[i].UpdateCooldown(player.GetSkillCooldownRemaining(i), player.GetSkillCooldownDuration(i));
        }
    }
}
