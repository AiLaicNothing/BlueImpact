using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    [Header("Gameplay")]
    [SerializeField]
    private int gameplayPriority = 10;

    [Header("Debug")]
    [SerializeField]
    private bool debug;

    private readonly Dictionary<int, CinemachineCamera>
        cinematicCameras = new();

    private CinemachineCamera gameplayCamera;

    private CinemachineCamera currentCamera;

    private Coroutine activeRoutine;

    private PlayerControl playerControl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RegisterAllCameras();

        // 🎮 IMPORTANTE: NO buscar PlayerControl aquí
        // El jugador NO existe aún (se instancia después del selector de personajes)
        // En su lugar, suscribirse al evento de spawn
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
            Debug.Log("[CameraDirector] ✅ Suscrito al evento OnPlayerSpawned");
        }
        else
        {
            Debug.LogWarning("[CameraDirector] ⚠️ PlayerSpawn_Manager no encontrado");
        }

        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest +=
                HandleRequest;
        }
    }

    private void OnDestroy()
    {
        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest -=
                HandleRequest;
        }

        // Desuscribirse del evento
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        }
    }

    /// <summary>
    /// Se ejecuta cuando el jugador es instanciado (después de seleccionar personaje)
    /// </summary>
    private void OnPlayerSpawned(PlayerControl player)
    {
        playerControl = player;
        Debug.Log("[CameraDirector] ✅ PlayerControl obtenido después del spawn: " + player.gameObject.name);
    }

    public void RegisterPlayerCamera(
        CinemachineCamera cam)
    {
        gameplayCamera = cam;

        gameplayCamera.Priority =
            gameplayPriority;
    }

    private void RegisterAllCameras()
    {
        CameraID[] cameras =
            FindObjectsByType<CameraID>(
                FindObjectsSortMode.None);

        foreach (var cam in cameras)
        {
            if (cam == null)
                continue;

            if (cam.Camera == null)
                continue;

            if (cinematicCameras.ContainsKey(cam.ID))
            {
                Debug.LogWarning(
                    $"Duplicate Camera ID: {cam.ID}");

                continue;
            }

            cinematicCameras.Add(
                cam.ID,
                cam.Camera);

            if (debug)
            {
                Debug.Log(
                    $"Registered Camera {cam.ID}");
            }
        }
    }

    private void HandleRequest(
        CameraRequest request)
    {
        if (request == null)
            return;

        if (!cinematicCameras.TryGetValue(
            request.cameraID,
            out var cam))
        {
            Debug.LogError(
                $"Camera not found: {request.cameraID}");

            return;
        }

        if (activeRoutine != null)
        {
            if (!request.interruptCurrent)
                return;

            StopCoroutine(activeRoutine);

            RestoreGameplayCamera();

            if (currentCamera != null)
            {
                currentCamera.Priority =
                    request.inactivePriority;
            }
        }

        activeRoutine =
            StartCoroutine(
                PlayRoutine(cam, request));
    }

    private IEnumerator PlayRoutine(
        CinemachineCamera cam,
        CameraRequest request)
    {
        currentCamera = cam;

        // 🎮 Pausa al jugador si está configurado
        if (request.pausePlayer && playerControl != null)
        {
            playerControl.LockPlayerControl();
            Debug.Log("[CameraDirector] ✅ Jugador pausado");
        }
        else if (request.pausePlayer && playerControl == null)
        {
            Debug.LogError("[CameraDirector] ❌ pausePlayer activado pero PlayerControl es NULL. ¿El jugador fue instanciado?");
        }

        // 🔇 Silencia audio del jugador si está configurado
        if (request.mutePlayerAudio && playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[CameraDirector] ✅ Audio del jugador silenciado");
        }

        if (request.followTarget != null)
        {
            cam.Follow =
                request.followTarget;
        }

        if (request.lookAtTarget != null)
        {
            cam.LookAt =
                request.lookAtTarget;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.Priority =
                request.inactivePriority;
        }

        cam.Priority =
            request.activePriority;

        if (request.duration > 0f)
        {
            yield return new WaitForSeconds(
                request.duration);
        }

        cam.Priority =
            request.inactivePriority;

        // 🎮 Reanudar control del jugador si estaba pausado
        if (request.pausePlayer && playerControl != null)
        {
            playerControl.UnlockPlayerControl();
            Debug.Log("[CameraDirector] ✅ Jugador desbloqueado");
        }

        // 🔊 Reactivar audio del jugador si estaba silenciado
        if (request.mutePlayerAudio && playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[CameraDirector] ✅ Audio del jugador reactivado");
        }

        if (request.restoreGameplayCamera)
        {
            RestoreGameplayCamera();
        }

        currentCamera = null;

        activeRoutine = null;
    }

    private void RestoreGameplayCamera()
    {
        if (gameplayCamera == null)
            return;

        gameplayCamera.Priority =
            gameplayPriority;
    }
}