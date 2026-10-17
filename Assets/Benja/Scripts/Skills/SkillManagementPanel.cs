using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillManagementPanel : MonoBehaviour, IGamepadPanel
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

    public event Action OnSkillEquipped;
    public event Action OnSkillUnequipped;
    public event Action OnClosePressed;

    private P_Skill_UI skillUI;
    private PlayerControl player;
    private Skill selectedSkill;
    private int selectedSlotIndex = -1;
    private readonly List<SkillButton> skillButtons = new List<SkillButton>();
    private SkillButton lastSelectedSkillButton;
    private bool isOpen;

    private void Awake()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectSlot(index));
        }

        closeButton.onClick.AddListener(CloseByUser);
        unequipButton.onClick.AddListener(UnequipSelectedSlot);
        if (SaveLoadManager.canLoad)
        {
            if (skillUI != null)
                skillUI.RefreshIcons();
        }
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    private void Update()
    {
        // X desequipa, solo si este panel es el activo
        if (!isOpen || Gamepad.current == null) return;
        if (GamepadCursor.Instance == null || !GamepadCursor.Instance.IsTop(this)) return;

        if (Gamepad.current.xButton.wasPressedThisFrame)
            UnequipSelectedSlot();
    }

    // ---------- OPEN / CLOSE ----------

    public void Open()
    {
        if (isOpen) return;

        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerControl>();
        if (player == null)
        {
            Debug.LogError("Player no encontrado");
            return;
        }

        if (skillUI == null)
            skillUI = FindAnyObjectByType<P_Skill_UI>();

        gameObject.SetActive(true);
        isOpen = true;

        selectedSkill = null;
        selectedSlotIndex = -1;
        Refresh();

        GamepadCursor.Instance?.Acquire(this, CloseByUser);
    }

    // Botón cerrar, B o Escape
    private void CloseByUser()
    {
        OnClosePressed?.Invoke();
        Close();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        selectedSkill = null;
        selectedSlotIndex = -1;

        GamepadCursor.Instance?.Release(this);
        gameObject.SetActive(false);

        Checkpoint_Menu.Instance?.ReOpenMenu();
    }

    // ---------- REFRESH ----------

    private void Refresh()
    {
        RefreshSlots();
        RefreshAvailableSkills();
        RefreshInfo();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slotIcons.Length; i++)
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

        foreach (var skill in player.GetUnlockedSkills())
        {
            bool equipped = false;
            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (player.GetEquippedSkill(i) == skill)
                {
                    equipped = true;
                    break;
                }
            }

            if (equipped) continue;

            var buttonObj = Instantiate(skillButtonPrefab, availableSkillsParent);
            var skillButton = buttonObj.GetComponent<SkillButton>();

            if (skillButton != null)
            {
                skillButton.Initialize(skill, this);
                skillButtons.Add(skillButton);
            }
        }
    }

    private void RefreshInfo()
    {
        if (selectedSkill != null)
        {
            skillNameText.text = selectedSkill.skillName;
            skillInfoText.text = BuildSkillDescription(selectedSkill);
            return;
        }

        if (selectedSlotIndex >= 0)
        {
            var skill = player.GetEquippedSkill(selectedSlotIndex);
            if (skill != null)
            {
                skillNameText.text = skill.skillName;
                skillInfoText.text = BuildSkillDescription(skill);
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

    private string BuildSkillDescription(Skill skill)
    {
        string description = $"<b>Costo:</b> {skill.cost}\n<b>Cooldown:</b> {skill.cooldown}s\n";

        string damageInfo = skill.GetDamageDescription();
        if (!string.IsNullOrEmpty(damageInfo))
            description += "\n" + damageInfo;

        return description;
    }

    // ---------- SELECTION / EQUIP ----------

    // Llamado por SkillButton al hacer click
    public void SelectSkill(Skill skill, SkillButton skillButton = null)
    {
        if (lastSelectedSkillButton != null)
            lastSelectedSkillButton.DeselectVisually();

        if (skillButton != null)
            skillButton.SelectVisually();

        lastSelectedSkillButton = skillButton;
        selectedSkill = skill;

        if (selectedSlotIndex >= 0)
        {
            EquipSelectedSkill();
            return;
        }

        RefreshInfo();
    }

    private void SelectSlot(int slot)
    {
        if (selectedSkill != null)
        {
            selectedSlotIndex = slot;
            EquipSelectedSkill();
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

    private void UnequipSelectedSlot()
    {
        if (selectedSlotIndex < 0) return;

        player.UnequipSkill(selectedSlotIndex);
        selectedSlotIndex = -1;
        Refresh();

        if (skillUI != null)
            skillUI.RefreshIcons();

        OnSkillUnequipped?.Invoke();
    }

    private void EquipSelectedSkill()
    {
        if (selectedSkill == null || selectedSlotIndex < 0) return;

        player.EquipSkill(selectedSlotIndex, selectedSkill);
        selectedSkill = null;
        selectedSlotIndex = -1;
        Refresh();

        if (skillUI != null)
            skillUI.RefreshIcons();

        OnSkillEquipped?.Invoke();
    }

    // ---------- IGamepadPanel ----------

    public void SetInputEnabled(bool enabled) { }

    public void SetInteractable(bool interactable)
    {
        foreach (var btn in slotButtons)
            btn.interactable = interactable;

        closeButton.interactable = interactable;
        unequipButton.interactable = interactable && selectedSlotIndex >= 0;

        foreach (var btn in skillButtons)
            btn.SetButtonInteractable(interactable);
    }

    public string GetPanelName() => "SkillManagementPanel";
}