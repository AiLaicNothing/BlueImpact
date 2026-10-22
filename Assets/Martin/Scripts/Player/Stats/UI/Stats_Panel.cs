using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Stats_Panel : MonoBehaviour, IGamepadPanel
{
    [Header("Portrait Zone")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI availablePointsText;
    private CharacterInfo data;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button returnButton;

    [Header("Stats rows")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;

    public event Action OnConfirmPressed;
    public event Action OnResetlPressed;
    public event Action OnReturnPressed;

    private bool isOpen;
    private readonly List<StatRow> entries = new List<StatRow>();

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (resetButton != null) resetButton.onClick.AddListener(OnCancel);
        if (returnButton != null) returnButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (CharacterStatsManager.Instance != null)
            CharacterStatsManager.Instance.OnStatsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (CharacterStatsManager.Instance != null)
            CharacterStatsManager.Instance.OnStatsChanged -= Refresh;

        if (isOpen)
        {
            isOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    private void Update()
    {
        // X confirma, Y cancela; solo si este panel es el activo
        if (!isOpen || Gamepad.current == null) return;
        if (GamepadCursor.Instance == null || !GamepadCursor.Instance.IsTop(this)) return;

        if (Gamepad.current.xButton.wasPressedThisFrame && confirmButton.interactable)
            OnConfirm();
        else if (Gamepad.current.yButton.wasPressedThisFrame)
            OnCancel();
    }

    // ---------- OPEN / CLOSE ----------

    public void Open()
    {
        if (isOpen) return;

        if (CharacterStatsManager.Instance == null)
        {
            Debug.LogError("Character stats manager is missing");
            return;
        }

        gameObject.SetActive(true);
        isOpen = true;

        data = Player_Manager.Instance.GetCharacterData();
        CharacterStatsManager.Instance.BeginEdit();
        characterIcon.sprite = data.data.characterImage;

        CreateEntries();
        Refresh();

        Time.timeScale = 0f;
        GamepadCursor.Instance?.Acquire(this, Close);
    }

    // Botón volver, B o Escape: descarta cambios y vuelve al menú de checkpoint
    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        CharacterStatsManager.Instance?.CancelChanges();

        GamepadCursor.Instance?.Release(this);
        gameObject.SetActive(false);

        Checkpoint_Menu.Instance?.ReOpenMenu();
        OnReturnPressed?.Invoke();
    }

    // ---------- BUTTONS ----------

    private void OnConfirm()
    {
        CharacterStatsManager.Instance.ApplyStats();
        OnConfirmPressed?.Invoke();
    }

    private void OnCancel()
    {
        CharacterStatsManager.Instance.CancelChanges();
        OnResetlPressed?.Invoke();
    }

    // ---------- ENTRIES ----------

    private void CreateEntries()
    {
        ClearEntries();

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            GameObject statRow = Instantiate(statRowPrefab, statsContainer);
            var content = statRow.GetComponent<StatRow>();
            content.Initialize(stat);
            entries.Add(content);
        }
    }

    private void ClearEntries()
    {
        foreach (StatRow row in entries)
        {
            if (row != null) Destroy(row.gameObject);
        }

        entries.Clear();
    }

    private void Refresh()
    {
        availablePointsText.text = $"Puntos: {CharacterStatsManager.Instance.AvailablePoints}";
        confirmButton.interactable = CharacterStatsManager.Instance.GetTotalPendingPoints() > 0;
    }

    // ---------- IGamepadPanel ----------

    public string GetPanelName() => "Checkpoint -> Stats Menu";

    public void SetInputEnabled(bool enabled) { }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        confirmButton.interactable = interactable && CharacterStatsManager.Instance.GetTotalPendingPoints() > 0;
        resetButton.interactable = interactable;

        foreach (var entry in entries)
        {
            var increaseButton = entry.GetIncreaseButton();
            var decreaseButton = entry.GetDecreaseButton();

            if (increaseButton != null) increaseButton.interactable = interactable;
            if (decreaseButton != null) decreaseButton.interactable = interactable;
        }
    }
}