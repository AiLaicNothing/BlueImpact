using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InputRebindingManager : MonoBehaviour
{
    public static InputRebindingManager Instance { get; private set; }

    private InputActionAsset inputActions;
    private Dictionary<string, string> rebindingOverrides = new();

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

        inputActions.Enable();
        LoadRebindings();
    }

    // ==================== REBINDING ====================

    /// <summary>
    /// ✅ ARREGLADO: Deshabilita la acción antes de hacer rebinding
    /// </summary>
    public async Task<bool> RemapActionAsync(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Acción no encontrada: {actionName}");
            return false;
        }

        // ✅ DESHABILITAR ANTES
        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex);

        bool success = false;

        rebind.OnComplete(operation =>
        {
            Debug.Log($"Remapeo completado para {actionName}: {action.bindings[bindingIndex].effectivePath}");
            success = true;
            SaveRebindings();
            operation.Dispose();

            // ✅ REHABILITAR AL COMPLETAR
            action.Enable();
        });

        rebind.OnCancel(operation =>
        {
            Debug.Log($"Remapeo cancelado para {actionName}");
            operation.Dispose();

            // ✅ REHABILITAR SI SE CANCELA
            action.Enable();
        });

        rebind.Start();

        while (rebind.completed == false)
        {
            await Task.Delay(10);
        }

        return success;
    }

    /// <summary>
    /// Obtiene todas las acciones del Input System
    /// </summary>
    public List<string> GetAllActions()
    {
        List<string> actions = new();

        if (inputActions == null) return actions;

        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                actions.Add($"{map.name}/{action.name}");
            }
        }

        return actions;
    }

    /// <summary>
    /// Obtiene los bindings de una acción específica
    /// </summary>
    public List<string> GetActionBindingPaths(string actionName)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null) return new();

        List<string> paths = new();
        foreach (var binding in action.bindings)
        {
            paths.Add(binding.effectivePath);
        }

        return paths;
    }

    /// <summary>
    /// Resetea todas las teclas a sus valores por defecto
    /// </summary>
    public void ResetAllBindings()
    {
        if (inputActions == null) return;

        foreach (var actionMap in inputActions.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                action.RemoveAllBindingOverrides();
            }
        }

        rebindingOverrides.Clear();
        SaveRebindings();
        Debug.Log("Todos los bindings han sido reseteados");
    }

    /// <summary>
    /// Resetea una acción específica a su valor por defecto
    /// </summary>
    public void ResetActionBinding(string actionName)
    {
        var action = inputActions.FindAction(actionName);
        if (action != null)
        {
            action.RemoveAllBindingOverrides();
            SaveRebindings();
            Debug.Log($"Binding reseteado para {actionName}");
        }
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

    public static string GetDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "No asignado";

        path = path.Replace("<Keyboard>/", "")
                   .Replace("<Mouse>/", "")
                   .Replace("<Gamepad>/", "")
                   .Replace("/", "");

        var nameMap = new Dictionary<string, string>()
        {
            { "w", "W" },
            { "a", "A" },
            { "s", "S" },
            { "d", "D" },
            { "space", "Espacio" },
            { "leftShift", "Shift" },
            { "leftCtrl", "Ctrl" },
            { "leftAlt", "Alt" },
            { "enter", "Enter" },
            { "escape", "Esc" },
            { "leftButton", "Click Izq" },
            { "rightButton", "Click Der" },
            { "buttonWest", "X (Gamepad)" },
            { "buttonSouth", "A (Gamepad)" },
            { "buttonNorth", "Y (Gamepad)" },
            { "buttonEast", "B (Gamepad)" },
            { "leftStickPress", "L3 (Gamepad)" },
            { "rightStickPress", "R3 (Gamepad)" },
            { "leftStick", "L-Stick (Gamepad)" },
            { "rightStick", "R-Stick (Gamepad)" },
        };

        return nameMap.ContainsKey(path) ? nameMap[path] : path;
    }

    public string GetBindingPath(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count)
            return "";

        return action.bindings[bindingIndex].effectivePath;
    }

    public InputActionAsset GetInputActions() => inputActions;
}