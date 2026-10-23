using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Dropdown que quita la navegación de sus ítems cada vez que se abre la lista.
public class NavlessDropdown : TMP_Dropdown
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        DisableItemNavigation();
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        base.OnSubmit(eventData);
        DisableItemNavigation();
    }

    // La lista se crea bajo el Canvas raíz y se destruye al cerrarla,
    // así que buscamos los ítems justo después de abrirla.
    private void DisableItemNavigation()
    {
        Canvas root = GetComponentInParent<Canvas>()?.rootCanvas;
        if (root == null) return;

        foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.GetComponent<TMP_Dropdown.DropdownItem>() == null) continue;

            Navigation nav = toggle.navigation;
            nav.mode = Navigation.Mode.None;
            toggle.navigation = nav;
        }
    }
}