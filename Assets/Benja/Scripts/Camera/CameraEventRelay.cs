using UnityEngine;

public class CameraEventRelay : MonoBehaviour
{
    public static CameraEventRelay Instance;

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
        if (request == null)
            return;

        if (CameraEventSystem.Instance == null)
        {
            Debug.LogError(
                "[CameraEventRelay] CameraEventSystem missing.");

            return;
        }

        CameraEventSystem.Instance.Play(request);
    }
}