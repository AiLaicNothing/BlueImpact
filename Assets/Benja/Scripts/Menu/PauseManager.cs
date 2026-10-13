using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitMenuButton;

    [Header("Settings")]
    [SerializeField] private MenuSettingsUI gameplaySettingsUI;

    private bool isPaused = false;
    private PlayerInputHandler playerInputHandler;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (pausePanelCanvasGroup == null)
            pausePanelCanvasGroup = GetComponent<CanvasGroup>();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (exitMenuButton != null)
            exitMenuButton.onClick.AddListener(ExitToMenu);

        SetPanelVisible(false);

        TrySubscribe();
    }

    private void Update()
    {
        // Reintenta la suscripción si PlayerInputHandler aparece después
        if (playerInputHandler == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (playerInputHandler != null && playerInputHandler.pauseAction != null)
            playerInputHandler.pauseAction.performed -= OnPauseInput;
    }

    // ---------- INPUT ----------

    private void TrySubscribe()
    {
        if (playerInputHandler == null)
            playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (playerInputHandler == null || playerInputHandler.pauseAction == null)
            return;

        // Evita suscribirse dos veces
        playerInputHandler.pauseAction.performed -= OnPauseInput;
        playerInputHandler.pauseAction.performed += OnPauseInput;
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        // Jerarquía de cierre: de lo más interno a lo más externo
        if (gameplaySettingsUI != null && gameplaySettingsUI.IsOpen)
        {
            gameplaySettingsUI.Close();
            return;
        }

        if (CheckpointMenuUI.Instance != null && CheckpointMenuUI.Instance.IsOpen())
        {
            CheckpointMenuUI.Instance.CloseMenu();
            return;
        }

        if (isPaused)
            Resume();
        else
            Pause();
    }

    // ---------- PAUSE / RESUME ----------

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        SetPanelVisible(true);

        // Este panel queda en la base de la pila: B reanuda si no hay ajustes encima
        GamepadCursor.Instance?.Acquire(this, Resume);

        // UI activa el cursor visible; el modo se encarga del resto
        GameModeManager.Instance?.SetMode(GameMode.UI);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        SetPanelVisible(false);

        GamepadCursor.Instance?.Release(this);

        // Gameplay oculta y bloquea el cursor de nuevo
        GameModeManager.Instance?.SetMode(GameMode.Gameplay);
    }

    // ---------- BUTTONS ----------

    public void OpenSettings()
    {
        if (gameplaySettingsUI != null)
            gameplaySettingsUI.Open();
    }

    public void ExitToMenu()
    {
        GamepadCursor.Instance?.Release(this);
        Time.timeScale = 1f;
        SceneManager.LoadScene("menu");
    }

    // ---------- HELPERS ----------

    private void SetPanelVisible(bool visible)
    {
        if (pausePanelCanvasGroup == null) return;

        pausePanelCanvasGroup.alpha = visible ? 1f : 0f;
        pausePanelCanvasGroup.interactable = visible;
        pausePanelCanvasGroup.blocksRaycasts = visible;
    }
}