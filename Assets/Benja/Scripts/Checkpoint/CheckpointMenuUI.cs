using UnityEngine;
using UnityEngine.UI;

public class CheckpointMenuUI : MonoBehaviour
{
    public static CheckpointMenuUI Instance { get; private set; }

    [Header("Panels")]
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




    private Checkpoint currentCheckpoint;

    private void Awake()
    {
        Instance = this;

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
    }
    public void Open(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;

        GameModeManager.Instance.SetMode(GameMode.UI);

        CloseAllPanels();

        mainPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        CloseAllPanels();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
    }

    private void OpenTravelPanel()
    {
        CloseAllPanels();

        teleportPanel.gameObject.SetActive(true);

        teleportPanel.Open();
    }

    private void OpenStatsPanel()
    {
        CloseAllPanels();

        statsPanel.SetActive(true);

        checkpointStatsPanel.OpenSession();
    }

    private void OpenSkillsPanel()
    {
        CloseAllPanels();

        skillsPanel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        mainPanel.SetActive(false);

        teleportPanel.gameObject.SetActive(false);

        statsPanel.SetActive(false);

        skillsPanel.SetActive(false);
    }
}