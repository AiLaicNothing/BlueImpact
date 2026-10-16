using System;
using UnityEngine;
using UnityEngine.UI;

public class Checkpoint_Menu : MonoBehaviour, IGamepadPanel
{
    public static Checkpoint_Menu Instance;

    [Header("Main panel")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject panel;

    [Header("Secondary panels")]
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject skillPanel;

    [Header("Buttons")]
    [SerializeField] private Button teleport;
    [SerializeField] private Button skill;
    [SerializeField] private Button stats;
    [SerializeField] private Button close;

    public event Action OnTravelPressed;
    public event Action OnStatsPressed;
    public event Action OnSkillsPressed;
    public event Action OnClosePressed;
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;

    private Checkpoint currentCheckpoint;
    private bool isOpen;

    public bool IsOpen() => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ClosePanels();

        teleport.onClick.AddListener(OpenTeleport);
        stats.onClick.AddListener(OpenStats);
        skill.onClick.AddListener(OpenSkills);
        close.onClick.AddListener(OnClose);
    }

    private void OnDisable()
    {
        if (isOpen)
            GamepadCursor.Instance?.Release(this);
    }

    // ---------- OPEN / CLOSE ----------

    public void Open(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;
        Time.timeScale = 0f;

        ReOpenMenu();
        OnMenuOpened?.Invoke();
    }

    // Lo llaman los subpaneles al cerrarse para volver al menú principal.
    // Acquire mueve este panel al tope de la pila sin duplicarlo.
    public void ReOpenMenu()
    {
        ClosePanels();

        panel.SetActive(true);
        isOpen = true;

        if (canvasGroup != null)
            canvasGroup.interactable = true;

        GamepadCursor.Instance?.Acquire(this, OnClose);
    }

    // B o Escape cierran el menú completo
    private void OnClose()
    {
        OnClosePressed?.Invoke();
        Close();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        ClosePanels();

        GamepadCursor.Instance?.Release(this);

        // Si la pausa sigue activa debajo, no reanudar el tiempo
        bool pausedNow = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        if (!pausedNow)
            Time.timeScale = 1f;

        OnMenuClosed?.Invoke();
    }

    private void ClosePanels()
    {
        panel.SetActive(false);
        teleportPanel.SetActive(false);
        skillPanel.SetActive(false);
        statsPanel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.interactable = false;
    }

    // ---------- SUBPANELS ----------

    // El menú se queda en la pila, debajo del subpanel. No se libera,
    // así el modo no pasa por Gameplay y el cursor no se bloquea en medio.
    private void OpenTeleport()
    {
        // Oculta solo el contenido del menú; el registro en la pila se mantiene
        panel.SetActive(false);
        teleportPanel.SetActive(false);
        skillPanel.SetActive(false);
        statsPanel.SetActive(false);

        teleportPanel.GetComponent<Travel_Panel>().Open();
        OnTravelPressed?.Invoke();
    }

    private void OpenStats()
    {
        panel.SetActive(false);
        teleportPanel.SetActive(false);
        skillPanel.SetActive(false);
        statsPanel.SetActive(false);

        statsPanel.GetComponent<Stats_Panel>().Open();
        OnStatsPressed?.Invoke();
    }

    private void OpenSkills()
    {
        panel.SetActive(false);
        teleportPanel.SetActive(false);
        skillPanel.SetActive(false);
        statsPanel.SetActive(false);

        skillPanel.GetComponent<SkillManagementPanel>().Open();
        OnSkillsPressed?.Invoke();
    }

    // ---------- IGamepadPanel ----------

    public string GetPanelName() => "Checkpoint -> Main Menu";

    public void SetInputEnabled(bool enabled) { }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
            canvasGroup.interactable = interactable;
    }
}