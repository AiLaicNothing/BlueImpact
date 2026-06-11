using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

using System;
using System.Xml.Linq;


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

    public enum LogicMode
    {
        AND,
        OR
    }

    [Header("Legacy Logic")]
    public LogicMode logicMode = LogicMode.AND;

    [Header("Advanced Requirements")]
    [SerializeField]
    private List<ActivatorRequirement> requirements = new();

    // =====================================================
    // IMPORTANTE:
    // Cambiado de MonoBehaviour -> PuzzleDoor
    // para evitar problemas de cast con interfaces.
    // =====================================================

    [Header("Targets")]
    [SerializeField]
    private List<PuzzleDoor> targets = new();

    [Header("Camera")]
#if CINEMACHINE_V3 || CINEMACHINE_3_0_0_OR_NEWER
    [SerializeField] private CinemachineCamera puzzleCamera;
#else
    [SerializeField] private CinemachineVirtualCameraBase puzzleCamera;
#endif

    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private float cameraDuration = 2.5f;
    [SerializeField] private bool cameraTriggerOnlyOnce = true;

    // =====================================================
    // LEGACY SYSTEM
    // =====================================================

    private readonly List<IActivator> _activators = new();

    // =====================================================

    private bool _currentState = false;

    public bool IsActive => _currentState;

    private bool _cameraHasTriggered = false;

    private Coroutine _cameraRoutine;

    private void Awake()
    {
        _currentState = false;
        Debug.Log(
            $"[PuzzleReceiver] Requirements count = {requirements.Count}");

        if (puzzleCamera != null)
            puzzleCamera.Priority = inactivePriority;

        //// Convert MonoBehaviour -> IActivator
        //foreach (var req in requirements)
        //{
        //    Debug.Log(
        //        $"[PuzzleReceiver] Requirement loaded -> " +
        //        $"{req.activatorObject}");


        //    if (req.activator == null)
        //    {
        //        Debug.LogWarning(
        //            $"[PuzzleReceiver] {req.activatorObject} NO implementa IActivator");
        //    }
        //}

        //Debug.Log(
        //    $"[PuzzleReceiver] Targets count = {targets.Count}");

        //foreach (var target in targets)
        //{
        //    Debug.Log(
        //        $"[PuzzleReceiver] Target loaded -> {target}");
        //}
    }

    // =====================================================
    // LEGACY SUPPORT
    // =====================================================

    public void RegisterActivator(IActivator activator)
    {
        if (!_activators.Contains(activator))
            _activators.Add(activator);
    }

    // =====================================================

    public void Evaluate()
    {


        bool shouldBeActive;

        // =====================================================
        // ADVANCED REQUIREMENTS SYSTEM
        // =====================================================

        if (requirements.Count > 0)
        {
            Debug.Log(
                "[PuzzleReceiver] USING ADVANCED SYSTEM");

            shouldBeActive = true;

            foreach (var req in requirements)
            {
                if (req.activatorObject == null)
                {
                    Debug.LogWarning(
                        "[PuzzleReceiver] ActivatorObject NULL");

                    shouldBeActive = false;
                    break;
                }

                IActivator activator =
                    req.activatorObject.GetComponent<IActivator>();

                if (activator == null)
                {
                    Debug.LogWarning(
                        $"[PuzzleReceiver] {req.activatorObject.name} NO implementa IActivator");

                    shouldBeActive = false;
                    break;
                }

                bool currentState = activator.IsActive;

                Debug.Log(
                    $"[PuzzleReceiver] " +
                    $"{req.activatorObject.name} -> " +
                    $"Current={currentState} | " +
                    $"Required={req.requiredState}");

                if (currentState != req.requiredState)
                {
                    Debug.Log(
                        $"[PuzzleReceiver] FAIL -> " +
                        $"{req.activatorObject.name}");

                    shouldBeActive = false;
                    break;
                }
            }
        }

        // =====================================================
        // LEGACY SYSTEM
        // =====================================================

        else
        {
            Debug.Log(
                "[PuzzleReceiver] USING LEGACY SYSTEM");

            shouldBeActive = logicMode switch
            {
                LogicMode.AND =>
                    _activators.TrueForAll(a => a.IsActive),

                LogicMode.OR =>
                    _activators.Exists(a => a.IsActive),

                _ => false
            };
        }

        // =====================================================

        Debug.Log(
            $"[PuzzleReceiver] shouldBeActive = {shouldBeActive}");

        if (shouldBeActive == _currentState)
        {
            Debug.Log(
                "[PuzzleReceiver] Estado no cambió");

            return;
        }

        _currentState = shouldBeActive;

        Debug.Log(
            $"[PuzzleReceiver] Nuevo estado -> {_currentState}");

        Debug.Log(
            $"[PuzzleReceiver] Activando targets...");

        foreach (var target in targets)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    "[PuzzleReceiver] Target NULL");

                continue;
            }

            Debug.Log(
                $"[PuzzleReceiver] Target -> {target.name}");

            if (_currentState)
            {
                Debug.Log(
                    $"[PuzzleReceiver] Activate -> {target.name}");

                target.Activate();
            }
            else
            {
                Debug.Log(
                    $"[PuzzleReceiver] Deactivate -> {target.name}");

                target.Deactivate();
            }
        }

        if (_currentState)
        {
            Debug.Log(
                "[PuzzleReceiver] Triggering camera");

            TriggerCamera();
        }
    }

    private void TriggerCamera()
    {
        if (puzzleCamera == null)
            return;

        if (cameraTriggerOnlyOnce &&
            _cameraHasTriggered)
            return;

        _cameraHasTriggered = true;

        if (_cameraRoutine != null)
            StopCoroutine(_cameraRoutine);

        _cameraRoutine =
            StartCoroutine(CameraRoutine());
    }

    private System.Collections.IEnumerator CameraRoutine()
    {
        puzzleCamera.Priority = activePriority;

        yield return new WaitForSeconds(cameraDuration);

        puzzleCamera.Priority = inactivePriority;

        _cameraRoutine = null;
    }

    /// <summary>
    /// Restauración directa desde save.
    /// SOLO SERVIDOR.
    /// </summary>
    public void SetStateDirectly(bool active)
    {


        _currentState = active;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            if (_currentState)
                target.Activate();
            else
                target.Deactivate();
        }

        Debug.Log(
            $"[PuzzleReceiver] Estado restaurado: {_currentState}");
    }
}