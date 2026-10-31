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

    [Header("Barra de casteo (la carrera contra el ataque)")]
    [SerializeField] private GameObject castGroup;       // contenedor de la barra de casteo, se enciende solo al castear
    [SerializeField] private TMP_Text castNameText;      // nombre del ataque, ej "Nova"
    [SerializeField] private Slider castTimeSlider;      // sube con el tiempo: lo que el boss quiere completar
    [SerializeField] private Image castTimeFill;         // opcional: el Fill de la barra de tiempo
    [SerializeField] private Slider castBreakSlider;     // sube con el dano del jugador: romper el casteo
    [SerializeField] private Image castBreakFill;        // opcional: el Fill de la barra de rotura
    [SerializeField] private TMP_Text castHintText;      // opcional: aviso corto, ej "¡Atacalo!"
    [SerializeField] private string castHint = "¡Atacalo para interrumpirlo!";
    [SerializeField] private string brokenLabel = "¡INTERRUMPIDO!";
    [SerializeField] private float brokenLabelDuration = 1.5f;

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
    // Mientras el boss es inmune al stagger la barra se apaga, asi el jugador entiende por que no sube
    [SerializeField] private Color staggerImmuneColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Colores del casteo")]
    [SerializeField] private Color castTimeColor = new Color(0.9f, 0.2f, 0.3f);      // rojo: el ataque viene
    [SerializeField] private Color castBreakColor = new Color(0.3f, 0.8f, 1f);       // celeste: tu progreso
    [SerializeField] private Color castBreakReadyColor = Color.white;                // casi roto

    private CanvasGroup canvasGroup;
    private Boss_MedusaMorgana boss;
    private Coroutine fadeRoutine;

    private bool isBound;
    private bool deathHandled;
    private float shownHealth;
    private float shownStagger;
    private float lastTargetHealth;
    private float trailTimer;

    private bool castVisible;
    private float brokenLabelUntil;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();

        if (healthSlider == null) Debug.LogWarning("[BossHUD] healthSlider no asignado");
        if (nameText == null) Debug.LogWarning("[BossHUD] nameText no asignado");

        // Error tipico: apuntar castGroup al mismo panel del HUD, que lo apaga entero
        if (castGroup == gameObject)
        {
            Debug.LogError("[BossHUD] castGroup apunta al propio HUD. Tiene que ser el contenedor de la barra de casteo, no el panel completo");
            castGroup = null;
        }
    }

    // Conecta el HUD con el boss. Deja las barras vacias para que se llenen al aparecer.
    public void Bind(Boss_MedusaMorgana newBoss)
    {
        boss = newBoss;
        isBound = boss != null;
        deathHandled = false;

        Debug.Log("[BossHUD] Bind: " + (isBound ? newBoss.DisplayName : "boss nulo"));

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

        HideCastBar();
    }

    public void Show()
    {
        // Si el GameObject del HUD esta desactivado, las corrutinas no corren y nunca se ve.
        // El HUD se oculta con el alpha del CanvasGroup, asi que debe quedar siempre activo.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("[BossHUD] El HUD esta desactivado en la escena. Dejalo activo: se oculta con el alpha del CanvasGroup, no con SetActive");
            return;
        }

        Debug.Log("[BossHUD] Show");
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
            HideCastBar();
            Hide();
            return;
        }

        float dt = Time.unscaledDeltaTime;

        UpdateHealth(dt);
        UpdateStagger(dt);
        UpdateCastBar();

        if (boss.IsDead && !deathHandled)
        {
            deathHandled = true;
            HideCastBar();
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
            if (boss.IsStaggered)
            {
                staggerFill.color = staggerBrokenColor;
            }
            else
            {
                staggerFill.color = boss.IsStaggerImmune ? staggerImmuneColor : staggerNormalColor;
            }
        }
    }

    // Dos barras en carrera: la roja es el ataque que viene, la celeste es lo que el jugador
    // lleva avanzado para interrumpirlo. Si la celeste llega primero, el ataque se cancela.
    private void UpdateCastBar()
    {
        if (castGroup == null) return;

        // Mientras esta interrumpido, se deja el cartel un momento para que el jugador lo lea
        if (boss.IsCastBroken)
        {
            brokenLabelUntil = Time.unscaledTime + brokenLabelDuration;

            ShowCastBar();

            if (castNameText != null) castNameText.text = brokenLabel;
            if (castHintText != null) castHintText.text = "";

            if (castTimeSlider != null) castTimeSlider.value = 0f;

            if (castBreakSlider != null) castBreakSlider.value = 1f;
            if (castBreakFill != null) castBreakFill.color = castBreakReadyColor;

            return;
        }

        bool casting = boss.IsCasting && !boss.IsDead;

        if (!casting)
        {
            if (Time.unscaledTime < brokenLabelUntil) return;

            HideCastBar();
            return;
        }

        ShowCastBar();

        float breakProgress = boss.CastBreakEnabled ? boss.CastBreakProgress : 0f;

        if (castNameText != null) castNameText.text = boss.CurrentAttackName;

        if (castHintText != null)
        {
            castHintText.text = boss.CastBreakEnabled ? castHint : "";
        }

        if (castTimeSlider != null) castTimeSlider.value = boss.CastTimeProgress;
        if (castTimeFill != null) castTimeFill.color = castTimeColor;

        if (castBreakSlider != null)
        {
            castBreakSlider.gameObject.SetActive(boss.CastBreakEnabled);
            castBreakSlider.value = breakProgress;
        }

        if (castBreakFill != null)
        {
            // Se pone blanco cerca del final: le avisa al jugador que un golpe mas lo rompe
            castBreakFill.color = breakProgress >= 0.75f ? castBreakReadyColor : castBreakColor;
        }
    }

    private void ShowCastBar()
    {
        if (castVisible || castGroup == null) return;

        castVisible = true;
        castGroup.SetActive(true);
    }

    private void HideCastBar()
    {
        castVisible = false;
        brokenLabelUntil = 0f;

        if (castGroup != null) castGroup.SetActive(false);
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