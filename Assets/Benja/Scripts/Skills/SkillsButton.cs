using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image bgImage;  // ✅ Referencia al hijo "bg"
    [SerializeField] private Sprite selectedBgSprite;  // ✅ Imagen cuando está seleccionado

    private Skill skill;
    private SkillManagementPanel panel;
    private Button button;
    private Sprite originalBgSprite;  // ✅ Guardar imagen original

    public void Initialize(Skill targetSkill, SkillManagementPanel targetPanel)
    {
        skill = targetSkill;
        panel = targetPanel;
        button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

        // ✅ Guardar imagen original del bg
        if (bgImage != null)
            originalBgSprite = bgImage.sprite;

        if (skillIcon != null)
            skillIcon.sprite = skill.skillSprite;
        else
            Debug.LogWarning("⚠️ skillIcon no asignado en el prefab");

        // ✅ Validar que bgImage esté asignado
        if (bgImage == null)
            Debug.LogWarning("⚠️ bgImage no asignado en el prefab - No habrá feedback visual");
    }

    private void OnClick()
    {
        panel.SelectSkill(skill, this);  // ✅ PASAR THIS COMO SKILLBUTTON
    }

    // ✅ CAMBIAR IMAGEN DEL BG CUANDO SE SELECCIONA
    public void SelectVisually()
    {
        if (bgImage != null && selectedBgSprite != null)
        {
            bgImage.sprite = selectedBgSprite;
            Debug.Log($"[SkillButton] {skill.skillName} seleccionado visualmente");
        }
    }

    // ✅ RESTAURAR IMAGEN ORIGINAL DEL BG CUANDO SE DESELECCIONA
    public void DeselectVisually()
    {
        if (bgImage != null && originalBgSprite != null)
        {
            bgImage.sprite = originalBgSprite;
            Debug.Log($"[SkillButton] {skill.skillName} deseleccionado visualmente");
        }
    }

    // ✅ CAMBIAR INTERACTABLE DEL BOTÓN
    public void SetButtonInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    // ✅ GETTER PARA GAMEPAD NAVIGATION
    public Skill GetSkill() => skill;
}