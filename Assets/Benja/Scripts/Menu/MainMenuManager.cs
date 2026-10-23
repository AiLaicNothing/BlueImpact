using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Paneles")]
    [SerializeField] private MenuSettingsUI settingsPanel;
    [SerializeField] private CreditsPanel creditsPanel;

    [Header("Animaciones")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    private bool isTransitioning = false;

    int firstSave;
    public bool reset;
    bool saved;
    private void Awake()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
        }
    }

    private void Start()
    {
        LoadSave();
        GameModeManager.Instance?.SetMode(GameMode.UI);
        ClearSelection();
        TakeCursor();
    }
    void LoadSave()
    {

        if (reset)
        {
            PlayerPrefs.SetInt("FirstSave", 0);
            Debug.Log(firstSave);

        }
        firstSave = PlayerPrefs.GetInt("FirstSave", 0);
        Debug.Log(firstSave);
        if (firstSave == 0)
        {
            saved = false;
            SaveLoadManager.Instance.SaveGame("gameCreated", saved);
            PlayerPrefs.SetInt("FirstSave", 1);
            Debug.Log(firstSave);
        }
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            if (!loadedData.gameCreated)
            {
                playButton.interactable = false;
            }
            else
            {
                playButton.interactable = true;
            }
        }
    }
    private void OnEnable()
    {
        Time.timeScale = 1f;

        if (GamepadCursor.Instance != null)
        {
            GameModeManager.Instance?.SetMode(GameMode.UI);
            ClearSelection();
            TakeCursor();
        }
    }

    private void OnDisable()
    {
        GamepadCursor.Instance?.Release(this);
    }

    // ==================== CURSOR / SELECCIÓN ====================

    // El menú principal es la base de la pila: B no cierra nada aquí
    private void TakeCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        GamepadCursor.Instance?.Acquire(this);
    }

    // Ningún botón debe quedar seleccionado: si lo está, A dispara Submit sobre él
    private void ClearSelection()
    {
        var es = EventSystem.current;
        if (es != null)
            es.SetSelectedGameObject(null);
    }

    // ==================== BUTTON CALLBACKS ====================

    private void OnNewGameClicked()
    {
        if (isTransitioning) return;
        saved = true;
        SaveLoadManager.Instance.SaveGame("gameCreated", saved);
        SaveLoadManager.canLoad = false;
        PlayButtonSound();
        StartCoroutine(FadeOutAndLoadScene(gameSceneName));

    }
    private void OnPlayButtonClicked()
    {
        if (isTransitioning) return;
        SaveLoadManager.canLoad = true;
        PlayButtonSound();
        StartCoroutine(FadeOutAndLoadScene(gameSceneName));
    }

    private void OnSettingsButtonClicked()
    {
        PlayButtonSound();

        if (settingsPanel != null)
            settingsPanel.Open();
        else
            Debug.LogWarning("MenuSettingsUI no está asignado");
    }

    private void OnCreditsButtonClicked()
    {
        PlayButtonSound();

        if (creditsPanel != null)
            creditsPanel.Open();
        else
            Debug.LogWarning("CreditsPanel no está asignado");
    }

    private void OnExitButtonClicked()
    {
        PlayButtonSound();
        ExitGame();
    }

    // ==================== SCENE MANAGEMENT ====================

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        isTransitioning = true;

        GamepadCursor.Instance?.Release(this);

        yield return FadeOut();

        GameModeManager.Instance?.SetMode(GameMode.Gameplay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        menuCanvasGroup.alpha = 0f;
    }

    private void ExitGame()
    {
        GamepadCursor.Instance?.Release(this);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================== AUDIO ====================

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound);
    }

    // ==================== HELPERS ====================

    public void ResumeMenu()
    {
        if (menuCanvasGroup != null)
            menuCanvasGroup.interactable = true;
    }

    public void ShowVersion(TextMeshProUGUI versionText)
    {
        if (versionText != null)
            versionText.text = $"v{Application.version}";
    }
}