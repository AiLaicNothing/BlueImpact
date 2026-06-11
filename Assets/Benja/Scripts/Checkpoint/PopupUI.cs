using System.Collections;
using TMPro;
using UnityEngine;

public class PopupUI : MonoBehaviour
{
    public static PopupUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private TMP_Text messageText;

    [SerializeField] private float visibleTime = 1f;

    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;

        canvasGroup.alpha = 0;
    }

    public void Show(string message)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine =
            StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;

        yield return Fade(0, 1);

        yield return new WaitForSeconds(visibleTime);

        yield return Fade(1, 0);

        currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    elapsed / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = to;
    }
} 