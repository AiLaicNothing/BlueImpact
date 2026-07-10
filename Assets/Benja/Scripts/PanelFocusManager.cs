using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

/// ✅ PANEL FOCUS MANAGER
/// Controla qué panel tiene el foco y evita navegación cruzada entre paneles
/// Esto previene que pierda el cursor navegando entre múltiples paneles activos

public class PanelFocusManager : MonoBehaviour
{
    public static PanelFocusManager Instance { get; private set; }

    private Stack<IGamepadPanel> panelStack = new Stack<IGamepadPanel>();
    private IGamepadPanel currentFocusedPanel;

    public event Action<IGamepadPanel> OnPanelFocusChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// ✅ REGISTRAR UN PANEL COMO ACTIVO (Le da el foco)
    public void PushPanel(IGamepadPanel panel)
    {
        // ✅ DESACTIVAR EL PANEL ANTERIOR
        if (currentFocusedPanel != null)
        {
            currentFocusedPanel.SetInputEnabled(false);
            currentFocusedPanel.SetInteractable(false);
        }

        // ✅ ACTIVAR EL NUEVO PANEL
        panelStack.Push(panel);
        currentFocusedPanel = panel;
        currentFocusedPanel.SetInputEnabled(true);
        currentFocusedPanel.SetInteractable(true);

        OnPanelFocusChanged?.Invoke(currentFocusedPanel);

        Debug.Log($"[PanelFocusManager] Panel enfocado: {panel.GetPanelName()}");
    }

    /// ✅ QUITAR PANEL DEL FOCO (Atrás)
    public void PopPanel()
    {
        if (panelStack.Count == 0)
        {
            Debug.LogWarning("[PanelFocusManager] No hay paneles en el stack");
            return;
        }

        // ✅ DESACTIVAR PANEL ACTUAL
        currentFocusedPanel = panelStack.Pop();
        currentFocusedPanel.SetInputEnabled(false);
        currentFocusedPanel.SetInteractable(false);

        // ✅ ACTIVAR PANEL ANTERIOR
        if (panelStack.Count > 0)
        {
            currentFocusedPanel = panelStack.Peek();
            currentFocusedPanel.SetInputEnabled(true);
            currentFocusedPanel.SetInteractable(true);

            OnPanelFocusChanged?.Invoke(currentFocusedPanel);

            Debug.Log($"[PanelFocusManager] Panel enfocado (volviendo): {currentFocusedPanel.GetPanelName()}");
        }
        else
        {
            currentFocusedPanel = null;
            Debug.Log("[PanelFocusManager] No hay paneles en el stack - Enfoque limpio");
        }
    }

    /// ✅ REEMPLAZAR PANEL (Para cambios sin pila)
    public void ReplacePanel(IGamepadPanel panel)
    {
        if (currentFocusedPanel != null)
        {
            currentFocusedPanel.SetInputEnabled(false);
            currentFocusedPanel.SetInteractable(false);
        }

        if (panelStack.Count > 0)
            panelStack.Pop();

        panelStack.Push(panel);
        currentFocusedPanel = panel;
        currentFocusedPanel.SetInputEnabled(true);
        currentFocusedPanel.SetInteractable(true);

        OnPanelFocusChanged?.Invoke(currentFocusedPanel);

        Debug.Log($"[PanelFocusManager] Panel reemplazado: {panel.GetPanelName()}");
    }

    /// ✅ OBTENER PANEL ACTUAL CON FOCO
    public IGamepadPanel GetCurrentPanel() => currentFocusedPanel;

    /// ✅ ¿ESTE PANEL TIENE EL FOCO?
    public bool IsPanelFocused(IGamepadPanel panel) => currentFocusedPanel == panel;
}

/// ✅ INTERFAZ PARA PANELES GAMEPAD
public interface IGamepadPanel
{
    void SetInputEnabled(bool enabled);
    void SetInteractable(bool interactable);
    string GetPanelName();
}