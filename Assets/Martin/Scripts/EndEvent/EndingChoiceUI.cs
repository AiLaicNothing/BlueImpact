using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel con las dos opciones del final. Lo abre BlueImpactEvent cuando el jugador
// interactua con el cristal, y le devuelve cual eligio.
//
// El cursor no se toca aqui: GameModeManager.SetMode ya lo muestra al entrar en Cutscene.
[RequireComponent(typeof(CanvasGroup))]
public class EndingChoiceUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject panel;        // contenedor que se enciende y apaga
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Button destroyButton;
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text destroyLabel;
    [SerializeField] private TMP_Text useLabel;

    [Header("Textos")]
    [TextArea]
    [SerializeField] private string prompt = "El Blue Impact late frente a ti.";
    [SerializeField] private string destroyText = "Destruirlo";
    [SerializeField] private string useText = "Usarlo";

    [Header("Animacion")]
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private Action<bool> onChosen;   // true = destruir, false = usar
    private Coroutine fadeRoutine;
    private bool answered;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (panel != null) panel.SetActive(false);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (destroyLabel != null) destroyLabel.text = destroyText;
        if (useLabel != null) useLabel.text = useText;
        if (promptText != null) promptText.text = prompt;

        if (destroyButton != null) destroyButton.onClick.AddListener(ChooseDestroy);
        if (useButton != null) useButton.onClick.AddListener(ChooseUse);
    }

    private void OnDestroy()
    {
        if (destroyButton != null) destroyButton.onClick.RemoveListener(ChooseDestroy);
        if (useButton != null) useButton.onClick.RemoveListener(ChooseUse);
    }

    // callback recibe true si el jugador destruye el cristal, false si lo usa
    public void Open(Action<bool> callback)
    {
        onChosen = callback;
        answered = false;

        if (panel != null) panel.SetActive(true);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Para que se pueda elegir con mando, no solo con el raton
        if (EventSystem.current != null && destroyButton != null)
        {
            EventSystem.current.SetSelectedGameObject(destroyButton.gameObject);
        }

        StartFade(1f);
    }

    public void Close()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartFade(0f, true);
    }

    private void ChooseDestroy()
    {
        Choose(true);
    }

    private void ChooseUse()
    {
        Choose(false);
    }

    private void Choose(bool destroyed)
    {
        // Un doble clic no debe disparar dos finales
        if (answered) return;

        answered = true;

        Close();

        onChosen?.Invoke(destroyed);
        onChosen = null;
    }

    private void StartFade(float target, bool disableAtEnd = false)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target, disableAtEnd));
    }

    private IEnumerator FadeRoutine(float target, bool disableAtEnd)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        // Tiempo real, por si el juego llega pausado a este punto
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, fadeDuration > 0f ? t / fadeDuration : 1f);
            yield return null;
        }

        canvasGroup.alpha = target;

        if (disableAtEnd && panel != null) panel.SetActive(false);

        fadeRoutine = null;
    }
}
