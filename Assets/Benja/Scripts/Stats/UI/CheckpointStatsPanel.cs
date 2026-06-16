using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CheckpointStatsPanel : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TMP_Text availablePointsText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private StatEntryUI statEntryPrefab;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    private PlayerStatsManager playerStats;  // ✅ SIN SERIALIZEFIELD
    private readonly List<StatEntryUI> entries = new();
    public StatsModificationSession Session { get; private set; }

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        confirmButton.onClick.AddListener(ConfirmChanges);
        backButton.onClick.AddListener(Back);
        cancelButton.onClick.AddListener(CancelChanges);
    }


    private void OnDisable()
    {
        // ✅ DESUSCRIBIRSE
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;

        Session = null;
        ClearEntries();
    }

    private void OnEnable()
    {
        Debug.Log("✅ CheckpointStatsPanel.OnEnable - Suscribiendo al evento");
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerControl player)
    {
        Debug.Log("✅ CheckpointStatsPanel.OnPlayerSpawned - Recibí el evento");
        playerStats = player.GetComponent<PlayerStatsManager>();
        Debug.Log($"playerStats guardado: {(playerStats != null ? "✅" : "❌")}");
    }

    private void Back()
    {
        if (Session != null)
            Session.CancelChanges();

        CheckpointMenuUI.Instance.ShowMainPanel();

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }

    public void OpenSession()
    {
        // ✅ SI NO TIENE playerStats, BUSCAR
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStatsManager>();
            Debug.LogWarning("⚠️ playerStats era null, lo busqué dinámicamente");
        }

        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStatsManager no encontrado");
            return;
        }

    

    Debug.Log("OpenSession");

        playerStats.EnsureInitialized();
        Debug.Log($"Stats encontradas: {playerStats.GetAllStats().Count}");

        Session = new StatsModificationSession(playerStats);

        characterIcon.sprite = playerStats.CharacterDefinition.characterIcon;

        CreateEntries();
        Refresh();

        if (eventSystem != null && entries.Count > 0)
        {
            var firstStat = entries[0].GetComponentInChildren<Button>();
            if (firstStat != null)
                eventSystem.SetSelectedGameObject(firstStat.gameObject);
        }
    }

    private void CreateEntries()
    {
        ClearEntries();

        foreach (var runtimeStat in playerStats.GetAllStats().Values)
        {
            StatEntryUI entry = Instantiate(statEntryPrefab, statsContainer);
            entry.Initialize(runtimeStat.definition, this);
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
            Refresh();
    }

    public void TryDecrease(StatDefinition stat)
    {
        if (Session.UndoIncrease(stat))
            Refresh();
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
        availablePointsText.text = $"Puntos: {Session.RemainingPoints}";
        confirmButton.interactable = Session.GetUsedPoints() > 0;

        foreach (StatEntryUI entry in entries)
        {
            entry.Refresh();
        }
    }
}