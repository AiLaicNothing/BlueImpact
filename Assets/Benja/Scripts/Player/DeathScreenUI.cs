using System.Collections;
using TMPro;
using UnityEngine;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float deathScreenDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;

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

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeInDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        // ✅ NO usar SetActive(false), dejar el GameObject activo con alpha = 0
    }
}