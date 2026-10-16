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

        playerInputHandler.pauseAction.performed -= OnPauseInput;
        playerInputHandler.pauseAction.performed += OnPauseInput;
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        var gc = GamepadCursor.Instance;

        // Escape cierra primero el panel activo (ajustes, popup, etc.)
        if (gc != null && gc.HasOpenPanels && !gc.IsTop(this) && gc.TopCanClose)
        {
            gc.CloseTop();
            return;
        }

        // Sin paneles encima: alterna la pausa
        if (isPaused)
            Resume();
        else
            Pause();
    }

    // ---------- PAUSE / RESUME ----------

    public void Pause()
    {
        if (isPaused) return;

        // Al abrir la pausa, se cierran los demás paneles (popups, etc.)
        GamepadCursor.Instance?.CloseAllExcept(this);

        isPaused = true;
        Time.timeScale = 0f;

        SetPanelVisible(true);

        // Base de la pausa: B o Escape la reanudan si no hay nada encima
        GamepadCursor.Instance?.Acquire(this, Resume);
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        SetPanelVisible(false);

        // Release sincroniza el modo: si no quedan paneles, vuelve a Gameplay
        GamepadCursor.Instance?.Release(this);
    }

    // ---------- BUTTONS ----------

    public void OpenSettings()
    {
        if (gameplaySettingsUI != null)
            gameplaySettingsUI.Open();
    }

    public void ExitToMenu()
    {
        GamepadCursor.Instance?.CloseAllExcept(null);
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