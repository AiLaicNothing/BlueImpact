using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private MenuSettingsUI settingsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float transitionDuration = 1f;

    [SerializeField] private string gameSceneName = "GameScene";

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeButtons();
        ShowMainMenu();
    }

    // ==================== INITIALIZATION ====================

    private void InitializeButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsPressed);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsPressed);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);
    }

    // ==================== BUTTON CALLBACKS ====================

    private void OnPlayPressed()
    {
        if (isTransitioning) return;

        Debug.Log("Iniciando juego...");
        StartCoroutine(LoadGameScene());
    }

    private void OnSettingsPressed()
    {
        Debug.Log("Abriendo opciones...");
        ShowSettings();
    }

    private void OnCreditsPressed()
    {
        Debug.Log("Abriendo créditos...");
        // TODO: Implementar pantalla de créditos si lo necesitas
    }

    private void OnQuitPressed()
    {
        Debug.Log("Saliendo del juego...");
        QuitGame();
    }

    // ==================== MENU NAVIGATION ====================

    public void ShowMainMenu()
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.Open();
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1;
            mainMenuCanvasGroup.interactable = true;
        }
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.Open();
        }
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.Close();
        }
    }

    public void ShowMainMenuFromSettings()
    {
        HideSettings();
        ShowMainMenu();
    }

    // ==================== SCENE TRANSITIONS ====================

    private IEnumerator LoadGameScene()
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(FadeOut(transitionDuration));

        // Cargar escena
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isTransitioning = false;
    }

    private IEnumerator FadeOut(float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        fadeCanvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn(float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        fadeCanvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    // ==================== QUIT ====================

    public void QuitGame()
    {
        Debug.Log("Cerrando aplicación...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // ==================== HELPERS ====================

    public bool IsTransitioning() => isTransitioning;

    public void SetGameSceneName(string sceneName)
    {
        gameSceneName = sceneName;
    }
}