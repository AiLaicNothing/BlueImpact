using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    private static readonly Dictionary<int, CinemachineCamera> registry = new();

    public static void Register(int id, CinemachineCamera cam)
    {
        if (cam == null) return;

        if (registry.TryGetValue(id, out var existing) &&
            existing != null &&
            existing != cam)
        {
            Debug.LogWarning($"[CameraDirector] Duplicate Camera ID {id}: " +
                $"'{existing.name}' and '{cam.name}'. Keeping the first one.", cam);

            return;
        }

        registry[id] = cam;
    }

    public static void Unregister(int id, CinemachineCamera cam)
    {
        if (registry.TryGetValue(id, out var existing) && existing == cam)
        {
            registry.Remove(id);
        }
    }

    // =========================
    // SETTINGS / STATE
    // =========================

    [Header("Gameplay")]
    [SerializeField]
    private int gameplayPriority = 10;

    [Header("Debug")]
    [SerializeField]
    private bool debug;

    private CinemachineCamera gameplayCamera;

    private CinemachineCamera currentCamera;

    private CameraRequest activeRequest;

    private Coroutine activeRoutine;

    private PlayerControl playerControl;

    private ICinemachineCamera blendTarget;

    private CinemachineBlendDefinition pendingBlend;

    // =========================
    // LIFECYCLE
    // =========================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        CinemachineCore.GetBlendOverride = OnBlendOverride;
    }

    private void OnDisable()
    {
        if (CinemachineCore.GetBlendOverride == OnBlendOverride)
        {
            CinemachineCore.GetBlendOverride = null;
        }
    }

    private void Start()
    {
        // The player does not exist yet (spawned after character select),
        // so wait for the spawn event.
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;

        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest += HandleRequest;
        }
    }

    private void OnDestroy()
    {
        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest -= HandleRequest;
        }

        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerControl player)
    {
        playerControl = player;

        if (debug) Debug.Log("[CameraDirector] PlayerControl received: " + player.gameObject.name);
        
    }

    public void RegisterPlayerCamera(CinemachineCamera cam)
    {
        gameplayCamera = cam;

        gameplayCamera.Priority = gameplayPriority;
    }

    // =========================
    // REQUESTS
    // =========================

    private CinemachineCamera ResolveCamera(CameraRequest request)
    {
        // Direct reference (same scene as the event) — preferred.
        if (request.camera != null) return request.camera;

        // Fallback: ID registered by a CameraID in any loaded scene.
        if (registry.TryGetValue(request.cameraID, out var cam) && cam != null)
            return cam;

        return null;
    }

    private void HandleRequest(CameraRequest request)
    {
        if (request == null) return;

        CinemachineCamera cam = ResolveCamera(request);

        if (cam == null)
        {
            Debug.LogError($"[CameraDirector] Camera not found. Assign 'Camera' in the request, " +
                $"or make sure a CameraID with ID {request.cameraID} is in a loaded scene.");

            return;
        }

        if (activeRoutine != null)
        {
            if (!request.interruptCurrent) return;

            StopCoroutine(activeRoutine);

            // Clean up the interrupted request (camera priority + player state),
            // but don't blend back to gameplay: the new camera takes over.
            if (currentCamera != null)
            {
                currentCamera.Priority = activeRequest.inactivePriority;
            }

            ApplyPlayerState(activeRequest, false);

            activeRoutine = null;
        }

        activeRoutine = StartCoroutine(PlayRoutine(cam, request));
    }

    private IEnumerator PlayRoutine(CinemachineCamera cam, CameraRequest request)
    {
        currentCamera = cam;
        activeRequest = request;

        ApplyPlayerState(request, true);

        if (request.followTarget != null)
        {
            cam.Follow = request.followTarget;
        }

        if (request.lookAtTarget != null)
        {
            cam.LookAt = request.lookAtTarget;
        }

        // Transition IN
        SetNextBlend(cam, request.overrideBlendIn, request.blendIn);

        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = request.inactivePriority;
        }

        cam.Priority = request.activePriority;

        if (request.duration > 0f)
        {
            yield return new WaitForSeconds( request.duration);
        }

        // Transition OUT
        if (request.restoreGameplayCamera)
        {
            SetNextBlend(gameplayCamera, request.overrideBlendOut, request.blendOut);

            RestoreGameplayCamera();
        }

        // The camera may have been destroyed if its Zone scene unloaded.
        if (cam != null)
        {
            cam.Priority = request.inactivePriority;
        }

        ApplyPlayerState(request, false);

        currentCamera = null;
        activeRequest = null;
        activeRoutine = null;
    }

    private void ApplyPlayerState(CameraRequest request, bool cinematic)
    {
        if (request == null) return;

        if (playerControl == null)
        {
            if (cinematic && (request.pausePlayer || request.mutePlayerAudio))
            {
                Debug.LogWarning("[CameraDirector] PlayerControl is null. Was the player spawned?");
            }

            return;
        }

        if (request.pausePlayer)
        {
            if (cinematic) playerControl.LockPlayerControl();
            else playerControl.UnlockPlayerControl();
        }

        if (request.mutePlayerAudio)
        {
            playerControl.MutePlayerAudio(cinematic);
        }
    }

    private void RestoreGameplayCamera()
    {
        if (gameplayCamera == null) return;

        gameplayCamera.Priority = gameplayPriority;
    }

    // =========================
    // BLENDS
    // =========================

    private void SetNextBlend(CinemachineCamera target, bool useOverride, CinemachineBlendDefinition blend)
    {
        if (!useOverride || target == null)
        {
            blendTarget = null;
            return;
        }

        blendTarget = target;
        pendingBlend = blend;
    }

    private CinemachineBlendDefinition OnBlendOverride(ICinemachineCamera fromCam, ICinemachineCamera toCam,
        CinemachineBlendDefinition defaultBlend, Object owner)
    {
        if (blendTarget != null && toCam == blendTarget)
        {
            blendTarget = null; // one-shot

            if (debug)
            {
                Debug.Log($"[CameraDirector] Blend {fromCam?.Name} -> {toCam?.Name}: " +
                          $"{pendingBlend.Style} {pendingBlend.Time}s");
            }

            return pendingBlend;
        }

        // Everything else uses the Brain's Default Blend / Custom Blends asset.
        return defaultBlend;
    }
}