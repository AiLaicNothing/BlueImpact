using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController inputAxisController;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    private void Start()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnGameModeChanged += HandleGameModeChanged;
            Debug.Log("✅ CameraManager suscrito");
        }
    }

    private void OnDisable()
    {
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.OnGameModeChanged -= HandleGameModeChanged;
    }

    private void HandleGameModeChanged(GameMode mode)
    {
        bool isGameplay = mode == GameMode.Gameplay;

        Debug.Log($"🎮 GameMode: {mode} | Movimiento: {(isGameplay ? "ACTIVO" : "BLOQUEADO")}");

        // ✅ DESHABILITAR SOLO EL MOVIMIENTO
        if (inputAxisController != null)
            inputAxisController.enabled = isGameplay;

        if (orbitalFollow != null)
            orbitalFollow.enabled = isGameplay;
    }
}