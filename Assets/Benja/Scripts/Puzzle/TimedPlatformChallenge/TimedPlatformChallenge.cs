using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedPlatformChallenge : MonoBehaviour
{
    public enum ChallengeState
    {
        Idle,
        Running,
        Completed
    }

    [Header("Persistence")]
    [SerializeField] private bool persistent = true;

    [SerializeField] private string challengeID;

    public bool Persistent => persistent;
    public string ChallengeID => challengeID;

    [Header("Rewards")]
    [SerializeField]
    private List<ChallengeDoor> rewardDoors = new();

    [Header("Blocking Doors")]
    [Tooltip("Puertas que bloquean una entrada mientras el puzzle está activo. " +
             "Posiciona la puerta en la escena en su posición ABIERTA. " +
             "El 'Open Point' de cada ChallengeDoor debe apuntar a la posición BLOQUEADA " +
             "(a donde baja al iniciar el puzzle).")]
    [SerializeField]
    private List<ChallengeDoor> blockingDoors = new();

    [Header("Challenge")]
    [SerializeField] private float challengeDuration = 30f;

    [Header("Platform Hide")]
    [SerializeField] private float platformHideDelay = 12f;

    [Header("Platforms")]
    [SerializeField]
    private List<ChallengePlatform> platforms = new();

    [Header("Cameras")]
    [SerializeField]
    private CameraRequest startCamera;

    [SerializeField]
    private CameraRequest completeCamera;

    [Header("UI")]
    [SerializeField]
    private string challengeName = "DESAFÍO";

    [SerializeField]
    private float platformSpawnDelay = 0.15f;

    private float _remainingTime;

    private ChallengeState _state =
        ChallengeState.Idle;

    private Coroutine _timerRoutine;

    private Coroutine _hideRoutine;

    public bool PersistentChallenge => persistent;

    public bool IsCompleted =>
        _state == ChallengeState.Completed;

    public bool IsRunning =>
        _state == ChallengeState.Running;

    public float RemainingTime =>
        _remainingTime;

    public ChallengeState State =>
        _state;

    //====================================================
    // RESTORE
    //====================================================

    public void RestoreCompletedState()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        _remainingTime = 0f;

        _state = ChallengeState.Completed;

        HideUI();

        foreach (var door in blockingDoors)
        {
            if (door == null)
                continue;

            door.SetClosedState(); // Instantáneo: posición inicial (abierta)
        }

        foreach (var platform in platforms)
        {
            if (platform == null)
                continue;

            platform.Show();
        }

        Debug.Log(
            $"[Challenge] Restaurado como completado: {challengeID}");
    }

    //====================================================
    // START
    //====================================================

    public void StartChallenge()
    {
        if (_state == ChallengeState.Completed)
            return;

        if (_state != ChallengeState.Idle)
            return;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        StartCoroutine(StartChallengeRoutine());
    }

    private IEnumerator StartChallengeRoutine()
    {
        if (startCamera != null)
        {
            CameraEventRelay.Instance?.Play(startCamera);
        }

        foreach (var door in blockingDoors)
        {
            if (door == null)
                continue;

            door.Open(); // "Open" mueve la puerta hacia openPoint, que en este caso es la posición de BLOQUEO
        }

        foreach (var platform in platforms)
        {
            if (platform == null)
                continue;

            platform.Show();

            yield return new WaitForSeconds(
                platformSpawnDelay);
        }

        float introDuration =
            startCamera != null
            ? startCamera.duration
            : 0f;

        yield return new WaitForSeconds(
            introDuration + 0.1f);

        _state = ChallengeState.Running;

        ShowUI();

        _remainingTime = challengeDuration;

        _timerRoutine =
            StartCoroutine(TimerRoutine());

        Debug.Log(
            $"[Challenge] Iniciado: {challengeID}");
    }

    //====================================================
    // TIMER
    //====================================================

    private IEnumerator TimerRoutine()
    {
        while (_remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;

            yield return null;
        }

        _remainingTime = 0f;

        Timeout();
    }

    private void Timeout()
    {
        Debug.Log("[Challenge] Timeout");

        ResetChallenge();
    }

    //====================================================
    // RESET (Timeout, caída del jugador, etc.)
    //====================================================

    public void ResetChallenge()
    {
        // ✅ NUEVO: No resetear si ya fue completado
        if (_state == ChallengeState.Completed)
        {
            Debug.Log($"[Challenge] {challengeID} ya fue completado — no se resetea");
            return;
        }

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        _remainingTime = 0f;

        _state = ChallengeState.Idle;

        HideUI();

        foreach (var door in blockingDoors)
        {
            if (door == null)
                continue;

            door.Close(); // Regresa a su posición inicial (abierta)
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
        }

        _hideRoutine =
            StartCoroutine(HidePlatformsRoutine());
    }

    //====================================================
    // COMPLETE
    //====================================================

    public void CompleteChallenge()
    {
        if (_state != ChallengeState.Running)
            return;

        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        _remainingTime = 0f;

        _state = ChallengeState.Completed;

        HideUI();

        foreach (var door in blockingDoors)
        {
            if (door == null)
                continue;

            // Usa SetCompletionState para respetar la configuración de cada puerta
            door.SetCompletionState();
        }

        if (completeCamera != null)
        {
            CameraEventRelay.Instance?.Play(completeCamera);
        }

        foreach (var door in rewardDoors)
        {
            if (door == null)
                continue;

            door.Open();
        }

        Debug.Log($"[Challenge] Completed: {challengeID}");
    }

    //====================================================
    // PLATFORMS
    //====================================================

    private IEnumerator HidePlatformsRoutine()
    {
        yield return new WaitForSeconds(
            platformHideDelay);

        foreach (var platform in platforms)
        {
            if (platform == null)
                continue;

            platform.Hide();
        }

        _hideRoutine = null;
    }

    //====================================================
    // UI
    //====================================================

    private void ShowUI()
    {
        ChallengeUI.Instance?.Show(
            challengeName,
            this);
    }

    private void HideUI()
    {
        ChallengeUI.Instance?.Hide();
    }
}