using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI creditsText;

    // Animación
    [SerializeField] private float fadeDuration = 0.3f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // Inicializar oculto
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;

        // Cargar créditos
        LoadCredits();
    }

    [SerializeField] private CanvasGroup mainMenuPanelCanvasGroup;

    public void Open()
    {
        gameObject.SetActive(true);

        if (mainMenuPanelCanvasGroup != null)
        {
            mainMenuPanelCanvasGroup.blocksRaycasts = false;
            mainMenuPanelCanvasGroup.interactable = false;
        }

        StartCoroutine(FadeIn());
    }

    public void Close()
    {
        if (mainMenuPanelCanvasGroup != null)
        {
            mainMenuPanelCanvasGroup.blocksRaycasts = true;
            mainMenuPanelCanvasGroup.interactable = true;
        }

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
        if (creditsText == null) return;

        string credits = @"<b>CRÉDITOS</b>

<size=80%>

<b>DESARROLLO</b>
Benja - Game Developer

<b>PROGRAMACIÓN</b>
Benja - Lead Programmer

<b>ARTE</b>
[Tu artista aquí]
[Tu artista aquí]

<b>SONIDO Y MÚSICA</b>
[Tu compositor aquí]
[Diseñador de sonido]

<b>GAMEPLAY Y DISEÑO</b>
Benja - Game Designer

<b>AGRADECIMIENTOS</b>
A todo el equipo por su dedicación
A la comunidad Unity
A todos nuestros testers

<b>HERRAMIENTAS UTILIZADAS</b>
Unity Engine
InputSystem
TextMesh Pro
Netcode for GameObjects

<b>LIBRERÍAS Y ASSETS</b>
[Agrega tus dependencias aquí]

<b>LICENCIAS</b>
Este juego fue desarrollado con Unity.

</size>

Gracias por jugar.
";

        creditsText.text = credits;
    }
}
