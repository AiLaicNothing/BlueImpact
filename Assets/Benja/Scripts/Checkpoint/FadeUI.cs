using System.Collections;
using UnityEngine;

public class FadeUI : MonoBehaviour
{
    public static FadeUI Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float fadeDuration = 0.5f;

    private void Awake()
    {
        Instance = this;

        canvasGroup.alpha = 0f;
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(
        float start,
        float end)
    {
        float elapsed = 0f;

        canvasGroup.alpha = start;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha =
                Mathf.Lerp(
                    start,
                    end,
                    elapsed / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = end;
    }
}