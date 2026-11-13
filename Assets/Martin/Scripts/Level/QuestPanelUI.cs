using System.Collections;
using TMPro;
using UnityEngine;

// Panel de Quest: contador de enemigos del nivel, entrada "Encuentro misterioso" del boss y avisos.
// Colocar en un Canvas (Screen Space - Overlay) en Game.unity.
public class QuestPanelUI : MonoBehaviour
{
    [Tooltip("Panel principal. Se muestra al entrar a un nivel.")]
    [SerializeField] private GameObject root;

    [SerializeField] private TMP_Text killsText;

    [Tooltip("Entrada del boss. Se muestra cuando inicia el encuentro.")]
    [SerializeField] private GameObject bossEntry;
    [SerializeField] private TMP_Text bossText;

    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float toastDuration = 2.5f;

    [SerializeField] private string killsTemplate = "Enemigos necesarios para pasar el nivel: {0} / {1}";
    [SerializeField] private string killsDoneText = "Enemigos necesarios para pasar el nivel: completado";
    [SerializeField] private string bossStartedText = "Encuentro misterioso";
    [SerializeField] private string bossDoneText = "Encuentro misterioso: superado";
    [SerializeField] private string blockedTemplate = "Derrota {0} enemigo(s) más para continuar";
    [SerializeField] private string unlockedText = "¡Salida desbloqueada!";

    private LevelObjectiveManager manager;
    private string currentLevelId;
    private Coroutine toastRoutine;

    private static QuestPanelUI active;

    private void Awake()
    {
        // Un solo panel persistente: si ya existe uno, este se elimina.
        if (active != null && active != this)
        {
            Destroy(gameObject);
            return;
        }

        active = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (root != null) root.SetActive(false);
        if (bossEntry != null) bossEntry.SetActive(false);
        if (toastRoot != null) toastRoot.SetActive(false);

        manager = LevelObjectiveManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[QuestPanelUI] No hay LevelObjectiveManager en la escena.", this);
            return;
        }

        manager.OnLevelEntered += HandleLevelEntered;
        manager.OnProgressChanged += HandleProgress;
        manager.OnBossEncounterStarted += HandleBossStarted;
        manager.OnBossDefeated += HandleBossDefeated;
        manager.OnExitBlocked += HandleExitBlocked;
        manager.OnThresholdReached += HandleThresholdReached;
        manager.OnSessionCleared += HandleSessionCleared;
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
        if (manager == null) return;

        manager.OnLevelEntered -= HandleLevelEntered;
        manager.OnProgressChanged -= HandleProgress;
        manager.OnBossEncounterStarted -= HandleBossStarted;
        manager.OnBossDefeated -= HandleBossDefeated;
        manager.OnExitBlocked -= HandleExitBlocked;
        manager.OnThresholdReached -= HandleThresholdReached;
        manager.OnSessionCleared -= HandleSessionCleared;
    }

    // Salir al menú: se oculta todo el panel hasta entrar de nuevo a un nivel.
    private void HandleSessionCleared()
    {
        currentLevelId = null;

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = null;

        if (root != null) root.SetActive(false);
        if (bossEntry != null) bossEntry.SetActive(false);
        if (toastRoot != null) toastRoot.SetActive(false);
    }

    private void HandleLevelEntered(string levelId)
    {
        currentLevelId = levelId;

        if (root != null) root.SetActive(true);

        if (manager.TryGetProgress(levelId, out int defeated, out int required))
            HandleProgress(levelId, defeated, required);
    }

    private void HandleProgress(string levelId, int defeated, int required)
    {
        if (levelId != currentLevelId || killsText == null || manager == null) return;

        killsText.text = manager.IsUnlocked(levelId)
            ? killsDoneText
            : string.Format(killsTemplate, defeated, required);
    }

    private void HandleBossStarted(string levelId)
    {
        if (levelId != currentLevelId) return;

        if (bossEntry != null) bossEntry.SetActive(true);
        if (bossText != null) bossText.text = bossStartedText;
    }

    private void HandleBossDefeated(string levelId)
    {
        if (levelId != currentLevelId || bossText == null) return;

        bossText.text = bossDoneText;
    }

    private void HandleExitBlocked(string levelId, int remaining)
    {
        if (levelId != currentLevelId) return;

        ShowToast(string.Format(blockedTemplate, remaining));
    }

    private void HandleThresholdReached(string levelId)
    {
        if (levelId != currentLevelId) return;

        ShowToast(unlockedText);
    }

    // Aviso breve, no bloquea al jugador. Si llega otro aviso, reemplaza al anterior.
    public void ShowToast(string message)
    {
        if (toastText == null) return;

        toastText.text = message;

        if (toastRoot != null) toastRoot.SetActive(true);

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToastLater());
    }

    private IEnumerator HideToastLater()
    {
        yield return new WaitForSeconds(toastDuration);

        if (toastRoot != null) toastRoot.SetActive(false);
        toastRoutine = null;
    }
}
