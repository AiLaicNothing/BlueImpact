using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private GameplaySettingsUI gameplaySettingsUI;

    [Header("UI")]
    [SerializeField] private EventSystem eventSystem;

    private bool isPaused = false;
    private PlayerInputHandler playerInputHandler;

    private void Awake()
    {
        // ✅ SINGLETON
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ✅ OBTENER REFERENCIAS
        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (pausePanelCanvasGroup == null)
            pausePanelCanvasGroup = GetComponent<CanvasGroup>();

        // ✅ CONECTAR BOTONES
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (exitMenuButton != null)
            exitMenuButton.onClick.AddListener(ExitToMenu);

        // ✅ OCULTAR PAUSE PANEL AL INICIO
        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 0;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }

        // ✅ SUSCRIBIRSE SI YA EXISTE
        SubscribeToPauseAction();
    }

    private void Update()
    {
        // ✅ SI PLAYERINPUTHANDLER AÚN NO EXISTE, INTENTAR CADA FRAME
        if (playerInputHandler == null)
        {
            playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

            if (playerInputHandler != null)
            {
                Debug.Log("✅ PlayerInputHandler encontrado en Update");
                SubscribeToPauseAction();
            }
        }
    }

    private void SubscribeToPauseAction()
    {
        if (playerInputHandler == null || playerInputHandler.pauseAction == null)
        {
            Debug.LogWarning("⚠️ No se puede suscribir a pauseAction aún");
            return;
        }

        playerInputHandler.pauseAction.performed += OnPauseInput;
        Debug.Log("✅ PauseManager suscrito a pauseAction");
    }

    private void OnDisable()
    {
        if (playerInputHandler != null && playerInputHandler.pauseAction != null)
        {
            playerInputHandler.pauseAction.performed -= OnPauseInput;
        }
    }

    private void OnPauseInput(InputAction.CallbackContext context)
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

        Debug.Log("⏸️ Juego pausado");

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 1;
            pausePanelCanvasGroup.interactable = true;
            pausePanelCanvasGroup.blocksRaycasts = true;
        }

        if (eventSystem != null && resumeButton != null)
        {
            eventSystem.SetSelectedGameObject(resumeButton.gameObject);
        }

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.UI);
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        Debug.Log("▶️ Juego reanudado");

        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.alpha = 0;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(GameMode.Gameplay);
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

    public void OnSettingsClosed()
    {
        if (eventSystem != null && settingsButton != null)
        {
            eventSystem.SetSelectedGameObject(settingsButton.gameObject);
        }
    }

    public bool IsPaused => isPaused;
}