using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitMenuButton;

    [SerializeField] private MenuSettingsUI gameplaySettingsUI;

    private bool isPaused = false;

    private void Start()
    {
        // Conectar botones
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (exitMenuButton != null)
            exitMenuButton.onClick.AddListener(ExitToMenu);

        // Pausepanel invisible al inicio
        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 0;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // ESC para pausar/reanudar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 1;
            pausePanelCanvasGroup.interactable = true;
            pausePanelCanvasGroup.blocksRaycasts = true;
        }

        // Cambiar GameMode
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.UI);

        Debug.Log("Juego pausado");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 0;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }

        // Cambiar GameMode
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.Gameplay);

        Debug.Log("Juego reanudado");
    }

    public void OpenSettings()
    {
        if (gameplaySettingsUI != null)
        {
            gameplaySettingsUI.Open();
        }
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f; // Restaurar tiempo
        SceneManager.LoadScene("MainMenu");
    }
}