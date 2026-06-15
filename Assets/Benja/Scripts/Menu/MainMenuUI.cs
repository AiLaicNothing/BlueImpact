using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentPanel;

    // Buttons
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    // Logos/Images
    [SerializeField] private Image gameLogo;
    [SerializeField] private Image backgroundImage;

    // Animations
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float slideDistance = 50f;

    private bool isOpen = false;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        InitializeButtonHovers();
    }

    private void InitializeButtonHovers()
    {
        // Agregar efectos visuales a los botones al hover
        AddButtonHoverEffect(playButton);
        AddButtonHoverEffect(settingsButton);
        AddButtonHoverEffect(creditsButton);
        AddButtonHoverEffect(quitButton);
    }

    private void AddButtonHoverEffect(Button button)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Lighter on hover
        colors.pressedColor = new Color(0.85f, 0.7f, 0.2f, 1f); // Gold on click
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        button.colors = colors;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        isOpen = true;
        StartCoroutine(AnimateIn());
    }

    public void Close()
    {
        isOpen = false;
        StartCoroutine(AnimateOut());
    }

    private IEnumerator AnimateIn()
    {
        canvasGroup.alpha = 0;
        contentPanel.anchoredPosition = new Vector2(slideDistance, 0);

        // Fade in + slide
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;

            canvasGroup.alpha = progress;
            contentPanel.anchoredPosition = Vector2.Lerp(
                new Vector2(slideDistance, 0),
                Vector2.zero,
                progress
            );

            yield return null;
        }

        canvasGroup.alpha = 1;
        contentPanel.anchoredPosition = Vector2.zero;
    }

    private IEnumerator AnimateOut()
    {
        canvasGroup.alpha = 1;
        contentPanel.anchoredPosition = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;

            canvasGroup.alpha = 1 - progress;
            contentPanel.anchoredPosition = Vector2.Lerp(
                Vector2.zero,
                new Vector2(-slideDistance, 0),
                progress
            );

            yield return null;
        }

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    // ==================== BUTTON ANIMATIONS ====================

    public void OnButtonEnter(Button button)
    {
        // Se ejecuta cuando el mouse entra en un botón
        StartCoroutine(ScaleButton(button, 1.1f, 0.1f));
    }

    public void OnButtonExit(Button button)
    {
        // Se ejecuta cuando el mouse sale de un botón
        StartCoroutine(ScaleButton(button, 1f, 0.1f));
    }

    private IEnumerator ScaleButton(Button button, float targetScale, float duration)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        Vector3 originalScale = rectTransform.localScale;
        Vector3 targetScaleVector = new Vector3(targetScale, targetScale, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScaleVector, progress);
            yield return null;
        }

        rectTransform.localScale = targetScaleVector;
    }

    // ==================== HELPERS ====================

    public bool IsOpen() => isOpen;

    public void SelectButton(Button button)
    {
        // Para navegación por teclado/gamepad
        button.Select();
    }
}