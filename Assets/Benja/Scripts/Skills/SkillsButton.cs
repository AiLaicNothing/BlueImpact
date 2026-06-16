using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    private Skill skill;
    private SkillManagementPanel panel;
    private Button button;

    public void Initialize(Skill targetSkill, SkillManagementPanel targetPanel)
    {
        skill = targetSkill;
        panel = targetPanel;
        button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

        // Asignar sprite
        var image = GetComponent<Image>();
        if (image != null)
            image.sprite = skill.skillSprite;
    }

    private void OnClick()
    {
        panel.SelectSkill(skill);
    }

    public Skill GetSkill() => skill;
}