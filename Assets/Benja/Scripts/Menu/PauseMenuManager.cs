using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;
    [SerializeField] private GameplaySettingsUI gameplaySettingsPanel;

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private float transitionDuration = 0.3f;

    private bool isPaused = false;
    private InputAction pauseAction;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        // Obtener la acción de pausa del Input System
        pauseAction = playerInput.actions["UI/Cancel"];
        pauseAction.performed += OnPausePressed;

        InitializeButtons();
    }

    private void OnEnable()
    {
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.Disable();
    }

    private void InitializeButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        // Mostrar menú de pausa
        pauseMenuCanvasGroup.alpha = 1;
        pauseMenuCanvasGroup.interactable = true;
        pauseMenuCanvasGroup.blocksRaycasts = true;

        Debug.Log("Juego pausado");
    }

    public void Resume()
    {
        if (!isPaused) return;

        // Asegurarse que los ajustes están cerrados
        if (gameplaySettingsPanel != null)
            gameplaySettingsPanel.Close();

        isPaused = false;
        Time.timeScale = 1f;

        // Ocultar menú de pausa
        pauseMenuCanvasGroup.alpha = 0;
        pauseMenuCanvasGroup.interactable = false;
        pauseMenuCanvasGroup.blocksRaycasts = false;

        Debug.Log("Juego reanudado");
    }

    private void OpenSettings()
    {
        if (gameplaySettingsPanel != null)
        {
            gameplaySettingsPanel.Open();
        }
    }

    private void GoToMainMenu()
    {
        // Restaurar tiempo antes de ir al menú
        Time.timeScale = 1f;
        isPaused = false;

        // Cargar escena del menú
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public bool IsPaused() => isPaused;
}