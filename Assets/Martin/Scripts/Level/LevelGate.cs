using UnityEngine;

// Compuerta de salida del nivel.
// - Este GameObject tiene un Collider isTrigger: aviso al acercarse ("Derrota N enemigos más").
// - blocker: Collider sólido (no trigger) que impide el paso. Se desactiva al cumplir el umbral.
[RequireComponent(typeof(Collider))]
public class LevelGate : MonoBehaviour
{
    [SerializeField] private LevelConfig config;

    [Tooltip("Collider sólido que bloquea la salida. Se desactiva al abrir.")]
    [SerializeField] private Collider blocker;

    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject openVisual;

    [Tooltip("Cámara que muestra la compuerta cuando se abre en vivo. Opcional. " +
             "Usar un CameraID de la escena y apuntar followTarget/lookAtTarget a esta compuerta.")]
    [SerializeField] private CameraRequest openRequest;

    private bool isOpen;
    private LevelObjectiveManager subscribed;

    private void Start()
    {
        LevelObjectiveManager manager = LevelObjectiveManager.Instance;

        if (manager == null || config == null)
        {
            Debug.LogWarning($"[LevelGate] {name}: falta LevelObjectiveManager en la escena o LevelConfig.", this);
            SetVisuals(false);
            return;
        }

        subscribed = manager;
        manager.OnThresholdReached += HandleThreshold;

        // Al cargar una partida ya desbloqueada no se reproduce la cámara.
        if (manager.IsUnlocked(config.LevelId)) Open(false);
        else SetVisuals(false);
    }

    private void OnDestroy()
    {
        if (subscribed != null) subscribed.OnThresholdReached -= HandleThreshold;
    }

    private void HandleThreshold(string levelId)
    {
        if (config != null && levelId == config.LevelId) Open(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen || config == null || !LevelZone.IsRealPlayer(other)) return;

        LevelObjectiveManager.Instance?.NotifyExitBlocked(config.LevelId);
    }

    private void Open(bool playEffects)
    {
        if (isOpen) return;

        isOpen = true;
        if (blocker != null) blocker.enabled = false;
        SetVisuals(true);

        if (playEffects && openRequest != null && CameraEventRelay.Instance != null)
            CameraEventRelay.Instance.Play(openRequest);
    }

    private void SetVisuals(bool open)
    {
        if (lockedVisual != null) lockedVisual.SetActive(!open);
        if (openVisual != null) openVisual.SetActive(open);
    }
}
