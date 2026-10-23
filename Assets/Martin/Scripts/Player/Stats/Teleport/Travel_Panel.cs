using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Travel_Panel : MonoBehaviour, IGamepadPanel
{
    [Header("Lista")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject entryPrefab;

    [Header("Preview")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI checkpointName;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private Button returnButton;

    public event Action OnTravelPressed;
    public event Action OnBackPressed;
    public event Action OnCheckpointSelected;

    private readonly List<CheckpointRow> entries = new List<CheckpointRow>();
    private DiscoveredCheckpoint selectedCheckpoint;
    private bool isTraveling;
    private bool isOpen;

    private void Awake()
    {
        travelButton.onClick.AddListener(OnTravel);
        returnButton.onClick.AddListener(OnBack);
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            GamepadCursor.Instance?.Release(this);
        }
    }

    // ---------- OPEN / CLOSE ----------

    public void Open()
    {
        if (isOpen) return;

        gameObject.SetActive(true);
        isOpen = true;
        isTraveling = false;

        RefreshList();

        selectedCheckpoint = null;
        previewImage.sprite = null;
        checkpointName.text = "Selecciona un destino";
        travelButton.interactable = false;

        var active = Checkpoints_Manager.Instance != null
            ? Checkpoints_Manager.Instance.GetActiveCheckpoint()
            : null;

        if (active != null)
            SelectCheckpoint(active);

        GamepadCursor.Instance?.Acquire(this, OnBack);
    }

    // Botón volver, B o Escape
    private void OnBack()
    {
        OnBackPressed?.Invoke();
        Close();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        GamepadCursor.Instance?.Release(this);
        gameObject.SetActive(false);

        Checkpoint_Menu.Instance?.ReOpenMenu();
    }

    // ---------- SELECTION / TRAVEL ----------

    public void SelectCheckpoint(DiscoveredCheckpoint checkpoint)
    {
        if (checkpoint == null || checkpoint._data == null) return;

        selectedCheckpoint = checkpoint;
        previewImage.sprite = checkpoint._data.previewImage;
        checkpointName.text = checkpoint._data.checkpointName;
        travelButton.interactable = true;

        OnCheckpointSelected?.Invoke();
    }

    private void OnTravel()
    {
        if (!travelButton.interactable) return;
        Travel();
    }

    private void Travel()
    {
        if (isTraveling)
        {
            Debug.LogWarning("[Travel Panel] Ya hay un viaje en curso.");
            return;
        }

        if (selectedCheckpoint == null)
        {
            Debug.LogError("[Travel Panel] No hay checkpoint seleccionado.");
            return;
        }

        if (Teleport_Manager.Instance == null)
        {
            Debug.LogError("[Travel Panel] Teleport_Manager.Instance no existe.");
            return;
        }

        isTraveling = true;
        travelButton.interactable = false;

        Teleport_Manager.Instance.Teleport(selectedCheckpoint);

        Time.timeScale = 1f;

        OnTravelPressed?.Invoke();
    }

    // ---------- LIST ----------

    private void RefreshList()
    {
        Clear();

        var manager = Checkpoints_Manager.Instance;
        if (manager == null) return;

        DiscoveredCheckpoint current = manager.GetActiveCheckpoint();

        foreach (DiscoveredCheckpoint checkpoint in manager.GetDiscoveredCheckpoints())
        {
            var entry = Instantiate(entryPrefab, content);
            bool isCurrent = current != null && current.Matches(checkpoint._sceneName, checkpoint._checkpointID);

            var entryUI = entry.GetComponent<CheckpointRow>();
            entryUI.Initialize(checkpoint, this, isCurrent);
            entries.Add(entryUI);
        }
    }

    private void Clear()
    {
        foreach (CheckpointRow entry in entries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    public List<CheckpointRow> GetEntries() => entries;

    // ---------- IGamepadPanel ----------

    public void SetInputEnabled(bool enabled) { }

    public void SetInteractable(bool interactable)
    {
        travelButton.interactable = interactable && selectedCheckpoint != null;
        returnButton.interactable = interactable;

        foreach (CheckpointRow entry in entries)
        {
            if (entry != null) entry.SetInteractable(interactable);
        }
    }

    public string GetPanelName() => "Checkpoint -> Travel panel";
}