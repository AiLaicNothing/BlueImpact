using System;
using UnityEngine;

public class CameraEventSystem : MonoBehaviour
{
    public static CameraEventSystem Instance;

    public event Action<CameraRequest> OnCameraRequest;

    public event Action<CameraRequest> OnCameraRelease;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Play(CameraRequest request)
    {
        if (request == null) return;

        OnCameraRequest?.Invoke(request);
    }

    // Ends a request early (or ends a "Hold Until Released" request).
    public void Release(CameraRequest request)
    {
        if (request == null) return;

        OnCameraRelease?.Invoke(request);
    }
}
