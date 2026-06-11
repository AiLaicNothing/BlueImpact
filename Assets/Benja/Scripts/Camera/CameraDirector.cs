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