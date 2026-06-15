using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    // Paneles
    [SerializeField] private MenuSettingsUI settingsPanel;
    [SerializeField] private CreditsPanel creditsPanel;

    // Animaciones
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private string gameSceneName = "GameScene";

    // Audio/Efectos
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Asignar listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        // Asegurar que el menú está visible
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1;
            menuCanvasGroup.interactable = true;
        }

        // Dejar que sea persistente si lo necesitas
        // DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Asegurar que el juego no está pausado
        Time.timeScale = 1f;
    }

    // ==================== BUTTON CALLBACKS ====================

    private void OnPlayButtonClicked()
    {
        if (isTransitioning) return;

        PlayButtonSound();
        StartCoroutine(FadeOutAndLoadScene(gameSceneName));
    }

    private void OnSettingsButtonClicked()
    {
        PlayButtonSound();

        if (settingsPanel != null)
        {
            settingsPanel.Open();
        }
        else
        {
            Debug.LogWarning("MenuSettingsUI no está asignado");
        }
    }

    private void OnCreditsButtonClicked()
    {
        PlayButtonSound();

        if (creditsPanel != null)
        {
            creditsPanel.Open();
        }
        else
        {
            Debug.LogWarning("CreditsPanel no está asignado");
        }
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

        // Fade out
        yield return FadeOut();

        // Cargar escena
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            menuCanvasGroup.alpha = alpha;
            yield return null;
        }

        menuCanvasGroup.alpha = 0f;
    }

    private void ExitGame()
    {
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
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    // ==================== HELPERS ====================

    public void ResumeMenu()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = true;
        }
    }

    public void ShowVersion(TextMeshProUGUI versionText)
    {
        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }
    }
}
