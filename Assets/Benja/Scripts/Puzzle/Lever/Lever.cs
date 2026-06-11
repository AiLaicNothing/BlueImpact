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

    private bool _isActive;

    public bool IsActive => _isActive;
    private void Start()
    {
        receiver?.RegisterActivator(this);
    }


    public void Interact()
    {
        if (_isActive && !canToggleOff)
            return;

        SetStateInternal(!_isActive);
    }


    private void SetStateInternal(bool state)
    {
        _isActive = state;

        animator?.SetBool("IsActive", state);

        receiver?.Evaluate();
    }

    public string GetInteractionText()
    {
        return "Mover palanca";
    }

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }

}