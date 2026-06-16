using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] private Image skillIcon;  // ✅ SERIALFIELD

    private Skill skill;
    private SkillManagementPanel panel;
    private Button button;

    public void Initialize(Skill targetSkill, SkillManagementPanel targetPanel)
    {
        skill = targetSkill;
        panel = targetPanel;
        button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

        // ✅ USAR EL SERIALFIELD
        if (skillIcon != null)
            skillIcon.sprite = skill.skillSprite;
        else
            Debug.LogWarning("⚠️ skillIcon no asignado en el prefab");
    }

    private void OnClick()
    {
        panel.SelectSkill(skill);
    }

    public Skill GetSkill() => skill;
}