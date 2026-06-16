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

    private void Update()
    {
        FindPlayer();

        if (player == null) return;

        UpdateCooldowns();
    }
    private void FindPlayer()
    {
        if (!hasFoundPlayer)
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();

            if (player != null)
            {
                hasFoundPlayer = true;
            }
        }
    }


    public void RefreshIcons()
    {
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
        for (int i = 0; i < slots.Length; i++)
        {
            Skill skill = player.GetSkill(i);

            if (skill == null) continue;

            slots[i].UpdateCooldown(player.GetSkillCooldownRemaining(i), player.GetSkillCooldownDuration(i));
        }
    }
}
