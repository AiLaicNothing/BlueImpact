using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BossHealthHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider healthTrailSlider;   // opcional: barra blanca que baja con retraso
    [SerializeField] private Slider staggerSlider;
    [SerializeField] private Image staggerFill;          // opcional: el Fill del slider de stagger

    [Header("Animacion")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float healthFillSpeed = 1.2f;   // barra completa por segundo
    [SerializeField] private float staggerFillSpeed = 2f;
    [SerializeField] private float trailDelay = 0.5f;
    [SerializeField] private float trailSpeed = 0.5f;
    [SerializeField] private float hideDelayOnDeath = 1f;

    [Header("Colores del Stagger")]
    [SerializeField] private Color staggerNormalColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color staggerBrokenColor = Color.white;

    private CanvasGroup canvasGroup;
    private Boss_MedusaMorgana boss;
    private Coroutine fadeRoutine;

    private bool isBound;
    private bool deathHandled;
    private float shownHealth;
    private float shownStagger;
    private float lastTargetHealth;
    private float trailTimer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    // Conecta el HUD con el boss. Deja las barras vacias para que se llenen al aparecer.
    public void Bind(Boss_MedusaMorgana newBoss)
    {
        boss = newBoss;
        isBound = boss != null;
        deathHandled = false;

        shownHealth = 0f;
        shownStagger = 0f;
        lastTargetHealth = 1f;

        if (!isBound) return;

        if (nameText != null) nameText.text = boss.DisplayName;

        if (healthSlider != null) healthSlider.value = 0f;
        if (healthTrailSlider != null) healthTrailSlider.value = 0f;

        if (staggerSlider != null)
        {
            staggerSlider.gameObject.SetActive(boss.HasStagger);
            staggerSlider.value = 0f;
        }
    }

    public void Show()
    {
        StartFade(1f);
    }

    public void Hide()
    {
        StartFade(0f);
    }

    public void HideImmediate()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        isBound = false;
        boss = null;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (!isBound) return;

        // El boss fue destruido
        if (boss == null)
        {
            isBound = false;
            Hide();
            return;
        }

        float dt = Time.unscaledDeltaTime;

        UpdateHealth(dt);
        UpdateStagger(dt);

        if (boss.IsDead && !deathHandled)
        {
            deathHandled = true;
            StartCoroutine(HideAfterDeath());
        }
    }

    private void UpdateHealth(float dt)
    {
        float targetHealth = boss.IsDead ? 0f : Mathf.Clamp01(boss.CurrentHp / Mathf.Max(1f, boss.MaxHp));

        shownHealth = Mathf.MoveTowards(shownHealth, targetHealth, healthFillSpeed * dt);

        if (healthSlider != null) healthSlider.value = shownHealth;

        // La barra de retraso solo empieza a bajar un momento despues de recibir dano
        if (targetHealth < lastTargetHealth)
        {
            trailTimer = Time.unscaledTime + trailDelay;
        }

        lastTargetHealth = targetHealth;

        if (healthTrailSlider != null)
        {
            float trail = healthTrailSlider.value;

            if (shownHealth >= trail)
            {
                healthTrailSlider.value = shownHealth;
            }
            else if (Time.unscaledTime >= trailTimer)
            {
                healthTrailSlider.value = Mathf.MoveTowards(trail, shownHealth, trailSpeed * dt);
            }
        }
    }

    private void UpdateStagger(float dt)
    {
        if (staggerSlider == null || !boss.HasStagger) return;

        float threshold = Mathf.Max(0.0001f, boss.StaggerThreshold);
        float targetStagger = Mathf.Clamp01(boss.StaggerBuild / threshold);

        shownStagger = Mathf.MoveTowards(shownStagger, targetStagger, staggerFillSpeed * dt);
        staggerSlider.value = shownStagger;

        if (staggerFill != null)
        {
            staggerFill.color = boss.IsStaggered ? staggerBrokenColor : staggerNormalColor;
        }
    }

    private IEnumerator HideAfterDeath()
    {
        yield return new WaitForSecondsRealtime(hideDelayOnDeath);

        isBound = false;
        Hide();
    }

    private void StartFade(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;
        fadeRoutine = null;
    }
}