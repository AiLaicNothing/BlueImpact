using System;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public GameMode CurrentMode { get; private set; } = GameMode.Gameplay;

    public event Action<GameMode> OnGameModeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetMode(GameMode newMode)
    {
        if (CurrentMode == newMode)
            return;

        CurrentMode = newMode;

        Debug.Log($"Game Mode → {CurrentMode}");

        OnGameModeChanged?.Invoke(CurrentMode);
    }
}