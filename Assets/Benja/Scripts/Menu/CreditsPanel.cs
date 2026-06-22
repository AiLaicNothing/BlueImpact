using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Auto Scroll")]
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform endMarker;
    [SerializeField] private float scrollSpeed = 30f;

    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isScrolling = false;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;

        LoadCredits();
    }

    public void Open()
    {
        gameObject.SetActive(true);

        StartCoroutine(FadeIn());

        // reset scroll al inicio
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, -Screen.height);

        isScrolling = true;
    }

    private void Update()
    {
        if (!isScrolling) return;

        content.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
 
        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        float maxScroll = contentHeight - viewportHeight;

        if (content.anchoredPosition.y >= maxScroll)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, maxScroll);
            isScrolling = false;
        }
    }

    private bool IsAtEnd()
    {
        float contentY = Mathf.Abs(content.anchoredPosition.y);
        float targetY = Mathf.Abs(endMarker.anchoredPosition.y);

        return contentY >= targetY;
    }

    public void Close()
    {
        isScrolling = false;
        StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private System.Collections.IEnumerator FadeOut()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    private void LoadCredits()
    {
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