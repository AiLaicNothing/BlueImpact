using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamepadCursor : MonoBehaviour
{
    public static GamepadCursor Instance { get; private set; }

    [Header("Cursor")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private float joystickSensitivity = 1000f;
    [SerializeField] private float stickDeadzone = 0.15f;

    [Header("Valores (Slider / Scrollbar)")]
    [SerializeField] private float sliderStepPercent = 0.05f;
    [SerializeField] private float sliderRepeatDelay = 0.4f;
    [SerializeField] private float sliderRepeatRate = 0.08f;

    private struct Entry
    {
        public object owner;
        public Action onClose;
    }

    // La última entrada es la activa. Al liberarla, la de abajo recupera el control.
    private readonly List<Entry> stack = new List<Entry>();
    private readonly List<RaycastResult> results = new List<RaycastResult>();
    private float nextAdjustTime;

    // ---------- Bootstrap ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("GamepadCursor");
        go.AddComponent<GamepadCursor>();
    }

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---------- API pública de la pila ----------

    public bool HasOpenPanels => stack.Count > 0;

    public bool IsTop(object owner) =>
        stack.Count > 0 && stack[stack.Count - 1].owner == owner;

    // True si el panel activo se puede cerrar (tiene callback de cierre)
    public bool TopCanClose =>
        stack.Count > 0 && stack[stack.Count - 1].onClose != null;

    // Cierra el panel activo (Escape o B)
    public void CloseTop()
    {
        if (!TopCanClose) return;
        stack[stack.Count - 1].onClose.Invoke();
    }

    // Cierra todos los paneles con callback, de arriba hacia abajo, excepto 'keep'
    public void CloseAllExcept(object keep)
    {
        var snapshot = new List<Entry>(stack);

        for (int i = snapshot.Count - 1; i >= 0; i--)
        {
            Entry e = snapshot[i];

            if (e.owner == keep || e.onClose == null)
                continue;

            // Solo cierra si sigue en la pila (un cierre previo pudo haberlo quitado)
            if (stack.Exists(x => x.owner == e.owner))
                e.onClose.Invoke();
        }
    }

    // Un panel llama a Acquire al abrirse. onClose se ejecuta al pulsar B o Escape.
    public void Acquire(object owner, Action onClose = null)
    {
        stack.RemoveAll(e => e.owner == owner);
        stack.Add(new Entry { owner = owner, onClose = onClose });

        ClearEventSystemSelection();
        SyncGameMode();
    }

    // Un panel llama a Release al cerrarse
    public void Release(object owner)
    {
        if (stack.RemoveAll(e => e.owner == owner) > 0)
            SyncGameMode();
    }

    // El modo de juego depende solo de si hay paneles abiertos
    private void SyncGameMode()
    {
        if (GameModeManager.Instance == null) return;

        GameModeManager.Instance.SetMode(stack.Count > 0 ? GameMode.UI : GameMode.Gameplay);
    }

    // ---------- Update ----------

    private void Update()
    {
        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        PruneDeadOwners();

        if (stack.Count == 0)
        {
            cursorInitialized = false;
            return;
        }
        if (eventSystem == null || Gamepad.current == null || Mouse.current == null) return;

        MoveCursor();
        AdjustValueUnderCursor();

        if (Gamepad.current.aButton.wasPressedThisFrame)
            ClickAtCursor();

        if (Gamepad.current.bButton.wasPressedThisFrame)
            CloseTop();
    }

    // Quita paneles ya destruidos (por ejemplo al cambiar de escena)
    private void PruneDeadOwners()
    {
        if (stack.RemoveAll(e => e.owner is UnityEngine.Object o && o == null) > 0)
            SyncGameMode();
    }

    // ---------- Cursor ----------

    private Vector2 virtualCursorPos;
    private bool cursorInitialized;

    private void MoveCursor()
    {
        if (!cursorInitialized)
        {
            virtualCursorPos = Mouse.current.position.ReadValue();
            cursorInitialized = true;
        }

        Vector2 stick = Gamepad.current.leftStick.ReadValue();

        if (stick.magnitude >= stickDeadzone)
        {
            // El stick manda: mueve y fuerza la posición del cursor
            virtualCursorPos += stick * joystickSensitivity * Time.unscaledDeltaTime;
            virtualCursorPos.x = Mathf.Clamp(virtualCursorPos.x, 0f, Screen.width - 1);
            virtualCursorPos.y = Mathf.Clamp(virtualCursorPos.y, 0f, Screen.height - 1);
            Mouse.current.WarpCursorPosition(virtualCursorPos);
        }
        else if (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
        {
            // El mouse se movió: sincronizamos y no hacemos warp
            virtualCursorPos = Mouse.current.position.ReadValue();
        }
    }

    // ---------- Click ----------

    private void ClickAtCursor()
    {
        RaycastUnderCursor();

        foreach (RaycastResult hit in results)
        {
            if (TryInvoke(hit.gameObject))
                break;
        }

        // Evita que Submit (A) dispare un botón que quedó seleccionado
        ClearEventSystemSelection();
    }

    private bool TryInvoke(GameObject go)
    {
        // Dropdown primero: sus items son Toggle y no deben caer en la rama genérica
        var dropdown = go.GetComponentInParent<TMP_Dropdown>();
        if (dropdown != null && dropdown.interactable)
        {
            if (dropdown.IsExpanded)
            {
                var item = go.GetComponentInParent<Toggle>();
                if (item != null && item.interactable)
                {
                    item.isOn = true;
                    return true;
                }

                dropdown.Hide();
                return true;
            }

            dropdown.Show();
            return true;
        }

        var button = go.GetComponentInParent<Button>();
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
            return true;
        }

        var toggle = go.GetComponentInParent<Toggle>();
        if (toggle != null && toggle.interactable)
        {
            toggle.isOn = !toggle.isOn;
            return true;
        }

        // Slider y Scrollbar no se activan con A: se ajustan con el D-pad
        return false;
    }

    // ---------- Slider / Scrollbar con D-pad ----------

    private void AdjustValueUnderCursor()
    {
        var dpad = Gamepad.current.dpad;
        Vector2 pad = dpad.ReadValue();
        if (pad == Vector2.zero) return;

        bool pressedNow = dpad.left.wasPressedThisFrame || dpad.right.wasPressedThisFrame
                       || dpad.up.wasPressedThisFrame || dpad.down.wasPressedThisFrame;

        if (!pressedNow && Time.unscaledTime < nextAdjustTime)
            return;

        nextAdjustTime = Time.unscaledTime + (pressedNow ? sliderRepeatDelay : sliderRepeatRate);

        RaycastUnderCursor();

        foreach (RaycastResult hit in results)
        {
            Slider slider = hit.gameObject.GetComponentInParent<Slider>();
            if (slider != null)
            {
                if (!slider.interactable) return;

                int dir = SliderDirection(slider.direction, pad);
                if (dir != 0)
                {
                    float step = (slider.maxValue - slider.minValue) * sliderStepPercent * dir;
                    slider.value = Mathf.Clamp(slider.value + step, slider.minValue, slider.maxValue);
                }
                return;
            }

            Scrollbar scrollbar = hit.gameObject.GetComponentInParent<Scrollbar>();
            if (scrollbar != null)
            {
                if (!scrollbar.interactable) return;

                int dir = ScrollbarDirection(scrollbar.direction, pad);
                if (dir != 0)
                    scrollbar.value = Mathf.Clamp01(scrollbar.value + sliderStepPercent * dir);
                return;
            }
        }
    }

    private int SliderDirection(Slider.Direction direction, Vector2 pad)
    {
        switch (direction)
        {
            case Slider.Direction.LeftToRight: return Mathf.RoundToInt(pad.x);
            case Slider.Direction.RightToLeft: return -Mathf.RoundToInt(pad.x);
            case Slider.Direction.BottomToTop: return Mathf.RoundToInt(pad.y);
            case Slider.Direction.TopToBottom: return -Mathf.RoundToInt(pad.y);
        }
        return 0;
    }

    private int ScrollbarDirection(Scrollbar.Direction direction, Vector2 pad)
    {
        switch (direction)
        {
            case Scrollbar.Direction.LeftToRight: return Mathf.RoundToInt(pad.x);
            case Scrollbar.Direction.RightToLeft: return -Mathf.RoundToInt(pad.x);
            case Scrollbar.Direction.BottomToTop: return Mathf.RoundToInt(pad.y);
            case Scrollbar.Direction.TopToBottom: return -Mathf.RoundToInt(pad.y);
        }
        return 0;
    }

    // ---------- Helpers ----------

    private void RaycastUnderCursor()
    {
        results.Clear();

        if (eventSystem == null || Mouse.current == null) return;

        var pointer = new PointerEventData(eventSystem)
        {
            position = Mouse.current.position.ReadValue()
        };

        eventSystem.RaycastAll(pointer, results);
    }

    private void ClearEventSystemSelection()
    {
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }
}