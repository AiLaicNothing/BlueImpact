using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }

    [System.Serializable]
    public class CheatDestination
    {
        [Header("Destination")]
        public string destinationName;
        public CheckpointData checkpointData;

        [Header("Input Combination")]
        public Key key = Key.Digit1;
        public bool requireCtrl = true;
        public bool requireShift = true;
        public bool requireAlt = false;
    }

    [Header("Settings")]
    [SerializeField] private bool enableCheats = true;

    [Header("Cheat Destinations")]
    [SerializeField] private List<CheatDestination> destinations = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!enableCheats || Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;

        foreach (CheatDestination destination in destinations)
        {
            if (destination == null || destination.checkpointData == null)
                continue;

            if (IsCombinationPressed(keyboard, destination))
            {
                TeleportToDestination(destination);
                break;
            }
        }
    }

    private static bool IsCombinationPressed(Keyboard keyboard, CheatDestination destination)
    {
        if (destination.key == Key.None)
            return false;

        var keyControl = keyboard[destination.key];
        if (keyControl == null || !keyControl.wasPressedThisFrame)
            return false;

        bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;

        // The modifier combination must match exactly.
        return ctrl == destination.requireCtrl &&
               shift == destination.requireShift &&
               alt == destination.requireAlt;
    }

    // Also callable from a debug UI Button or the Inspector through a UnityEvent.
    public void TeleportToDestination(int index)
    {
        if (!enableCheats)
            return;

        if (index < 0 || index >= destinations.Count)
        {
            Debug.LogWarning("[Cheat] Invalid destination index.");
            return;
        }

        TeleportToDestination(destinations[index]);
    }

    private void TeleportToDestination(CheatDestination destination)
    {
        if (!enableCheats || destination == null)
            return;

        if (destination.checkpointData == null)
        {
            Debug.LogError("[Cheat] CheckpointData is missing.");
            return;
        }

        if (Teleport_Manager.Instance == null)
        {
            Debug.LogError("[Cheat] Teleport_Manager is missing.");
            return;
        }

        if (Teleport_Manager.Instance.IsTeleporting)
            return;

        Debug.Log($"[Cheat] Teleporting to '{destination.destinationName}'.");

        // CheckpointData overload: no DiscoveredCheckpoint constructor change required.
        // isCheat=true means the active respawn checkpoint is NOT saved/changed.
        Teleport_Manager.Instance.Teleport(destination.checkpointData, isCheat: true);
    }
}
