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
    [SerializeField]
    private Button backButton;

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;

    [SerializeField] private StatEntryUI statEntryPrefab;

    private readonly List<StatEntryUI> entries = new();

    public StatsModificationSession Session { get; private set; }

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<PlayerStatsManager>();
        }

        confirmButton.onClick.AddListener(ConfirmChanges);
        backButton.onClick.AddListener(Back);
        cancelButton.onClick.AddListener(CancelChanges);
    }


    private void Back()
    {
        Session.CancelChanges();

        CheckpointMenuUI.Instance.ShowMainPanel();
    }
    private void OnDisable()
    {
        Session = null;

        ClearEntries();
    }

    public void OpenSession()
    {
        Debug.Log("OpenSession");

        playerStats.EnsureInitialized();

        Debug.Log(
    $"Stats encontradas: {playerStats.GetAllStats().Count}");

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