using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

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

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    [Header("Gamepad Settings")]
    [SerializeField] private float gamepadNavigationCooldown = 0.2f;
    [SerializeField] private float joystickSensitivity = 500f; // Velocidad del cursor con joystick

    public event Action OnSkillEquipped;
    public event Action OnSkillUnequipped;
    public event Action OnClosePressed;

    private P_Skill_UI skillUI;

    private PlayerControl player;
    private Skill selectedSkill;
    private int selectedSlotIndex = -1;
    private List<SkillButton> skillButtons = new();  // ✅ Cambiar a List<SkillButton>
    private SkillButton lastSelectedSkillButton;  // ✅ Trackear el último seleccionado

    // ✅ CONTROL DE FOCO
    private bool inputEnabled = true;

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        SetupSlotButtons();
        closeButton.onClick.AddListener(() => OnCloseButtonClicked());
        unequipButton.onClick.AddListener(() => OnUnequipButtonClicked());
    }

    private void Update()
    {
        // ✅ SOLO PROCESAR INPUT SI ESTE PANEL TIENE EL FOCO
        if (!gameObject.activeInHierarchy || !inputEnabled)
            return;

        HandleGamepadInput();
    }

    private void HandleGamepadInput()
    {
        // ✅ MOVER CURSOR CON JOYSTICK (Solo en este panel)
        HandleJoystickCursorMovement();

        // ✅ BOTONES DE SISTEMA (A, B, X, Y)
        HandleGamepadButtonInputs();
    }

    /// <summary>
    /// ✅ Mover el cursor con el Left Stick del gamepad
    /// </summary>
    private void HandleJoystickCursorMovement()
    {
        if (Gamepad.current == null)
        {
            Debug.LogWarning("[Joystick] Gamepad.current es NULL");
            return;
        }

        Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

        // Solo mover si hay input significativo (deadzone reducido a 0.05f)
        if (stickInput.magnitude > 0.05f)
        {
            Debug.Log("[Joystick] Input detectado, moviendo cursor...");

            // ✅ Usar Input.mousePosition (API antigua pero funciona)
            Vector3 currentCursorPos = Input.mousePosition;

            // Calcular movimiento CON MULTIPLICADOR MAYOR
            // ✅ USAR unscaledDeltaTime para que funcione cuando Time.timeScale = 0
            Vector2 cursorDelta = stickInput * joystickSensitivity * Time.unscaledDeltaTime * 2f;

            // Nueva posición
            Vector3 newCursorPos = currentCursorPos + new Vector3(cursorDelta.x, cursorDelta.y, 0);

            // Limitar a los límites de la pantalla
            newCursorPos.x = Mathf.Clamp(newCursorPos.x, 0, Screen.width);
            newCursorPos.y = Mathf.Clamp(newCursorPos.y, 0, Screen.height);

            // ✅ USAR WARP CURSOR POSITION DEL INPUT SYSTEM
#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                Mouse.current.WarpCursorPosition(newCursorPos);
                Debug.Log($"[Joystick] ✅ Cursor movido a: {newCursorPos}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Joystick] ❌ Error moviendo cursor: {e.Message}");
            }
#endif
        }
    }

    /// <summary>
    /// ✅ Manejar botones del gamepad (A, B, X, Y) para acciones rápidas
    /// </summary>
    private void HandleGamepadButtonInputs()
    {
        if (Gamepad.current == null)
            return;

        // ✅ A Button = Simular click en el elemento bajo el cursor
        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            SimulateClickAtCursor();
        }

        // ✅ X Button = Desequipar skill
        if (Gamepad.current.xButton.wasPressedThisFrame)
        {
            UnequipSelectedSlot();
        }

        // ✅ B Button = Cerrar panel
        if (Gamepad.current.bButton.wasPressedThisFrame)
        {
            OnCloseButtonClicked();
            return;
        }
    }

    /// <summary>
    /// ✅ Simular un click del mouse en la posición actual del cursor
    /// </summary>
    private void SimulateClickAtCursor()
    {
        if (eventSystem == null)
        {
            Debug.LogError("[SimulateClick] EventSystem es NULL");
            return;
        }

        // Raycast desde posición del cursor
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        Debug.Log($"[SimulateClick] Raycast encontró {results.Count} objetos");

        // ✅ Buscar el PRIMER BUTTON en los resultados (no el primer objeto)
        foreach (RaycastResult hit in results)
        {
            Button button = hit.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                Debug.Log($"✅ Click simulado en: {button.gameObject.name}");
                return;
            }

            // Si no tiene Button, buscar en padre
            Button parentButton = hit.gameObject.GetComponentInParent<Button>();
            if (parentButton != null && parentButton.interactable)
            {
                parentButton.onClick.Invoke();
                Debug.Log($"✅ Click simulado en padre: {parentButton.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("[SimulateClick] No se encontró ningún Button clickeable");
    }

    private void SetupSlotButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectSlot(index));
        }
    }

    private void OnCloseButtonClicked()
    {
        Close();
        OnClosePressed?.Invoke();
        Debug.Log("[Gamepad] Panel Skills cerrado");
    }

    private void OnUnequipButtonClicked()
    {
        UnequipSelectedSlot();
    }

    public void Open()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerControl>();
        if (player == null)
        {
            Debug.LogError("❌ Player no encontrado");
            return;
        }

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

        // ✅ HABILITAR INPUT DEL PANEL
        inputEnabled = true;

        // ✅ ASEGURAR QUE CURSOR ESTÉ VISIBLE Y DESBLOQUEADO PARA JOYSTICK + MOUSE
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("✅ Cursor visible y desbloqueado - Gamepad + Mouse activado");
        Debug.Log("✅ Input habilitado");

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);

        // ✅ REGISTRAR ESTE PANEL COMO ACTIVO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PushPanel(this);
    }

    public void Close()
    {
        selectedSkill = null;
        selectedSlotIndex = -1;
        gameObject.SetActive(false);

        // ✅ DESREGISTRAR ESTE PANEL DEL FOCO
        if (PanelFocusManager.Instance != null)
            PanelFocusManager.Instance.PopPanel();

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
            bool equipped = false;
            for (int i = 0; i < 4; i++)
            {
                if (player.GetEquippedSkill(i) == skill)
                {
                    equipped = true;
                    break;
                }
            }

            if (equipped)
                continue;

            var buttonObj = Instantiate(skillButtonPrefab, availableSkillsParent);
            var skillButton = buttonObj.GetComponent<SkillButton>();

            if (skillButton != null)
            {
                skillButton.Initialize(skill, this);
                skillButtons.Add(skillButton);  // ✅ Agregar SkillButton en lugar de Button
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
        string description = "";
        description += $"<b>Costo:</b> {skill.cost}\n";
        description += $"<b>Cooldown:</b> {skill.cooldown}s\n";

        string damageInfo = skill.GetDamageDescription();
        if (!string.IsNullOrEmpty(damageInfo))
        {
            description += "\n" + damageInfo;
        }

        return description;
    }

    public void SelectSkill(Skill skill, SkillButton skillButton = null)
    {
        // ✅ DESELECCIONAR VISUALMENTE EL ANTERIOR
        if (lastSelectedSkillButton != null)
        {
            lastSelectedSkillButton.DeselectVisually();
        }

        // ✅ SELECCIONAR VISUALMENTE EL NUEVO
        if (skillButton != null)
        {
            skillButton.SelectVisually();
        }

        // ✅ GUARDAR NUEVO COMO SELECCIONADO
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
            EquipSelectedSkill();  // ✅ DESCOMENTAR
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

        Debug.Log($"[Gamepad] Slot {slot} seleccionado");
    }

    private void UnequipSelectedSlot()
    {
        if (selectedSlotIndex >= 0)
        {
            player.UnequipSkill(selectedSlotIndex);
            selectedSlotIndex = -1;
            Refresh();

            if (skillUI != null)
                skillUI.RefreshIcons();

            OnSkillUnequipped?.Invoke();
            Debug.Log("[Gamepad] Skill desequipada");
        }
    }

    // ✅ EQUIPAR SKILL SELECCIONADA EN SLOT SELECCIONADO
    private void EquipSelectedSkill()
    {
        if (selectedSkill != null && selectedSlotIndex >= 0)
        {
            player.EquipSkill(selectedSlotIndex, selectedSkill);
            selectedSkill = null;
            selectedSlotIndex = -1;
            Refresh();

            if (skillUI != null)
                skillUI.RefreshIcons();

            OnSkillEquipped?.Invoke();
            Debug.Log($"[SkillManagementPanel] ✅ Skill equipada");
        }
    }

    // ✅ IMPLEMENTAR INTERFAZ IGamepadPanel
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        Debug.Log($"[SkillManagementPanel] Input {(enabled ? "HABILITADO" : "DESHABILITADO")}");
    }

    public void SetInteractable(bool interactable)
    {
        // Deshabilitar todos los botones de slot
        foreach (var btn in slotButtons)
        {
            btn.interactable = interactable;
        }

        closeButton.interactable = interactable;
        unequipButton.interactable = interactable && (selectedSlotIndex >= 0);

        // Deshabilitar todos los botones de skill
        foreach (var btn in skillButtons)
        {
            btn.SetButtonInteractable(interactable);  // ✅ USAR NUEVO MÉTODO
        }

        Debug.Log($"[SkillManagementPanel] Interactable = {interactable}");
    }

    public string GetPanelName() => "SkillManagementPanel";
}