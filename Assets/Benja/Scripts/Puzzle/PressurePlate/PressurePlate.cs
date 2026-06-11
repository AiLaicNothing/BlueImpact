// PressurePlate.cs

using System;
using System.Xml.Linq;
using UnityEngine;

public class PressurePlate : MonoBehaviour, IActivator
{
    [Header("Configuración")]
    public PuzzleReceiver receiver;

    public LayerMask validLayers;

    public bool canDeactivate = true;

    private int _objectsOnPlate;

    public bool IsActive => _objectsOnPlate > 0;

    private void Start()
    {
        Debug.Log($"[PressurePlate] Start -> {name}");

        receiver?.RegisterActivator(this);
    }


    private void UpdateVisual(bool pressed)
    {
        Debug.Log($"[PressurePlate] Visual actualizado. Pressed = {pressed}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInValidLayer(other.gameObject))
            return;

        _objectsOnPlate++;

        UpdateVisual(IsActive);

        receiver?.Evaluate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInValidLayer(other.gameObject))
            return;

        if (!canDeactivate)
            return;

        _objectsOnPlate =
            Mathf.Max(0, _objectsOnPlate - 1);

        UpdateVisual(IsActive);

        receiver?.Evaluate();
    }

    private bool IsInValidLayer(GameObject obj)
    {
        bool valid =
            (validLayers.value & (1 << obj.layer)) != 0;

        Debug.Log($"[PressurePlate] ValidLayer check: {obj.name} -> {valid}");

        return valid;
    }

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }
}