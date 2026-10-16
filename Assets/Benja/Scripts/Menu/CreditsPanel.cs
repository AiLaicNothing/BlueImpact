using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Auto Scroll")]
    [SerializeField] private RectTransform content;
    [SerializeField] private float scrollSpeed = 30f;

    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isOpen = false;
    private bool isScrolling = false;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        LoadCredits();

        gameObject.SetActive(false);
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        gameObject.SetActive(true);

        // Mientras el auto scroll está activo, el ScrollRect no debe mover el contenido
        if (scrollRect != null)
            scrollRect.enabled = false;

        // Empieza con el texto debajo de la vista
        float viewportHeight = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport.rect.height
            : Screen.height;

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, -viewportHeight);
        isScrolling = true;

        // Cursor: B cierra los créditos
        GamepadCursor.Instance?.Acquire(this, Close);

        StartFade(FadeIn());
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        isScrolling = false;

        // Libera primero para que el menú principal recupere el cursor de inmediato
        GamepadCursor.Instance?.Release(this);

        if (scrollRect != null)
            scrollRect.enabled = true;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartFade(FadeOut());
    }

    private void Update()
    {
        if (!isScrolling || content == null || scrollRect == null || scrollRect.viewport == null)
            return;

        // Scroll independiente del tiempo de juego, por si hay pausa activa
        float dt = Time.unscaledDeltaTime;
        content.anchoredPosition += Vector2.up * scrollSpeed * dt;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

        if (content.anchoredPosition.y >= maxScroll)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, maxScroll);
            isScrolling = false;
        }
    }

    // Evita que dos coroutines de fade pelean (abrir mientras se cierra, etc.)
    private void StartFade(IEnumerator routine)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(routine);
    }

    private IEnumerator FadeIn()
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        fadeRoutine = null;
    }

    private IEnumerator FadeOut()
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        fadeRoutine = null;

        // Solo desactiva si no se volvió a abrir durante el fade
        if (!isOpen)
            gameObject.SetActive(false);
    }

    private void LoadCredits()
    {
        if (creditsText == null) return;

        creditsText.text = @"


~ ° ~

Este juego fue creado por seis personas.

Sin departamentos.
Sin barreras.
Solo ideas compartidas, pruebas, errores y mejoras constantes.

Cada uno de nosotros hizo un poco de todo,
y todo el equipo hizo posible este juego.

EQUIPO

[ BENJAMIN ]

[ FRANCESCA ]

[ ALVARO ]

[ MARTIN ]

[ ROSELING ]

[ BOADA ]

AGRADECIMIENTOS

Gracias por ser parte de esta aventura y por ayudarnos a crecer como equipo.


[ 7UP ]
";
    }
}