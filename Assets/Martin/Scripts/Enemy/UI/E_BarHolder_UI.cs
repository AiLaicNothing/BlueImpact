using System.Collections;
using UnityEngine;

public class E_BarHolder_UI : MonoBehaviour
{
    [SerializeField] private float visibleTime = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Transform cam;

    private Coroutine hideRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        transform.forward = cam.forward;
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        // stay visible
        yield return new WaitForSeconds(visibleTime);

        // fade out
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = 1 - (timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        hideRoutine = null;
    }
}
