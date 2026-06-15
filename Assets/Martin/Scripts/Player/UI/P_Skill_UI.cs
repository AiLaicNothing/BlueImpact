using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class P_Skill_UI : MonoBehaviour
{
    [SerializeField] private P_SkillSlot_UI[] slots;

    private PlayerControl player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
    }

    private void Start()
    {
        RefreshIcons();
    }

    private void Update()
    {
        if (player == null) return;

        UpdateCooldowns();
    }

    private void RefreshIcons()
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
