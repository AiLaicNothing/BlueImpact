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

        // ✅ CONTROLAR CURSOR AUTOMÁTICAMENTE
        UpdateCursorState(newMode);

        OnGameModeChanged?.Invoke(CurrentMode);
    }

    /// <summary>
    /// ✅ Controla la visibilidad y bloqueo del cursor según el GameMode
    /// </summary>
    private void UpdateCursorState(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Gameplay:
                // ❌ Sin mouse, bloqueado al centro
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                Debug.Log("🎮 Cursor bloqueado (Gameplay)");
                break;

            case GameMode.UI:
            case GameMode.Puzzle:
            case GameMode.Dialogue:
            case GameMode.Cutscene:
                // ✅ Con mouse, visible
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("🖱️ Cursor desbloqueado (UI/Menu)");
                break;
        }
    }

    /// <summary>
    /// ✅ Método público para controlar el cursor manualmente si es necesario
    /// </summary>
    public void SetCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode;
    }

    /// <summary>
    /// ✅ Mostrar cursor
    /// </summary>
    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// ✅ Ocultar y bloquear cursor
    /// </summary>
    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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