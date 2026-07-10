using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float deathScreenDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;

    [SerializeField] private Image gifImage;
    [SerializeField] private Sprite[] gifFrames;
    [SerializeField] private float gifFPS = 12f;

    private Coroutine gifCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // ✅ Panel invisible pero GameObject ACTIVO
        canvasGroup.alpha = 0f;
        // ❌ REMOVER: gameObject.SetActive(false);
    }

    private void Start()
    {
        // ✅ CAMBIO: Suscribirse en Start() en lugar de OnEnable()
        // (porque el GameObject está inactivo, OnEnable() nunca se ejecuta)

        PlayerControl.OnPlayerDied += ShowDeathScreen;

        Debug.Log("✅ DeathScreenUI - Suscrito al evento OnPlayerDied en Start()");
    }

    private void OnDestroy()
    {
        // ✅ LIMPIAR suscripción
        PlayerControl.OnPlayerDied -= ShowDeathScreen;
    }

    private void ShowDeathScreen()
    {
        Debug.Log("💀 DeathScreenUI - ShowDeathScreen() EJECUTADO");

        if (gifCoroutine != null)
            StopCoroutine(gifCoroutine);

        gifCoroutine = StartCoroutine(PlayGif());

        StartCoroutine(DeathScreenRoutine());
    }

    private IEnumerator DeathScreenRoutine()
    {
        // ✅ NO usar SetActive(true), solo manejar alpha

        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Mostrar texto
        if (deathText != null)
            deathText.text = "MORISTE...";

        // Cuenta atrás
        float remainingTime = deathScreenDuration;

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            if (countdownText != null)
            {
                int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
                countdownText.text = $"Respawneando en {secondsLeft}s...";
            }

            yield return null;
        }

        // Respawnear
        Debug.Log("📢 Llamando a RespawnManager.Respawn()");
        RespawnManager.Instance?.Respawn();


        if (gifCoroutine != null)
        {
            StopCoroutine(gifCoroutine);
            gifCoroutine = null;
        }

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeInDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;

         
    }

    private IEnumerator PlayGif()
    {
        int index = 0;
        float wait = 1f / gifFPS;

        while (true)
        {
            gifImage.sprite = gifFrames[index];

            index++;
            if (index >= gifFrames.Length)
                index = 0;

            yield return new WaitForSeconds(wait);
        }
    }
}