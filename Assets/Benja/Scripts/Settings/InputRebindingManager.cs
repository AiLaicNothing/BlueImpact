using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InputRebindingManager : MonoBehaviour
{
    public static InputRebindingManager Instance { get; private set; }

    private InputActionAsset inputActions;
    private Dictionary<string, string> rebindingOverrides = new();

    // Configuración por defecto del Input System
    private readonly Dictionary<string, string> DefaultBindings = new()
    {
        { "Player/Move/Keyboard", "<Keyboard>/w,<Keyboard>/a,<Keyboard>/s,<Keyboard>/d" },
        { "Player/Move/Gamepad", "<Gamepad>/leftStick" },
        { "Player/Look/Keyboard", "<Pointer>/delta" },
        { "Player/Look/Gamepad", "<Gamepad>/rightStick" },
        { "Player/Attack/Keyboard", "<Mouse>/leftButton" },
        { "Player/Attack/Gamepad", "<Gamepad>/buttonWest" },
        { "Player/Jump/Keyboard", "<Keyboard>/space" },
        { "Player/Jump/Gamepad", "<Gamepad>/buttonSouth" },
        { "Player/Dash/Keyboard", "<Keyboard>/leftShift" },
        { "Player/Dash/Gamepad", "<Gamepad>/leftStickPress" },
        { "Player/Interact/Keyboard", "<Keyboard>/e" },
        { "Player/Interact/Gamepad", "<Gamepad>/buttonNorth" },
        { "Player/Crouch/Keyboard", "<Keyboard>/c" },
        { "Player/Crouch/Gamepad", "<Gamepad>/buttonEast" },
        { "Player/Skill1/Keyboard", "<Keyboard>/1" },
        { "Player/Skill2/Keyboard", "<Keyboard>/2" },
        { "Player/Skill3/Keyboard", "<Keyboard>/3" },
        { "Player/Skill4/Keyboard", "<Keyboard>/4" },
    };

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
        // Cargar el Input System (ajusta la ruta según tu proyecto)
        inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

        if (inputActions == null)
        {
            Debug.LogError("No se pudo cargar InputSystem_Actions desde Resources");
            return;
        }

        inputActions.Enable();
    }

    // ==================== REBINDING ====================

    /// <summary>
    /// Inicia el proceso de rebinding para una acción específica
    /// </summary>
    public async Task<bool> RemapActionAsync(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Acción no encontrada: {actionName}");
            return false;
        }

        var rebind = action.PerformInteractiveRebinding(bindingIndex);
        rebind.OnComplete(operation =>
        {
            Debug.Log($"Remapeo completado para {actionName}: {action.bindings[bindingIndex].effectivePath}");
            SaveRebindings();
            operation.Dispose();
        });

        rebind.OnCancel(operation =>
        {
            Debug.Log($"Remapeo cancelado para {actionName}");
            operation.Dispose();
        });

        rebind.Start();

        // Esperar a que termine
        while (rebind.completed == false)
        {
            await Task.Delay(10);
        }

        return true;
    }

    /// <summary>
    /// Obtiene todas las acciones del Input System
    /// </summary>
    public List<string> GetAllActions()
    {
        List<string> actions = new();

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
    //public void ResetAllBindings()
    //{
    //    if (inputActions == null) return;

    //    foreach (var action in inputActions.ListAllActions())
    //    {
    //        action.RemoveAllBindingOverrides();
    //    }

    //    rebindingOverrides.Clear();
    //    SaveRebindings();
    //}

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
        }
    }

    // ==================== SAVE & LOAD ====================

    public void SaveRebindings()
    {
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
            inputActions.LoadBindingOverridesFromJson(rebindData);
            Debug.Log("Rebindings cargados");
        }
    }

    // ==================== HELPERS ====================

    /// <summary>
    /// Obtiene un nombre legible para una tecla/botón
    /// </summary>
    public static string GetDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "No asignado";

        // Limpiar la ruta para que sea legible
        path = path.Replace("<Keyboard>/", "")
                   .Replace("<Mouse>/", "")
                   .Replace("<Gamepad>/", "")
                   .Replace("/", "");

        // Mapear nombres comunes
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

    /// <summary>
    /// Obtiene el camino efectivo de un binding
    /// </summary>
    public string GetBindingPath(string actionName, int bindingIndex = 0)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count)
            return "";

        return action.bindings[bindingIndex].effectivePath;
    }

    public InputActionAsset GetInputActions() => inputActions;
}