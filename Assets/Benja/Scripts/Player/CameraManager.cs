using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private CinemachineInputAxisController axisController;

    private void Awake()
    {
        axisController =
            FindFirstObjectByType<CinemachineInputAxisController>();
    }

    private void OnEnable()
    {
        GameModeManager.Instance.OnGameModeChanged += HandleGameModeChanged;
    }

    private void OnDisable()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged -= HandleGameModeChanged;
        }
    }

    private void HandleGameModeChanged(GameMode mode)
    {
        if (axisController == null)
            return;

        axisController.enabled =
            mode == GameMode.Gameplay;
    }
}