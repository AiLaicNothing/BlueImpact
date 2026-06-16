using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManagementPanel : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Button[] slotButtons = new Button[4];
    [SerializeField] private Image[] slotIcons = new Image[4];

    [Header("Available Skills")]
    [SerializeField] private Transform availableSkillsParent;
    [SerializeField] private GameObject skillButtonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillInfoText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button unequipButton;

    private P_Skill_UI skillUI;

    private PlayerControl player;
    private Skill selectedSkill;
    private int selectedSlotIndex = -1;
    private List<Button> skillButtons = new();

    private void Awake()
    {
        SetupSlotButtons();
        closeButton.onClick.AddListener(Close);
        unequipButton.onClick.AddListener(UnequipSelectedSlot);
    }

    private void SetupSlotButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectSlot(index));
        }
    }

    public void Open()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerControl>();
        if (player == null)
        {
            Debug.LogError("❌ Player no encontrado");
            return;
        }

        // ✅ BUSCAR P_Skill_UI
        if (skillUI == null)
            skillUI = FindAnyObjectByType<P_Skill_UI>();

        gameObject.SetActive(true);
        selectedSkill = null;
        selectedSlotIndex = -1;

        var unlockedSkills = player.GetUnlockedSkills();
        Debug.Log($"📋 Skills desbloqueadas: {unlockedSkills.Count}");
        foreach (var skill in unlockedSkills)
        {
            Debug.Log($"   - {skill.skillName}");
        }

        Refresh();
    }

    public void Close()
    {
        selectedSkill = null;
        selectedSlotIndex = -1;
        gameObject.SetActive(false);
        CheckpointMenuUI.Instance?.ShowMainPanel();
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshAvailableSkills();
        RefreshInfo();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            var skill = player.GetEquippedSkill(i);

            if (skill != null && skill.skillSprite != null)
            {
                slotIcons[i].sprite = skill.skillSprite;
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }
        }
    }

    private void RefreshAvailableSkills()
    {
        if (player == null) return;

        foreach (Transform child in availableSkillsParent)
            Destroy(child.gameObject);

        skillButtons.Clear();

        var unlockedSkills = player.GetUnlockedSkills();

        foreach (var skill in unlockedSkills)
        {
            // ✅ VERIFICAR SI YA ESTÁ EQUIPADA
            bool equipped = false;
            for (int i = 0; i < 4; i++)
            {
                if (player.GetEquippedSkill(i) == skill)
                {
                    equipped = true;
                    break;
                }
            }

            // ✅ SI YA ESTÁ EQUIPADA, NO MOSTRARLA
            if (equipped)
                continue;

            // SOLO CREAR EL BOTÓN SI NO ESTÁ EQUIPADA
            var buttonObj = Instantiate(skillButtonPrefab, availableSkillsParent);
            var skillButton = buttonObj.GetComponent<SkillButton>();

            if (skillButton != null)
            {
                skillButton.Initialize(skill, this);
            }
        }
    }

    private void RefreshInfo()
    {
        if (selectedSkill != null)
        {
            skillNameText.text = selectedSkill.skillName;
            skillInfoText.text = $"Cost: {selectedSkill.cost}\nCooldown: {selectedSkill.cooldown}";
            return;
        }

        if (selectedSlotIndex >= 0)
        {
            var skill = player.GetEquippedSkill(selectedSlotIndex);
            if (skill != null)
            {
                skillNameText.text = skill.skillName;
                skillInfoText.text = $"Cost: {skill.cost}\nCooldown: {skill.cooldown}";
            }
            else
            {
                skillNameText.text = $"Slot {selectedSlotIndex + 1}";
                skillInfoText.text = "Vacío";
            }
            return;
        }

        skillNameText.text = "Skills";
        skillInfoText.text = "Selecciona una habilidad o slot";
    }

    public void SelectSkill(Skill skill)
    {
        selectedSkill = skill;

        if (selectedSlotIndex >= 0)
        {
            EquipSkill();
            return;
        }

        RefreshInfo();
    }

    private void SelectSlot(int slot)
    {
        if (selectedSkill != null)
        {
            selectedSlotIndex = slot;
            EquipSkill();
            return;
        }

        if (selectedSlotIndex == slot)
        {
            selectedSlotIndex = -1;
            Refresh();
            return;
        }

        selectedSlotIndex = slot;
        RefreshInfo();
    }

    private void EquipSkill()
    {
        if (selectedSkill != null && selectedSlotIndex >= 0)
        {
            player.EquipSkill(selectedSlotIndex, selectedSkill);
            selectedSkill = null;
            selectedSlotIndex = -1;
            Refresh();

            // ✅ ACTUALIZAR HUD
            if (skillUI != null)
                skillUI.RefreshIcons();
        }
    }

    private void UnequipSelectedSlot()
    {
        if (selectedSlotIndex >= 0)
        {
            player.UnequipSkill(selectedSlotIndex);
            selectedSlotIndex = -1;
            Refresh();

            // ✅ ACTUALIZAR HUD
            if (skillUI != null)
                skillUI.RefreshIcons();
        }
    }
}