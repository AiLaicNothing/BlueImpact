using UnityEngine;
using UnityEngine.UI;

public static class UINavigation
{
    // Pone Navigation en None en el objeto y todos sus hijos, incluidos los inactivos.
    public static void DisableNavigation(Transform root)
    {
        if (root == null) return;

        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            Navigation nav = selectable.navigation;
            nav.mode = Navigation.Mode.None;
            selectable.navigation = nav;
        }
    }
}