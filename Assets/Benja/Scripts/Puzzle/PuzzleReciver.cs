using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class PuzzleReceiver : MonoBehaviour
{
    [System.Serializable]
    public class ActivatorRequirement
    {
        [Header("Activator")]
        public MonoBehaviour activatorObject;

        [Header("Required State")]
        public bool requiredState = true;
    }

    [Header("Save System")]
    [SerializeField] private string receiverID;
    public string ReceiverID => receiverID;

    public enum LogicMode { AND, OR }

    [Header("Legacy Logic")]
    public LogicMode logicMode = LogicMode.AND;

    [Header("Advanced Requirements")]
    [SerializeField] private List<ActivatorRequirement> requirements = new();

    [Header("Targets")]
    [SerializeField] private List<PuzzleDoor> targets = new();

    [Header("Camera - On Activation")]
    [SerializeField] private bool useCameraOnActivation = true;
    [SerializeField] private CameraRequest activationCamera;

    [Header("Fallo y Cooldown")]
    [SerializeField] private float failCooldownDuration = 3f;

    private readonly List<IActivator> _activators = new();
    private readonly List<Lever> _levers = new();
    private bool _currentState = false;

    public bool IsActive => _currentState;

    private void Awake()
    {
        _currentState = false;
        Debug.Log($"[PuzzleReceiver] Requirements count = {requirements.Count}");
    }

    public void RegisterActivator(IActivator activator)
    {
        if (!_activators.Contains(activator))
            _activators.Add(activator);

        // Guardamos referencia a Lever para poder resetear/cooldown
        if (activator is Lever lever && !_levers.Contains(lever))
            _levers.Add(lever);
    }

    public void Evaluate()
    {
        // ── ADVANCED REQUIREMENTS ──────────────────────────────────────────
        if (requirements.Count > 0)
        {
            Debug.Log("[PuzzleReceiver] USING ADVANCED SYSTEM");

            bool puzzleSolved = true;

            foreach (var req in requirements)
            {
                if (req.activatorObject == null)
                {
                    Debug.LogWarning("[PuzzleReceiver] ActivatorObject NULL");
                    puzzleSolved = false;
                    break;
                }

                IActivator activator = req.activatorObject.GetComponent<IActivator>();
                if (activator == null)
                {
                    Debug.LogWarning($"[PuzzleReceiver] {req.activatorObject.name} NO implementa IActivator");
                    puzzleSolved = false;
                    break;
                }

                bool current = activator.IsActive;
                bool required = req.requiredState;

                Debug.Log($"[PuzzleReceiver] {req.activatorObject.name} -> Current={current} | Required={required}");

                if (current != required)
                {
                    // Solo falla si la palanca está ACTIVA y no debería
                    // (fallo inmediato al encender algo incorrecto)
                    if (current == true && required == false)
                    {
                        Debug.Log($"[PuzzleReceiver] FALLO INMEDIATO -> {req.activatorObject.name} activa sin ser requerida");
                        TriggerFail();
                        return;
                    }

                    puzzleSolved = false;
                    // No es fallo aún: simplemente el puzzle no está completo
                }
            }

            if (!puzzleSolved)
            {
                Debug.Log("[PuzzleReceiver] Puzzle incompleto (sin fallo)");
                return;
            }

            // Todas las condiciones se cumplen → resolver
            ResolvePuzzle();
        }

        // ── LEGACY SYSTEM ─────────────────────────────────────────────────
        else
        {
            Debug.Log("[PuzzleReceiver] USING LEGACY SYSTEM");

            bool shouldBeActive = logicMode switch
            {
                LogicMode.AND => _activators.TrueForAll(a => a.IsActive),
                LogicMode.OR => _activators.Exists(a => a.IsActive),
                _ => false
            };

            if (shouldBeActive == _currentState) return;

            _currentState = shouldBeActive;
            ApplyTargets();

            if (_currentState) TriggerCamera();
        }
    }

    // ── RESOLVER PUZZLE ───────────────────────────────────────────────────

    private void ResolvePuzzle()
    {
        if (_currentState) return; // Ya estaba resuelto

        _currentState = true;
        Debug.Log("[PuzzleReceiver] ¡Puzzle resuelto!");

        // Bloquear palancas permanentemente — el puzzle ya no es interactuable
        foreach (var lever in _levers)
            lever.SetCooldown(true);

        ApplyTargets();
        TriggerCamera();
    }

    // ── FALLO ─────────────────────────────────────────────────────────────

    private void TriggerFail()
    {
        Debug.Log($"[PuzzleReceiver] Fail → reseteando palancas. Cooldown: {failCooldownDuration}s");
        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        // 1. Bloquear todas las palancas
        foreach (var lever in _levers)
            lever.SetCooldown(true);

        // 2. Resetear estado visual y lógico de cada palanca
        foreach (var lever in _levers)
            lever.ResetState();

        // 3. Esperar cooldown
        yield return new WaitForSeconds(failCooldownDuration);

        // 4. Desbloquear
        foreach (var lever in _levers)
            lever.SetCooldown(false);

        Debug.Log("[PuzzleReceiver] Cooldown terminado. Palancas disponibles.");
    }

    // ── HELPERS ───────────────────────────────────────────────────────────

    private void ApplyTargets()
    {
        foreach (var target in targets)
        {
            if (target == null) { Debug.LogWarning("[PuzzleReceiver] Target NULL"); continue; }

            if (_currentState) target.Activate();
            else target.Deactivate();
        }
    }

    private void TriggerCamera()
    {
        if (!useCameraOnActivation) return;

        if (activationCamera == null)
        {
            Debug.LogWarning("[PuzzleReceiver] Activation camera asignada pero NULL");
            return;
        }

        if (CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(activationCamera);
            Debug.Log("[PuzzleReceiver] Camera played");
        }
    }

    public void SetStateDirectly(bool active)
    {
        _currentState = active;
        ApplyTargets();
        Debug.Log($"[PuzzleReceiver] Estado restaurado: {_currentState}");
    }
}