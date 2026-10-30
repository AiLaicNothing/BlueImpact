using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class InputTextToken
{
    public string token;
    public InputActionReference action;
}

public class InputTextFormatter : MonoBehaviour
{
    public static InputTextFormatter Instance {  get; private set; }

    [Header("Player Input")]
    [SerializeField] private PlayerControl _playerControl;
    [SerializeField] private PlayerInput _playerInput;

    [Header("Avaible Placeholders")]
    [SerializeField] private List<InputTextToken> _tokens = new();

    public event Action OnBindingChanged;

    private string lastControlScheme;

    private void OnEnable()
    {
        lastControlScheme = _playerInput != null? _playerInput.currentControlScheme : null;
        PlayerSpawn_Manager.OnPlayerSpawned += HandlePlayerSpawned;

        InputSystem.onActionChange += HandleActionChange;

        // ADDED: Retrieve the player if already spawned.
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerControl existingPlayer = PlayerSpawn_Manager.Instance.CurrentPlayerControl;

            if (existingPlayer != null) SetPlayer(existingPlayer);
        }

        lastControlScheme = _playerInput != null? _playerInput.currentControlScheme : null;
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= HandlePlayerSpawned;

        InputSystem.onActionChange -= HandleActionChange;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (_playerInput == null) return;

        if (lastControlScheme != _playerInput.currentControlScheme)
        {
            lastControlScheme = _playerInput.currentControlScheme;

            OnBindingChanged?.Invoke();
        }
    }

    private void HandleActionChange(object obj, InputActionChange change)
    {
        if (_playerInput == null) return;

        if (change == InputActionChange.BoundControlsChanged)
        {
            OnBindingChanged?.Invoke();
        }
    }

    public string Format(string originalText)
    {
        if (string.IsNullOrEmpty(originalText)) return string.Empty;

        if (_playerInput == null || _playerInput.actions == null) return originalText;

        string result = originalText;
        string bindingGroup = null;

        if (!string.IsNullOrEmpty(_playerInput.currentControlScheme))
        {
            var scheme = _playerInput.actions.FindControlScheme(_playerInput.currentControlScheme);

            if (scheme.HasValue) bindingGroup = scheme.Value.bindingGroup;
        }

        foreach (InputTextToken token in _tokens)
        {
            if (token == null || string.IsNullOrEmpty(token.token) ||
                token.action == null || token.action.action == null)
                continue;

            InputAction action = _playerInput.actions.FindAction(token.action.action.id.ToString(), false);

            if (action == null) continue;

            string buttonName;

            if (TryGetKeyboardComposite(action, bindingGroup, out string compositeName))
            {
                buttonName = compositeName;
            }
            else
            {
                buttonName = action.GetBindingDisplayString(options: 
                    InputBinding.DisplayStringOptions.DontIncludeInteractions,
                    group: bindingGroup);
            }

            if (string.IsNullOrWhiteSpace(buttonName)) continue;

            string placeholder = "{" + token.token + "}";

            result = result.Replace(placeholder, buttonName);
        }

        return result;
    }


    private bool TryGetKeyboardComposite(InputAction action, string bindingGroup, out string result)
    {
        result = null;

        var bindings = action.bindings;

        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding composite = bindings[i];

            if (!composite.isComposite) continue;

            bool parentMatches = string.IsNullOrEmpty(bindingGroup) ||
                InputBinding.MaskByGroup(bindingGroup).Matches(composite);

            string up = null;
            string left = null;
            string down = null;
            string right = null;

            for (int j = i + 1; j < bindings.Count; j++)
            {
                InputBinding part = bindings[j];

                if (!part.isPartOfComposite) break;

                bool partMatches = string.IsNullOrEmpty(bindingGroup) ||
                    parentMatches || InputBinding.MaskByGroup(bindingGroup).Matches(part);

                if (!partMatches) continue;

                string path = part.effectivePath;

                if (string.IsNullOrEmpty(path) ||!path.StartsWith("<Keyboard>/",StringComparison.OrdinalIgnoreCase))
                    continue;

                string keyName = action.GetBindingDisplayString(j,
                    InputBinding.DisplayStringOptions.DontIncludeInteractions);

                switch (part.name?.ToLowerInvariant())
                {
                    case "up":
                        up = keyName;
                        break;

                    case "left":
                        left = keyName;
                        break;

                    case "down":
                        down = keyName;
                        break;

                    case "right":
                        right = keyName;
                        break;
                }
            }

            if (up != null && left != null && down != null && right != null)
            {
                result = string.Join("/", new[] { up, left, down, right });

                return true;
            }
        }

        return false;
    }

    private void HandlePlayerSpawned(PlayerControl player)
    {
        SetPlayer(player);
    }

    private void SetPlayer(PlayerControl player)
    {
        _playerControl = player;
        _playerInput = null;

        if (_playerControl == null) return;

        _playerInput = _playerControl.GetComponentInChildren<PlayerInput>(true);

        if (_playerInput == null) _playerInput = _playerControl.GetComponentInParent<PlayerInput>();

        if (_playerInput == null)
        {
            Debug.LogWarning("[InputTextFormatter] PlayerInput not found.", _playerControl);

            return;
        }

        lastControlScheme = _playerInput.currentControlScheme;

        OnBindingChanged?.Invoke();
    }
}
