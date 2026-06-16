using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitMenuButton;

    [SerializeField] private GameplaySettingsUI gameplaySettingsUI;
    [SerializeField] private EventSystem eventSystem;

    private bool isPaused = false;
    private PlayerInputHandler playerInputHandler;
    private Button currentSelectedButton;

    private void Start()
    {
        // Obtener el input handler
        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

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

        // EventSystem para navegación
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        currentSelectedButton = resumeButton;
    }

    private void OnEnable()
    {
        // Suscribirse a evento de pausa del input system
        if (playerInputHandler != null && playerInputHandler.pauseAction != null)
        {
            playerInputHandler.pauseAction.performed += OnPauseInput;
        }
    }

    private void OnDisable()
    {
        if (playerInputHandler != null && playerInputHandler.pauseAction != null)
        {
            playerInputHandler.pauseAction.performed -= OnPauseInput;
        }
    }

    // Callback del Input System
    private void OnPauseInput(InputAction.CallbackContext context)
    {
        if (isPaused)
            Resume();
        else
            Pause();
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

        // Mantener el button target en Resume
        if (eventSystem != null && resumeButton != null)
        {
            eventSystem.SetSelectedGameObject(resumeButton.gameObject);
            currentSelectedButton = resumeButton;
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

        // Deseleccionar botón
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("menu");
    }

    // Método para cuando Settings se cierra
    public void OnSettingsClosed()
    {
        // Volver a seleccionar botón después de cerrar settings
        if (eventSystem != null && settingsButton != null)
        {
            eventSystem.SetSelectedGameObject(settingsButton.gameObject);
        }
    }
}