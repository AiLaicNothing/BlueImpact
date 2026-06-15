using System;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }
    public GameMode CurrentMode { get; private set; } = GameMode.Gameplay;
    public event Action<GameMode> OnGameModeChanged;

    // Permite que otros scripts sepan si GameModeManager está listo
    public static bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        // Si ya existe una instancia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsInitialized = true;

        // Opcional: hacer persistente entre escenas
        DontDestroyOnLoad(gameObject);

        Debug.Log("GameModeManager inicializado");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            IsInitialized = false;
        }
    }

    public void SetMode(GameMode newMode)
    {
        if (CurrentMode == newMode)
            return;

        CurrentMode = newMode;
        Debug.Log($"Game Mode → {CurrentMode}");
        OnGameModeChanged?.Invoke(CurrentMode);
    }

    /// <summary>
    /// Método seguro para suscribirse a cambios de modo
    /// </summary>
    public static void SafeSubscribe(Action<GameMode> callback)
    {
        if (Instance != null)
        {
            Instance.OnGameModeChanged += callback;
        }
        else
        {
            Debug.LogWarning("GameModeManager no existe aún. Reintentando...");
            // Esperar un frame y reintentar
            // Esta es una solución temporal
        }
    }
}