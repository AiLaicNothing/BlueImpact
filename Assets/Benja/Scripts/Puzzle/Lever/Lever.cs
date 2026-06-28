using System;
using System.Xml.Linq;
using UnityEngine;

public class Lever : MonoBehaviour, IInteractable, IActivator
{
    [Header("Configuración")]
    public bool canToggleOff = true;

    public PuzzleReceiver receiver;

    [Header("Save")]
    [SerializeField] private bool persistent = false;
    [SerializeField] private string leverID;

    public bool Persistent => persistent;
    public string LeverID => leverID;

    [Header("Visual")]
    public Animator animator;

    [Header("Indicador Visual")]
    [SerializeField] private bool useVisualIndicator = false;
    [SerializeField] private SpriteRenderer indicatorSprite;
    [SerializeField] private Color offColor = Color.gray;
    [SerializeField] private Color onColor = Color.green;

    private bool _isActive;
    private bool _onCooldown;

    public bool IsActive => _isActive;

    private void Start()
    {
        receiver?.RegisterActivator(this);
        ApplyIndicator(false);
    }

    public void Interact()
    {
        if (_onCooldown) return;
        if (_isActive && !canToggleOff) return;

        SetStateInternal(!_isActive);
    }

    private void SetStateInternal(bool state)
    {
        _isActive = state;

        animator?.SetBool("IsActive", state);
        ApplyIndicator(state);

        receiver?.Evaluate();
    }

    // Llamado por PuzzleReceiver para forzar reset
    public void ResetState()
    {
        _isActive = false;
        animator?.SetBool("IsActive", false);
        ApplyIndicator(false);
    }

    // Llamado por PuzzleReceiver para bloquear/desbloquear
    public void SetCooldown(bool active)
    {
        _onCooldown = active;
    }

    private void ApplyIndicator(bool state)
    {
        if (!useVisualIndicator || indicatorSprite == null) return;
        indicatorSprite.color = state ? onColor : offColor;
    }

    public string GetInteractionText()
    {
        return _onCooldown ? null : "Mover palanca";
    }

    public bool CanInteract() => !_onCooldown;

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }
}