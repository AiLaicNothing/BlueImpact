using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 🎮 InputRebindingManager - Gestión de remapeo de controles
/// 
/// ✅ Protección anti-duplicados: impide asignar una tecla ya en uso
/// ✅ Reset a defaults del diseño original del juego
/// ✅ Persistencia en PlayerPrefs
/// </summary>
public class InputRebindingManager : MonoBehaviour
{
    public static InputRebindingManager Instance { get; private set; }

    private InputActionAsset inputActions;
    // Snapshot de los bindings originales del juego para poder resetear
    private string defaultBindingsSnapshot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadInputActions();
    }

    private void LoadInputActions()
    {
        inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

        if (inputActions == null)
        {
            var allAssets = Resources.LoadAll<InputActionAsset>("");
            if (allAssets.Length > 0)
            {
                inputActions = allAssets[0];
                Debug.Log("InputActionAsset encontrado: " + inputActions.name);
            }
        }

        if (inputActions == null)
        {
            Debug.LogError("No se pudo cargar InputSystem_Actions");
            return;
        }

        // ✅ GUARDAR SNAPSHOT DE DEFAULTS ANTES DE CARGAR OVERRIDES GUARDADOS
        // Esto captura exactamente como diseñamos el juego originalmente
        defaultBindingsSnapshot = inputActions.SaveBindingOverridesAsJson();

        inputActions.Enable();
        LoadRebindings();
    }

    // ==================== REBINDING ====================

    /// <summary>
    /// Remapea una acción con protección anti-duplicados.
    /// Si la tecla presionada ya está asignada a otra acción, cancela y avisa.
    /// Retorna (success, conflictMessage).
    /// </summary>
    public async Task<(bool success, string conflictMessage)> RemapActionAsync(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Acción no encontrada: {actionName}");
            return (false, null);
        }

        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape");

        bool success = false;
        string conflictMessage = null;
        bool completed = false;

        rebind.OnComplete(operation =>
        {
            string newPath = action.bindings[bindingIndex].effectivePath;

            // ✅ VERIFICAR DUPLICADO
            string conflict = FindConflict(actionName, bindingIndex, newPath);

            if (conflict != null)
            {
                // Revertir el binding recién asignado
                action.RemoveBindingOverride(bindingIndex);
                conflictMessage = conflict;
                success = false;
                Debug.LogWarning($"⚠️ Conflicto: '{newPath}' ya está asignado a '{conflict}'");
            }
            else
            {
                success = true;
                SaveRebindings();
                Debug.Log($"✅ Remapeo completado: {actionName}[{bindingIndex}] → {newPath}");
            }

            operation.Dispose();
            action.Enable();
            completed = true;
        });

        rebind.OnCancel(operation =>
        {
            Debug.Log($"Remapeo cancelado para {actionName}");
            operation.Dispose();
            action.Enable();
            completed = true;
        });

        rebind.Start();

        while (!completed)
            await Task.Delay(10);

        return (success, conflictMessage);
    }

    /// <summary>
    /// Busca si una ruta (path) ya está asignada a otra acción/binding.
    /// Retorna el nombre de la acción en conflicto, o null si no hay conflicto.
    /// </summary>
    private string FindConflict(string currentActionName, int currentBindingIndex, string newPath)
    {
        if (string.IsNullOrEmpty(newPath)) return null;

        foreach (var map in inputActions.actionMaps)
        {
            foreach (var otherAction in map.actions)
            {
                string fullName = $"{map.name}/{otherAction.name}";

                for (int i = 0; i < otherAction.bindings.Count; i++)
                {
                    // Saltar el binding que estamos modificando
                    if (fullName == currentActionName && i == currentBindingIndex)
                        continue;

                    // Saltar composite parts que son parte del mismo composite
                    if (otherAction.bindings[i].isPartOfComposite)
                        continue;

                    string existingPath = otherAction.bindings[i].effectivePath;
                    if (string.Equals(existingPath, newPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{map.name}/{otherAction.name}";
                    }
                }
            }
        }

        return null;
    }

    // ==================== RESET ====================

    /// <summary>
    /// Resetea TODOS los bindings al diseño original del juego.
    /// Usa el snapshot guardado al arrancar, antes de cargar overrides del jugador.
    /// </summary>
    public void ResetAllBindingsToDefault()
    {
        if (inputActions == null) return;

        foreach (var actionMap in inputActions.actionMaps)
            foreach (var action in actionMap.actions)
                action.RemoveAllBindingOverrides();

        // Si existía un snapshot, restaurarlo (por si los defaults incluían overrides de diseño)
        if (!string.IsNullOrEmpty(defaultBindingsSnapshot))
            inputActions.LoadBindingOverridesFromJson(defaultBindingsSnapshot);

        // Borrar los overrides guardados del jugador
        PlayerPrefs.DeleteKey("InputRebindings");
        PlayerPrefs.Save();

        Debug.Log("✅ Todos los bindings reseteados al diseño original del juego");
    }

    /// <summary>
    /// Resetea una acción específica a su binding por defecto.
    /// </summary>
    public void ResetActionBinding(string actionName)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null) return;

        action.RemoveAllBindingOverrides();
        SaveRebindings();
        Debug.Log($"✅ Binding reseteado para {actionName}");
    }

    // ==================== SAVE & LOAD ====================

    public void SaveRebindings()
    {
        if (inputActions == null) return;
        var rebindData = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputRebindings", rebindData);
        PlayerPrefs.Save();
        Debug.Log("Rebindings guardados");
    }

    public void LoadRebindings()
    {
        if (inputActions == null) return;
        string rebindData = PlayerPrefs.GetString("InputRebindings", "");

        if (!string.IsNullOrEmpty(rebindData))
        {
            try
            {
                inputActions.LoadBindingOverridesFromJson(rebindData);
                Debug.Log("Rebindings cargados");
            }
            catch
            {
                Debug.LogWarning("Error al cargar rebindings, usando valores por defecto");
            }
        }
    }

    // ==================== HELPERS ====================

    public string GetBindingPath(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count)
            return "";
        return action.bindings[bindingIndex].effectivePath;
    }

    public static string GetDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "No asignado";

        path = path.Replace("<Keyboard>/", "")
                   .Replace("<Mouse>/", "")
                   .Replace("<Gamepad>/", "")
                   .Replace("/", "");

        var nameMap = new Dictionary<string, string>
        {
            { "w", "W" }, { "a", "A" }, { "s", "S" }, { "d", "D" },
            { "space", "Espacio" },
            { "leftShift", "Shift" }, { "rightShift", "Shift Der" },
            { "leftCtrl", "Ctrl" }, { "rightCtrl", "Ctrl Der" },
            { "leftAlt", "Alt" }, { "rightAlt", "Alt Der" },
            { "enter", "Enter" }, { "escape", "Esc" },
            { "e", "E" }, { "f", "F" }, { "r", "R" }, { "q", "Q" },
            { "1", "1" }, { "2", "2" }, { "3", "3" }, { "4", "4" },
            { "leftButton", "Click Izq" }, { "rightButton", "Click Der" },
            { "buttonWest", "X (Gamepad)" }, { "buttonSouth", "A (Gamepad)" },
            { "buttonNorth", "Y (Gamepad)" }, { "buttonEast", "B (Gamepad)" },
            { "leftStickPress", "L3" }, { "rightStickPress", "R3" },
            { "leftStick", "L-Stick" }, { "rightStick", "R-Stick" },
            { "leftShoulder", "LB" }, { "rightShoulder", "RB" },
            { "leftTrigger", "LT" }, { "rightTrigger", "RT" },
            { "start", "Start" }, { "select", "Select" },
        };

        return nameMap.ContainsKey(path) ? nameMap[path] : path;
    }

    public List<string> GetAllActions()
    {
        List<string> actions = new();
        if (inputActions == null) return actions;

        foreach (var map in inputActions.actionMaps)
            foreach (var action in map.actions)
                actions.Add($"{map.name}/{action.name}");

        return actions;
    }

    public InputActionAsset GetInputActions() => inputActions;
}