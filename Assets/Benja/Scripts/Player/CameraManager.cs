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

    private void Start()
    {
        // ✅ CAMBIO: Suscribirse en Start() en lugar de OnEnable()
        // En Start(), GameModeManager ya está inicializado
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged += HandleGameModeChanged;
            Debug.Log("CameraManager suscrito a GameModeManager");
        }
        else
        {
            Debug.LogError("GameModeManager no encontrado");
        }
    }

    // ✅ CAMBIO: Desuscribirse en OnDisable()
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