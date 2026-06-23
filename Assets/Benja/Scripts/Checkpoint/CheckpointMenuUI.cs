using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CheckpointMenuUI : MonoBehaviour
{
    public static CheckpointMenuUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainPanelCanvasGroup;
    [SerializeField] private GameObject mainPanel;

    [SerializeField] private TeleportPanelUI teleportPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private CheckpointStatsPanel checkpointStatsPanel;

    [SerializeField] private GameObject skillsPanel;

    [Header("Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button closeButton;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    private Checkpoint currentCheckpoint;
    private bool isOpen = false;
    public bool IsOpen() => isOpen;

    private void Awake()
    {
        Instance = this;

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (mainPanelCanvasGroup == null)
            mainPanelCanvasGroup = mainPanel.GetComponent<CanvasGroup>();

        CloseAllPanels();

        travelButton.onClick.AddListener(OpenTravelPanel);
        statsButton.onClick.AddListener(OpenStatsPanel);
        skillsButton.onClick.AddListener(OpenSkillsPanel);
        closeButton.onClick.AddListener(CloseMenu);
    }

    public void ShowMainPanel()
    {
        CloseAllPanels();
        mainPanel.SetActive(true);
        isOpen = true;

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(travelButton.gameObject);
        }
    }

    public void Open(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.UI);

        CloseAllPanels();
        mainPanel.SetActive(true);
        isOpen = true;

        // ✅ CONGELAR JUEGO UNA VEZ AL ABRIR (safe zone)
        Time.timeScale = 0f;

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(travelButton.gameObject);
        }
    }

    public void CloseMenu()
    {
        CloseAllPanels();
        isOpen = false;

        // ✅ DESCONGELAR JUEGO AL CERRAR
        Time.timeScale = 1f;

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private void OpenTravelPanel()
    {
        CloseAllPanels();
        teleportPanel.gameObject.SetActive(true);
        teleportPanel.Open();

        // ✅ SIN TOCAR Time.timeScale - el juego sigue congelado

        if (eventSystem != null)
        {
            var firstButton = teleportPanel.GetComponent<Button>();
            if (firstButton != null)
                eventSystem.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private void OpenStatsPanel()
    {
        CloseAllPanels();
        statsPanel.SetActive(true);
        checkpointStatsPanel.OpenSession();

        // ✅ SIN TOCAR Time.timeScale - el juego sigue congelado

        if (eventSystem != null && checkpointStatsPanel != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private void OpenSkillsPanel()
    {
        CloseAllPanels();
        skillsPanel.SetActive(true);

        // ✅ SIN TOCAR Time.timeScale - el juego sigue congelado

        var skillPanel = skillsPanel.GetComponent<SkillManagementPanel>();
        if (skillPanel != null)
            skillPanel.Open();

        if (eventSystem != null)
        {
            var firstButton = skillsPanel.GetComponentInChildren<Button>();
            if (firstButton != null)
                eventSystem.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private void CloseAllPanels()
    {
        mainPanel.SetActive(false);
        teleportPanel.gameObject.SetActive(false);
        statsPanel.SetActive(false);
        skillsPanel.SetActive(false);
    }
}