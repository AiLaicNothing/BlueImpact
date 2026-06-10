using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointStatsPanel : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerStatsManager playerStats;

    [Header("Header")]
    [SerializeField] private Image characterIcon;

    [SerializeField] private TMP_Text availablePointsText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    [SerializeField] private Button cancelButton;

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;

    [SerializeField] private StatEntryUI statEntryPrefab;

    private readonly List<StatEntryUI> entries = new();

    public StatsModificationSession Session { get; private set; }

    private void Awake()
    {
        confirmButton.onClick.AddListener(ConfirmChanges);

        cancelButton.onClick.AddListener(CancelChanges);
    }

    private void OnEnable()
    {
        OpenSession();
    }

    private void OnDisable()
    {
        ClearEntries();
    }

    private void OpenSession()
    {
        playerStats.EnsureInitialized();

        Session = new StatsModificationSession(playerStats);

        characterIcon.sprite =
            playerStats.CharacterDefinition.characterIcon;

        CreateEntries();

        Refresh();
    }

    private void CreateEntries()
    {
        ClearEntries();

        foreach (var runtimeStat in playerStats.GetAllStats().Values)
        {
            StatEntryUI entry =
                Instantiate(
                    statEntryPrefab,
                    statsContainer);

            entry.Initialize(
                runtimeStat.definition,
                this);

            entries.Add(entry);
        }
    }

    private void ClearEntries()
    {
        foreach (StatEntryUI entry in entries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    public void TryIncrease(StatDefinition stat)
    {
        if (Session.IncreaseStat(stat))
        {
            Refresh();
        }
    }

    public void TryDecrease(StatDefinition stat)
    {
        if (Session.UndoIncrease(stat))
        {
            Refresh();
        }
    }

    private void ConfirmChanges()
    {
        Session.ConfirmChanges();

        Refresh();
    }

    private void CancelChanges()
    {
        Session.CancelChanges();

        Refresh();
    }

    private void Refresh()
    {
        availablePointsText.text =
            $"Puntos: {Session.RemainingPoints}";

        confirmButton.interactable =
            Session.GetUsedPoints() > 0;

        foreach (StatEntryUI entry in entries)
        {
            entry.Refresh();
        }
    }
}